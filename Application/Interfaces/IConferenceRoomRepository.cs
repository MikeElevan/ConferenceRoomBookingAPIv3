using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

public interface IConferenceRoomRepository
{
    Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default);
    Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConferenceRoom> AddRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default);
    Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default);
    Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
