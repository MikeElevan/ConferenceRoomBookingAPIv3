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
