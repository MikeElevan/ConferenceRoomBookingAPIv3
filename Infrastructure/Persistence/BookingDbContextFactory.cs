using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` build a <see cref="BookingDbContext"/>
/// without booting the full application host — which in non-Development environments requires a
/// configured JWT authority/audience this tool has no reason to need. Only used by the EF Core CLI
/// at design time; never invoked by the running application.
/// </summary>
public sealed class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=ConferenceRoomBooking;Trusted_Connection=True;TrustServerCertificate=True;";

        DbContextOptionsBuilder<BookingDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        return new BookingDbContext(optionsBuilder.Options);
    }
}
