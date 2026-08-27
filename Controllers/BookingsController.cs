using ConferenceRoomBookingAPIv3.Application;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
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

        try
        {
            Booking booking = await service.CreateAsync(request.RoomId, request.StartsAt!.Value, request.DurationMinutes, request.ServiceIds, cancellationToken);
            var response = new BookingResponse(
                booking.Id,
                booking.RoomId,
                booking.StartsAt,
                booking.EndsAt,
                booking.RoomCost,
                booking.ServicesCost,
                booking.TotalCost,
                booking.Services.Select(item => new ServiceResponse(item.Id, item.Name, item.Price)).ToList());

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (BookingException exception)
        {
            return exception.Code switch
            {
                ErrorCode.RoomNotFound or ErrorCode.ServiceNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: exception.Code.ToValue(), detail: exception.Message),
                ErrorCode.BookingConflict => Problem(statusCode: StatusCodes.Status409Conflict, title: exception.Code.ToValue(), detail: exception.Message),
                _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Code.ToValue(), detail: exception.Message)
            };
        }
    }
}
