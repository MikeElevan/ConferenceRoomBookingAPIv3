using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

/// <summary>
/// Реализация IBookingTransactionExecutor на основе EF Core.
/// Инкапсулирует логику создания ExecutionStrategy и транзакций,
/// чтобы репозиторий не зависел от инфраструктурных деталей EF Core напрямую.
/// </summary>
public sealed class DatabaseBookingTransactionExecutor(
    BookingDbContext dbContext,
    IDbContextFactory<BookingDbContext> contextFactory) : IBookingTransactionExecutor
{
    /// <inheritdoc />
    public async Task<Booking?> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ValidateBooking(booking);

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using BookingDbContext attempt = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction =
                await attempt.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);

            // A transient error from CommitAsync leaves its outcome unknown. If the commit did
            // succeed, a retry must recognize this booking rather than treating it as a conflict.
            if (await attempt.Bookings.AsNoTracking()
                .AnyAsync(existing => existing.Id == booking.Id, cancellationToken))
            {
                return await GetBookingAsync(attempt, booking.Id, cancellationToken);
            }

            bool hasConflict = await attempt.Bookings.AnyAsync(
                existing => existing.RoomId == booking.RoomId &&
                            existing.StartsAt < booking.EndsAt &&
                            booking.StartsAt < existing.EndsAt,
                cancellationToken);

            if (hasConflict)
            {
                return null;
            }

            attempt.Bookings.Add(booking);
            try
            {
                await attempt.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsIdempotencyKeyViolation(exception))
            {
                // Two requests raced with the same IdempotencyKey: this one lost the unique-index
                // race to the winner's committed row. Return the winner's booking so the caller
                // replies with the first-completed result (idempotent replay) instead of a 500.
                await transaction.RollbackAsync(cancellationToken); // abort the now-doomed transaction
                await using BookingDbContext lookup = await contextFactory.CreateDbContextAsync(cancellationToken);
                return await lookup.Bookings.AsNoTracking()
                    .Include(item => item.Services)
                    .SingleOrDefaultAsync(item => item.IdempotencyKey == booking.IdempotencyKey, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return booking;
        });
    }

    private static Task<Booking?> GetBookingAsync(BookingDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.Bookings.AsNoTracking()
            .Include(item => item.Services)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private static bool IsIdempotencyKeyViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
        sqlException.Errors.Cast<Microsoft.Data.SqlClient.SqlError>()
            .Any(error => error.Number is 2601 or 2627 && error.Message.Contains("IdempotencyKey"));

    private static void ValidateBooking(Booking booking)
    {
        ValidateIdentifier(booking.RoomId, nameof(booking.RoomId));
        if (booking.EndsAt <= booking.StartsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(booking));
        }
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }
}