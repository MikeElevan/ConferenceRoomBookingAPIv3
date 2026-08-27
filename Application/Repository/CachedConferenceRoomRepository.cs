using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Infrastructure;
using ConferenceRoomBookingAPIv3.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Application.Repository;

public sealed class CachedConferenceRoomRepository(
    IConferenceRoomRepositoryAdapter adapter,
    HybridCache cache,
    IOptions<CacheOptions> options) : IConferenceRoomRepository
{
    private readonly CacheOptions cacheOptions = options.Value;

    public async Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        if (!cacheOptions.Enabled)
        {
            return await adapter.GetRoomsAsync(cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            CacheKeys.Rooms,
            async cancel => await adapter.GetRoomsAsync(cancel),
            CreateOptions(cacheOptions.RoomListMinutes),
            tags: new string[] { CacheKeys.RoomsTag },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default)
    {
        return await adapter.GetAvailableRoomsAsync(startsAt, endsAt, capacity, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
    {
        if (!cacheOptions.Enabled)
        {
            return await adapter.GetAllBookingsAsync(cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            CacheKeys.AllBookings,
            async cancel => await adapter.GetAllBookingsAsync(cancel),
            CreateOptions(cacheOptions.BookingsMinutes),
            tags: new string[] { CacheKeys.RoomsTag },
            cancellationToken: cancellationToken);
    }

    public async Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        if (!cacheOptions.Enabled)
        {
            return await adapter.GetRoomAsync(id, cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            string.Format(CacheKeys.Room, id),
            async cancel => await adapter.GetRoomAsync(id, cancel),
            CreateOptions(cacheOptions.RoomMinutes),
            tags: new string[] { CacheKeys.RoomsTag },
            cancellationToken: cancellationToken);
    }

    public async Task<ConferenceRoom> AddRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        ConferenceRoom result = await adapter.AddRoomAsync(room, cancellationToken);
        await InvalidateRoomsAsync(cancellationToken);
        return result;
    }

    public async Task<bool> UpdateRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        bool result = await adapter.UpdateRoomAsync(room, cancellationToken);
        if (result)
        {
            await InvalidateRoomsAsync(cancellationToken);
        }
        return result;
    }

    public async Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(patch);

        bool result = await adapter.PatchRoomAsync(id, patch, cancellationToken);
        if (result)
        {
            await InvalidateRoomsAsync(cancellationToken);
        }
        return result;
    }

    public async Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        bool result = await adapter.DeleteRoomAsync(id, cancellationToken);
        if (result)
        {
            await InvalidateRoomsAsync(cancellationToken);
        }
        return result;
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
            await cache.RemoveAsync(CacheKeys.AllBookings, cancellationToken);
        }

        return result;
    }

    private async Task InvalidateRoomsAsync(CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(CacheKeys.RoomsTag, cancellationToken);
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
