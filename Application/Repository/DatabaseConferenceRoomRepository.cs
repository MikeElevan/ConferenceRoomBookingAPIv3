using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class DatabaseConferenceRoomRepository(BookingDbContext dbContext, IDbContextFactory<BookingDbContext> contextFactory) : IConferenceRoomRepository, IBookingRepository
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
        // A fresh DbContext per attempt (rather than the shared, request-scoped one) matters here
        // specifically: UpsertServices adds new RoomService entries onto an already-tracked
        // ConferenceRoom. If SaveChangesAsync fails transiently and the strategy retries the same
        // delegate against the shared context, the still-tracked "Added" entities from the failed
        // attempt would still be there — patch() runs again and appends the same new service a
        // second time. A throwaway context per attempt means each retry starts from a clean read,
        // matching the pattern DeleteRoomAsync and TryAddBookingAsync already use below.
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using BookingDbContext attempt = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction =
                await attempt.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

            ConferenceRoom? existingRoom = await attempt.Rooms
                .Include(item => item.Services)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (existingRoom is null)
            {
                return false;
            }

            patch(existingRoom);

            try
            {
                await attempt.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueServiceNameViolation(exception))
            {
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
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using BookingDbContext attempt = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction = await attempt.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            ConferenceRoom? room = await attempt.Rooms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (room is null)
            {
                return false;
            }

            if (await attempt.Bookings.AnyAsync(booking => booking.RoomId == id, cancellationToken))
            {
                throw new BookingException(ErrorCode.RoomHasBookings, ErrorMessages.RoomHasBookings);
            }

            attempt.Rooms.Remove(room);
            try
            {
                await attempt.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new BookingException(ErrorCode.RoomHasBookings, ErrorMessages.RoomHasBookings);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
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
            await using BookingDbContext attempt = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction =
                await attempt.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            // A transient error from CommitAsync leaves its outcome unknown. If the commit did
            // succeed, a retry must recognize this booking rather than treating it as a conflict.
            if (await attempt.Bookings.AsNoTracking().AnyAsync(existing => existing.Id == booking.Id, cancellationToken))
            {
                return true;
            }

            bool hasConflict = await attempt.Bookings.AnyAsync(existing =>
                existing.RoomId == booking.RoomId &&
                existing.StartsAt < booking.EndsAt &&
                booking.StartsAt < existing.EndsAt, cancellationToken);

            if (hasConflict)
            {
                return false;
            }

            attempt.Bookings.Add(booking);
            await attempt.SaveChangesAsync(cancellationToken);
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
