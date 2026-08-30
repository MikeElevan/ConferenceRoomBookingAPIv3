using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.Controllers.Helpers;
using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ConferenceRoomBookingAPIv3.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/bookings")]
public sealed class BookingsController(BookingService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(BookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Booking booking = await service.CreateAsync(request.RoomId!.Value, request.StartsAt!.Value, request.DurationMinutes, request.ServiceIds, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, BookingsHelper.ToResponse(booking));
    }
}
