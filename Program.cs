using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Repository;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Infrastructure;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using ConferenceRoomBookingAPIv3.Middleware;
using ConferenceRoomBookingAPIv3.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

PersistenceOptions persistenceOptions = builder.Configuration
    .GetSection(PersistenceOptions.SectionName)
    .Get<PersistenceOptions>() ?? new();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BookingExceptionHandler>();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication("Test")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestingAuthenticationHandler>("Test", _ => { });
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<DevelopmentAuthOptions>(builder.Configuration.GetSection(DevelopmentAuthOptions.SectionName));
    builder.Services.AddAuthentication("Development")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", _ => { });
}
else
{
    SecurityOptions securityOptions = builder.Configuration
        .GetSection(SecurityOptions.SectionName)
        .Get<SecurityOptions>() ?? throw new InvalidOperationException("Security configuration is missing.");
    if (string.IsNullOrWhiteSpace(securityOptions.Authority) || string.IsNullOrWhiteSpace(securityOptions.Audience))
    {
        throw new InvalidOperationException("Security:Authority and Security:Audience must be configured.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = securityOptions.Authority;
            options.Audience = securityOptions.Audience;
            options.RequireHttpsMetadata = securityOptions.RequireHttpsMetadata;
        });
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("Reporting", policy => policy.RequireRole("Administrator", "Manager"));
});
builder.Services.AddHealthChecks();
builder.Services.Configure<HttpLoggingOptions>(builder.Configuration.GetSection(HttpLoggingOptions.SectionName));
builder.Services.Configure<PricingOptions>(builder.Configuration.GetSection(PricingOptions.SectionName));
builder.Services.AddSystemTimeZoneProvider();
if (persistenceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    string connectionString = builder.Configuration.GetConnectionString(persistenceOptions.ConnectionStringName)
        ?? throw new InvalidOperationException($"Connection string '{persistenceOptions.ConnectionStringName}' is not configured.");

    builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(
            persistenceOptions.Retry.MaxRetryCount,
            TimeSpan.FromSeconds(persistenceOptions.Retry.MaxRetryDelaySeconds),
            null)));
    builder.Services.AddDbContextFactory<BookingDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(
            persistenceOptions.Retry.MaxRetryCount,
            TimeSpan.FromSeconds(persistenceOptions.Retry.MaxRetryDelaySeconds),
            null)));

    builder.Services.AddScoped<DatabaseConferenceRoomRepository>();
    builder.Services.AddScoped<DatabaseBookingRepository>();
    builder.Services.AddScoped<IConferenceRoomRepository>(sp => sp.GetRequiredService<DatabaseConferenceRoomRepository>());
    builder.Services.AddScoped<IBookingRepository>(sp => sp.GetRequiredService<DatabaseBookingRepository>());
    builder.Services.AddScoped<IBookingTransactionExecutor, DatabaseBookingTransactionExecutor>();
}
else if (persistenceOptions.Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<InMemoryConferenceRoomRepository>();
    builder.Services.AddSingleton<IConferenceRoomRepository>(sp => sp.GetRequiredService<InMemoryConferenceRoomRepository>());
    builder.Services.AddSingleton<IBookingRepository>(sp => sp.GetRequiredService<InMemoryConferenceRoomRepository>());
    builder.Services.AddSingleton<IBookingTransactionExecutor>(sp => sp.GetRequiredService<InMemoryConferenceRoomRepository>());
}
else
{
    throw new InvalidOperationException($"Unsupported persistence provider '{persistenceOptions.Provider}'. Use 'InMemory' or 'SqlServer'.");
}

builder.Services.AddSingleton<IPricingService, PricingService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
         RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
         {
             PermitLimit = 100,
             Window = TimeSpan.FromMinutes(1),
             QueueLimit = 0,
             AutoReplenishment = true
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

// Logging stays outside the handler so it records failure responses too.
app.UseMiddleware<HttpLoggingMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { CultureInfo.InvariantCulture }
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
