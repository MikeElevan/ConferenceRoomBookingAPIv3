using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Controllers.Helpers;

public static class BookingsHelper
{
    public static BookingResponse ToResponse(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new BookingResponse(
            booking.Id,
            booking.RoomId,
            booking.StartsAt,
            booking.EndsAt,
            booking.RoomCost,
            booking.ServicesCost,
            booking.TotalCost,
            booking.Services.Select(service => new ServiceResponse(service.Id, service.Name, service.Price)).ToList());
    }
}
