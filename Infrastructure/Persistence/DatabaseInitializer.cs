using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static void Initialize(BookingDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        dbContext.Database.EnsureCreated();

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
