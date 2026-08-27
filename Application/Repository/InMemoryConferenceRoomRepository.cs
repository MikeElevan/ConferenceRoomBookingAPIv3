using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Application.Interfaces;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class InMemoryConferenceRoomRepository : IConferenceRoomRepositoryAdapter
{
    private readonly Lock sync = new();
    private readonly Dictionary<Guid, ConferenceRoom> rooms = new Dictionary<Guid, ConferenceRoom>();
    private readonly List<Booking> bookings = new List<Booking>();

    public InMemoryConferenceRoomRepository()
    {
        AddSeedRoom("Зал А", 50, 2000m, ("Проектор", 500m), ("Wi-Fi", 300m));
        AddSeedRoom("Зал B", 100, 3500m, ("Проектор", 500m), ("Wi-Fi", 300m), ("Звук", 700m));
        AddSeedRoom("Зал C", 30, 1500m, ("Проектор", 500m), ("Wi-Fi", 300m), ("Звук", 700m));
    }

    public Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<Booking>>(bookings.ToList());
        }
    }

    public Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            List<ConferenceRoom> availableRooms = rooms.Values
                .Where(room => room.Capacity >= capacity)
                .Where(room => !bookings.Any(booking => booking.RoomId == room.Id && booking.StartsAt < endsAt && startsAt < booking.EndsAt))
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<ConferenceRoom>>(availableRooms);
        }
    }

    private void AddSeedRoom(string name, int capacity, decimal rate, params (string Name, decimal Price)[] services)
    {
        var room = new ConferenceRoom
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

        rooms.Add(room.Id, room);
    }

    public Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<ConferenceRoom>>(rooms.Values.Select(Clone).ToList());
        }
    }

    public Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        lock (sync)
        {
            return Task.FromResult(rooms.TryGetValue(id, out ConferenceRoom? room) ? Clone(room) : null);
        }
    }

    public Task<ConferenceRoom> AddRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        lock (sync)
        {
            rooms.Add(room.Id, Clone(room));
            return Task.FromResult(Clone(room));
        }
    }

    public Task<bool> UpdateRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        lock (sync)
        {
            if (!rooms.ContainsKey(room.Id))
            {
                return Task.FromResult(false);
            }

            rooms[room.Id] = Clone(room);
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        lock (sync)
        {
            return Task.FromResult(rooms.Remove(id));
        }
    }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(roomId, nameof(roomId));

        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<Booking>>(bookings.Where(booking => booking.RoomId == roomId).ToList());
        }
    }

    public Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ValidateBooking(booking);

        lock (sync)
        {
            bool hasConflict = bookings.Any(existing =>
                existing.RoomId == booking.RoomId &&
                existing.StartsAt < booking.EndsAt &&
                booking.StartsAt < existing.EndsAt);

            if (hasConflict)
            {
                return Task.FromResult(false);
            }

            bookings.Add(booking);
            return Task.FromResult(true);
        }
    }

    private static ConferenceRoom Clone(ConferenceRoom room) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        BaseHourlyRate = room.BaseHourlyRate,
        Services = room.Services.Select(service => new RoomService
        {
            Id = service.Id,
            Name = service.Name,
            Price = service.Price
        }).ToList()
    };

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateBooking(Booking booking)
    {
        ValidateIdentifier(booking.RoomId, nameof(booking.RoomId));
        if (booking.EndsAt <= booking.StartsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(booking));
        }
    }
}
