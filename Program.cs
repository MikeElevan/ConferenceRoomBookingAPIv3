using System.Threading.RateLimiting;
using System.Globalization;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using ConferenceRoomBookingAPIv3.Infrastructure;
using ConferenceRoomBookingAPIv3.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using ConferenceRoomBookingAPIv3.Application.Repository;
using ConferenceRoomBookingAPIv3.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ConferenceRoomBookingAPIv3.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

PersistenceOptions persistenceOptions = builder.Configuration
    .GetSection(PersistenceOptions.SectionName)
    .Get<PersistenceOptions>()??new();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication("Test")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestingAuthenticationHandler>("Test", _ => { });
}
else if (builder.Environment.IsDevelopment())
{
    // Fake auth for local debugging only — no real IdP/JWT required.
    // Simulated user name/roles are configured via the "DevelopmentAuth" section
    // in appsettings.Development.json, so they can be tweaked without recompiling.
    builder.Services.Configure<DevelopmentAuthOptions>(builder.Configuration.GetSection(DevelopmentAuthOptions.SectionName));
    builder.Services.AddAuthentication("Development")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", _ => { });
}
else
{
    SecurityOptions securityOptions = builder.Configuration
        .GetSection(SecurityOptions.SectionName)
        .Get<SecurityOptions>()??throw new InvalidOperationException("Security configuration is missing.");
    if (string.IsNullOrWhiteSpace(securityOptions.Authority)||string.IsNullOrWhiteSpace(securityOptions.Audience))
    {
        throw new InvalidOperationException("Security:Authority and Security:Audience must be configured.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority=securityOptions.Authority;
            options.Audience=securityOptions.Audience;
            options.RequireHttpsMetadata=securityOptions.RequireHttpsMetadata;
        });
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("Reporting", policy => policy.RequireRole("Administrator", "Manager"));
});
builder.Services.AddHealthChecks();
builder.Services.Configure<HttpLoggingOptions>(builder.Configuration.GetSection(HttpLoggingOptions.SectionName));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHybridCache();
if (persistenceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    string connectionString = builder.Configuration.GetConnectionString(persistenceOptions.ConnectionStringName)
        ??throw new InvalidOperationException($"Connection string '{persistenceOptions.ConnectionStringName}' is not configured.");

    builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(
            persistenceOptions.Retry.MaxRetryCount,
            TimeSpan.FromSeconds(persistenceOptions.Retry.MaxRetryDelaySeconds),
            null)));
    builder.Services.AddScoped<IConferenceRoomRepositoryAdapter, DatabaseConferenceRoomRepository>();
}
else if (persistenceOptions.Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IConferenceRoomRepositoryAdapter, InMemoryConferenceRoomRepository>();
}
else
{
    throw new InvalidOperationException($"Unsupported persistence provider '{persistenceOptions.Provider}'. Use 'InMemory' or 'SqlServer'.");
}

builder.Services.AddScoped<IConferenceRoomRepository, CachedConferenceRoomRepository>();
builder.Services.AddSingleton<IPricingService, PricingService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode=StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext, string>(context =>
         RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown", _ => new FixedWindowRateLimiterOptions
         {
             PermitLimit=100,
             Window=TimeSpan.FromMinutes(1),
             QueueLimit=0,
             AutoReplenishment=true
         }));
});

WebApplication app = builder.Build();

if (persistenceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    using (IServiceScope scope = app.Services.CreateScope())
    {
        DatabaseInitializer.Initialize(scope.ServiceProvider.GetRequiredService<BookingDbContext>());
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Conference Room Booking API v1");
        options.RoutePrefix="swagger";
    });
}
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture=new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures=new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures=new List<CultureInfo> { CultureInfo.InvariantCulture }
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.UseMiddleware<HttpLoggingMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();