using ConferenceRoomBookingAPIv3.Constants;

namespace ConferenceRoomBookingAPIv3.Infrastructure;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public bool Enabled { get; init; } = true;
    public int RoomListMinutes { get; init; } = CacheDurations.RoomListMinutes;
    public int RoomMinutes { get; init; } = CacheDurations.RoomMinutes;
    public int BookingsMinutes { get; init; } = CacheDurations.BookingsMinutes;
}
