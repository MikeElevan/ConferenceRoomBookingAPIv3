using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Controllers.Helpers;

/// <summary>
/// Маппинг Booking domain model → BookingResponse DTO.
/// </summary>
public static class BookingsHelper
{
    /// <summary>Преобразует доменную модель бронирования в контракт ответа.</summary>
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
            booking.Services.Select(service => new ServiceResponse(service.ServiceId, service.Name, service.Price)).ToList());
    }
}
