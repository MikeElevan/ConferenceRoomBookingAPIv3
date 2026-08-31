using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

/// <summary>
/// Репозиторий бронирований на основе EF Core.
/// Реализует только операции чтения/агрегации бронирований,
/// чтобы не смешивать ответственность с репозиторием залов (ISP).
/// </summary>
public sealed class DatabaseBookingRepository(BookingDbContext dbContext) : IBookingRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(roomId, nameof(roomId));
        return await dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .Where(booking => booking.RoomId == roomId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        return dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Services)
            .SingleOrDefaultAsync(booking => booking.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        List<RoomBookingStats> stats = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.StartsAt < to && from < booking.EndsAt)
            .GroupBy(booking => booking.RoomId)
            .Select(group => new RoomBookingStats(
                group.Key,
                group.Count(),
                group.Sum(b => b.RoomCost + b.ServicesCost),
                group.Sum(b => EF.Functions.DateDiffSecond(
                    b.StartsAt > from ? b.StartsAt : from,
                    b.EndsAt < to ? b.EndsAt : to) / 3600.0)))
            .ToListAsync(cancellationToken);

        return stats;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        List<ServiceStats> stats = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.StartsAt < to && from < booking.EndsAt)
            .SelectMany(booking => booking.Services)
            .GroupBy(service => new { service.ServiceId, service.Name })
            .Select(group => new ServiceStats(
                group.Key.ServiceId,
                group.Key.Name,
                group.Count(),
                group.Sum(s => s.Price)))
            .ToListAsync(cancellationToken);

        return stats;
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }
}