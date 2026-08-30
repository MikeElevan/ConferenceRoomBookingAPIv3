using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Persistence operations for the "booking" aggregate only. Split out from
/// <see cref="IConferenceRoomRepository"/> so that consumers who only need bookings (or only
/// need rooms) depend on — and can be mocked against — exactly the surface they use.
/// </summary>
public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
