using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBookingAPIv3.Controllers;

/// <summary>
/// API для получения отчётов по бронированиям.
/// Доступен только пользователям с ролями Administrator или Manager.
/// </summary>
[ApiController]
[Authorize(Policy = "Reporting")]
[Route("api/v1/reports")]
public sealed class ReportsController(ReportService service) : ControllerBase
{
    /// <summary>
    /// Получить отчёт по бронированиям за указанный период.
    /// </summary>
    /// <param name="request">Параметры отчёта: дата начала и окончания периода.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Отчёт с общей статистикой, детализацией по залам и услугам.</returns>
    [HttpGet("bookings")]
    public async Task<ActionResult<BookingReportResponse>> GetBookings([FromQuery] ReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await service.GetBookingReportAsync(request.From!.Value, request.To!.Value, cancellationToken));
    }
}
