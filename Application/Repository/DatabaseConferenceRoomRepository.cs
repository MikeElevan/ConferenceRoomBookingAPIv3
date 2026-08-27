using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class DatabaseConferenceRoomRepository(BookingDbContext dbContext) : IConferenceRoomRepositoryAdapter
{
    public async Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Services)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default) =>
        await dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Services)
            .Where(room => room.Capacity >= capacity)
            .Where(room => !dbContext.Bookings.Any(booking =>
                booking.RoomId == room.Id &&
                booking.StartsAt < endsAt &&
                startsAt < booking.EndsAt))
            .ToListAsync(cancellationToken);

    public Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        return GetRoomInternalAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .ToListAsync(cancellationToken);

    private Task<ConferenceRoom?> GetRoomInternalAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Rooms
            .AsNoTracking()
            .Include(room => room.Services)
            .SingleOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<ConferenceRoom> AddRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        await dbContext.Rooms.AddAsync(room, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return room;
    }

    public async Task<bool> UpdateRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        ConferenceRoom? existingRoom = await dbContext.Rooms
            .Include(item => item.Services)
            .SingleOrDefaultAsync(item => item.Id == room.Id, cancellationToken);

        if (existingRoom is null)
        {
            return false;
        }

        existingRoom.Name = room.Name;
        existingRoom.Capacity = room.Capacity;
        existingRoom.BaseHourlyRate = room.BaseHourlyRate;
        SyncServices(existingRoom, room.Services);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(patch);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        ConferenceRoom? existingRoom = await dbContext.Rooms
            .Include(item => item.Services)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (existingRoom is null)
        {
            return false;
        }

        patch(existingRoom);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        ConferenceRoom? room = await dbContext.Rooms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (room is null)
        {
            return false;
        }

        dbContext.Rooms.Remove(room);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(roomId, nameof(roomId));
        return GetBookingsInternalAsync(roomId, cancellationToken);
    }

    private async Task<IReadOnlyList<Booking>> GetBookingsInternalAsync(Guid roomId, CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .Where(booking => booking.RoomId == roomId)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ValidateBooking(booking);

        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken))
        {
            bool hasConflict = await dbContext.Bookings.AnyAsync(existing =>
                existing.RoomId == booking.RoomId &&
                existing.StartsAt < booking.EndsAt &&
                booking.StartsAt < existing.EndsAt, cancellationToken);

            if (hasConflict)
            {
                return false;
            }

            foreach (RoomService service in booking.Services)
            {
                dbContext.Entry(service).State = EntityState.Unchanged;
            }

            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
    }

    private void SyncServices(ConferenceRoom existingRoom, IReadOnlyList<RoomService> incomingServices)
    {
        Dictionary<Guid, RoomService> incomingById = incomingServices.ToDictionary(service => service.Id);
        List<RoomService> servicesToRemove = existingRoom.Services
            .Where(service => !incomingById.ContainsKey(service.Id))
            .ToList();

        if (servicesToRemove.Count > 0)
        {
            dbContext.RoomServices.RemoveRange(servicesToRemove);
            foreach (RoomService service in servicesToRemove)
            {
                existingRoom.Services.Remove(service);
            }
        }

        foreach (RoomService existingService in existingRoom.Services)
        {
            RoomService incoming = incomingById[existingService.Id];
            existingService.Name = incoming.Name;
            existingService.Price = incoming.Price;
        }

        HashSet<Guid> existingIds = existingRoom.Services.Select(service => service.Id).ToHashSet();
        foreach (RoomService incoming in incomingServices.Where(service => !existingIds.Contains(service.Id)))
        {
            existingRoom.Services.Add(incoming);
        }
    }

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
