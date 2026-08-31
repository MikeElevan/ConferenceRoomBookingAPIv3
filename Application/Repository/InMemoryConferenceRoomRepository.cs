using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application;
using ConferenceRoomBookingAPIv3.Constants;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

/// <summary>
/// Ин-memory хранилище залов и бронирований для dev-режима и тестов.
/// Реализует интерфейсы залов, бронирований и транзакционного исполнителя,
/// так как это единое хранилище: разделение на отдельные классы
/// потребовало бы передачи общего внутреннего состояния между ними.
/// </summary>
public sealed class InMemoryConferenceRoomRepository : IConferenceRoomRepository, IBookingRepository, IBookingTransactionExecutor
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

    public Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(to));
        }

        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<Booking>>(
                bookings.Where(booking => booking.StartsAt < to && from < booking.EndsAt).Select(CloneBooking).ToList());
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
            Services = services.Select(service => RoomServiceFactory.Create(service.Name, service.Price)).ToList()
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

    public Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(patch);

        lock (sync)
        {
            if (!rooms.TryGetValue(id, out ConferenceRoom? room))
            {
                return Task.FromResult(false);
            }

            ConferenceRoom updated = Clone(room);
            patch(updated);
            rooms[id] = updated;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        lock (sync)
        {
            if (!rooms.ContainsKey(id))
            {
                return Task.FromResult(false);
            }

            if (bookings.Any(booking => booking.RoomId == id))
            {
                throw new BookingException(ErrorCode.RoomHasBookings, ErrorMessages.RoomHasBookings);
            }

            return Task.FromResult(rooms.Remove(id));
        }
    }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(roomId, nameof(roomId));

        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<Booking>>(
                bookings.Where(booking => booking.RoomId == roomId).Select(CloneBooking).ToList());
        }
    }

        public Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            Booking? existing = bookings.FirstOrDefault(booking => booking.IdempotencyKey == idempotencyKey);
            return Task.FromResult(existing is null ? null : CloneBooking(existing));
        }
    }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        lock (sync)
        {
            Booking? existing = bookings.FirstOrDefault(booking => booking.Id == id);
            return Task.FromResult(existing is null ? null : CloneBooking(existing));
        }
    }

    public Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var stats = bookings
                .Where(booking => booking.StartsAt < to && from < booking.EndsAt)
                .GroupBy(booking => booking.RoomId)
                .Select(group => new RoomBookingStats(
                    group.Key,
                    group.Count(),
                    group.Sum(b => b.RoomCost + b.ServicesCost),
                    group.Sum(b => GetOverlapHours(b.StartsAt, b.EndsAt, from, to))))
                .ToList();

            return Task.FromResult<IReadOnlyList<RoomBookingStats>>(stats);
        }
    }

    public Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var stats = bookings
                .Where(booking => booking.StartsAt < to && from < booking.EndsAt)
                .SelectMany(booking => booking.Services)
                .GroupBy(service => service.ServiceId)
                .Select(group => new ServiceStats(
                    group.Key,
                    group.First().Name,
                    group.Count(),
                    group.Sum(s => s.Price)))
                .ToList();

            return Task.FromResult<IReadOnlyList<ServiceStats>>(stats);
        }
    }

    private static double GetOverlapHours(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset from, DateTimeOffset to)
    {
        DateTimeOffset overlapStart = startsAt > from ? startsAt : from;
        DateTimeOffset overlapEnd = endsAt < to ? endsAt : to;
        return overlapEnd > overlapStart ? (overlapEnd - overlapStart).TotalHours : 0d;
    }

    public Task<Booking?> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
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
                return Task.FromResult<Booking?>(null);
            }

            // Idempotency guard inside the same lock as the insert: if a concurrent request with
            // the same key already committed, return its booking instead of creating a duplicate.
            if (!string.IsNullOrWhiteSpace(booking.IdempotencyKey))
            {
                Booking? existing = bookings.FirstOrDefault(item => item.IdempotencyKey == booking.IdempotencyKey);
                if (existing is not null)
                {
                    return Task.FromResult<Booking?>(CloneBooking(existing));
                }
            }

            bookings.Add(CloneBooking(booking));
            return Task.FromResult<Booking?>(booking);
        }
    }

    private static ConferenceRoom Clone(ConferenceRoom room) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        BaseHourlyRate = room.BaseHourlyRate,
        RowVersion = room.RowVersion,
        Services = room.Services.Select(RoomServiceFactory.Clone).ToList()
    };

    private static Booking CloneBooking(Booking booking) => new()
    {
        Id = booking.Id,
        RoomId = booking.RoomId,
        StartsAt = booking.StartsAt,
        EndsAt = booking.EndsAt,
        IdempotencyKey = booking.IdempotencyKey,
        RoomCost = booking.RoomCost,
        ServicesCost = booking.ServicesCost,
        Services = booking.Services.Select(service => new BookingServiceSnapshot
        {
            BookingId = service.BookingId,
            ServiceId = service.ServiceId,
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
