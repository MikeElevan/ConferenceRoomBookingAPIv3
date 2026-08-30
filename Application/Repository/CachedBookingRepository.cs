using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class CachedBookingRepository(
    IBookingRepositoryAdapter adapter,
    HybridCache cache,
    IOptions<CacheOptions> options) : IBookingRepository
{
    private readonly CacheOptions cacheOptions = options.Value;

    public async Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        // Not cached, deliberately — same reasoning as GetAvailableRoomsAsync on the room side:
        // report date ranges are effectively arbitrary, so a cache key per (from, to) pair would
        // almost never hit while still paying full cache-management overhead.
        return await adapter.GetBookingsInRangeAsync(from, to, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(roomId, nameof(roomId));

        if (!cacheOptions.Enabled)
        {
            return await adapter.GetBookingsAsync(roomId, cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            string.Format(CacheKeys.Bookings, roomId),
            async cancel => await adapter.GetBookingsAsync(roomId, cancel),
            CreateOptions(cacheOptions.BookingsMinutes),
            tags: new string[] { CacheKeys.RoomsTag },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ValidateIdentifier(booking.RoomId, nameof(booking.RoomId));
        if (booking.EndsAt <= booking.StartsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(booking));
        }

        bool result = await adapter.TryAddBookingAsync(booking, cancellationToken);
        if (result)
        {
            await cache.RemoveAsync(string.Format(CacheKeys.Bookings, booking.RoomId), cancellationToken);
        }

        return result;
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static HybridCacheEntryOptions CreateOptions(int durationMinutes) => new()
    {
        Expiration = TimeSpan.FromMinutes(durationMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(durationMinutes)
    };
}
