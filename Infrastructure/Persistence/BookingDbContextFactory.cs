using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

/// <summary>
/// Фабрика для создания <see cref="BookingDbContext"/> в design-time (EF Core CLI).
/// Используется только для миграций, не вызывается в runtime.
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
