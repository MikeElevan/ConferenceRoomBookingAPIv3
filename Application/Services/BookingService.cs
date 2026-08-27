using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Application.Interfaces;

namespace ConferenceRoomBookingAPIv3.Application.Services;

public sealed class BookingService(IConferenceRoomRepository repository, IPricingService pricing)
{
    public async Task<IReadOnlyList<ConferenceRoom>> SearchAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(endsAt));
        }

        return await repository.GetAvailableRoomsAsync(startsAt, endsAt, capacity, cancellationToken);
    }

    public async Task<Booking> CreateAsync(Guid roomId, DateTimeOffset startsAt, int durationMinutes, IEnumerable<Guid> serviceIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIds);
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("The room identifier cannot be empty.", nameof(roomId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes);

        ConferenceRoom room = await repository.GetRoomAsync(roomId, cancellationToken)
            ?? throw new BookingException(ErrorCode.RoomNotFound, ErrorMessages.RoomNotFound);
        DateTimeOffset endsAt = startsAt.AddMinutes(durationMinutes);
        HashSet<Guid> selectedIds = serviceIds.ToHashSet();
        List<RoomService> selectedServices = room.Services.Where(service => selectedIds.Contains(service.Id)).ToList();

        if (selectedServices.Count != selectedIds.Count)
        {
            throw new BookingException(ErrorCode.ServiceNotFound, ErrorMessages.ServiceNotFound);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Services = selectedServices,
            RoomCost = pricing.CalculateRoomCost(room.BaseHourlyRate, startsAt, endsAt),
            ServicesCost = selectedServices.Sum(service => service.Price)
        };

        if (!await repository.TryAddBookingAsync(booking, cancellationToken))
        {
            throw new BookingException(ErrorCode.BookingConflict, ErrorMessages.BookingConflict);
        }

        return booking;
    }
}
