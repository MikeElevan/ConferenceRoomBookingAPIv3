using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application;
using ConferenceRoomBookingAPIv3.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class DatabaseConferenceRoomRepository(BookingDbContext dbContext) : IConferenceRoomRepository, IBookingRepository
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

    public async Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(to));
        }

        // Pushed into the SQL WHERE clause instead of pulling every booking ever made into
        // application memory and filtering with LINQ-to-Objects. A report over one day used to
        // cost a full table scan of Bookings regardless of range size — this scales with the
        // number of bookings that actually overlap [from, to), not with total booking history.
        return await dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .Where(booking => booking.StartsAt < to && from < booking.EndsAt)
            .ToListAsync(cancellationToken);
    }

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

    public async Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(patch);

        // EnableRetryOnFailure requires every multi-statement unit of work (transaction included)
        // to run through the resiliency-aware execution strategy, or EF Core throws at startup.
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // ReadCommitted, not Serializable: the one invariant this method must protect —
            // "no two services on the same room share a name" — is already enforced by the
            // (ConferenceRoomId, Name) unique index regardless of isolation level, since unique
            // constraints are checked at write time, not governed by MVCC snapshot rules. Two
            // concurrent PATCHes upserting different service names to the same room don't touch
            // overlapping rows and can safely run concurrently. Serializable would instead take
            // range locks across the whole read set for no additional correctness benefit here —
            // exactly the kind of unnecessary contention that produces the deadlocks under
            // concurrent load. (Contrast with TryAddBookingAsync below, where "no overlapping
            // booking" is a genuine range invariant with no equivalent index — that one keeps
            // Serializable because it actually needs it.)
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

            ConferenceRoom? existingRoom = await dbContext.Rooms
                .Include(item => item.Services)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (existingRoom is null)
            {
                return false;
            }

            patch(existingRoom);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueServiceNameViolation(exception))
            {
                // Two concurrent PATCHes each upserting a *new* service with the same name on
                // the same room: under ReadCommitted neither sees the other's still-uncommitted
                // insert, both attempt an insert, and the unique index rejects the second one at
                // the database level. Translate that race into the same typed conflict response
                // the rest of the API already uses, instead of an unhandled 500.
                throw new BookingException(ErrorCode.ServiceNameConflict, ErrorMessages.ServiceNameConflict);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private static bool IsUniqueServiceNameViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
        sqlException.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(error => error.Number is 2601 or 2627);

    public async Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        ConferenceRoom? room = await dbContext.Rooms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (room is null)
        {
            return false;
        }

        // Booking -> Room is Restrict, so the database itself would reject this delete with
        // an FK-violation error once bookings exist. Checking up front turns that into a clean,
        // typed 409 instead of an unhandled SqlException/DbUpdateException surfacing as a 500 —
        // and it keeps Room -> Services staying Cascade safe: a room can only be deleted once it
        // has zero bookings, which is also the only state in which none of its services can be
        // referenced by a BookingServices row.
        bool hasBookings = await dbContext.Bookings.AnyAsync(booking => booking.RoomId == id, cancellationToken);
        if (hasBookings)
        {
            throw new BookingException(ErrorCode.RoomHasBookings, ErrorMessages.RoomHasBookings);
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

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

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
        });
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
