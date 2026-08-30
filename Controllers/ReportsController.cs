using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBookingAPIv3.Controllers;

[ApiController]
[Authorize(Policy = "Reporting")]
[Route("api/v1/reports")]
public sealed class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<ActionResult<BookingReportResponse>> GetBookings([FromQuery] ReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await service.GetBookingReportAsync(request.From!.Value, request.To!.Value, cancellationToken));
    }
}
