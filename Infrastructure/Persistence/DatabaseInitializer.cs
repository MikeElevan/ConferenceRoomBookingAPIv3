using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static void Initialize(BookingDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        // EnsureCreated() builds a schema from the current model snapshot and has no concept of
        // versioned change over time — it cannot apply incremental changes to a database that
        // already exists, and it must never be mixed with migrations (the two are mutually
        // exclusive). Migrate() applies whatever migrations haven't run yet, in order, and is a
        // no-op if the schema is already current — the only safe option for a production path.
        dbContext.Database.Migrate();

        if (dbContext.Rooms.Any())
        {
            return;
        }

        dbContext.Rooms.AddRange(
            CreateRoom("Зал А", 50, 2000m, ("Проектор", 500m), ("Wi-Fi", 300m)),
            CreateRoom("Зал B", 100, 3500m, ("Проектор", 500m), ("Wi-Fi", 300m), ("Звук", 700m)),
            CreateRoom("Зал C", 30, 1500m, ("Проектор", 500m), ("Wi-Fi", 300m), ("Звук", 700m)));
        dbContext.SaveChanges();
    }

    private static ConferenceRoom CreateRoom(
        string name,
        int capacity,
        decimal rate,
        params (string Name, decimal Price)[] services) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Capacity = capacity,
            BaseHourlyRate = rate,
            Services = services.Select(service => new RoomService
            {
                Id = Guid.NewGuid(),
                Name = service.Name,
                Price = service.Price
            }).ToList()
        };
}
