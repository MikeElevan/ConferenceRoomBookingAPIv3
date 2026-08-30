using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Persistence operations for the "conference room" aggregate only. Deliberately excludes
/// booking operations — a consumer that only ever manages rooms (e.g. <c>ConferenceRoomsController</c>'s
/// CRUD actions) shouldn't have to depend on, mock, or be recompiled for booking concerns it
/// never touches. See <see cref="IBookingRepository"/> for the booking aggregate.
/// </summary>
public interface IConferenceRoomRepository
{
    Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default);
    Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConferenceRoom> AddRoomAsync(ConferenceRoom room, CancellationToken cancellationToken = default);
    Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default);
}
