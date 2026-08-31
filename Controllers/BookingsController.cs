using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.Controllers.Helpers;
using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ConferenceRoomBookingAPIv3.Controllers;

/// <summary>
/// API для создания бронирований конференц-залов.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/bookings")]
public sealed class BookingsController(BookingService service) : ControllerBase
{
    /// <summary>
    /// Создать новое бронирование конференц-зала.
    /// Стоимость рассчитывается автоматически на основе тарифа зала и выбранных услуг.
    /// </summary>
    /// <param name="request">Данные для создания бронирования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Созданное бронирование с рассчитанной стоимостью.</returns>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(BookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Booking booking = await service.CreateAsync(request.RoomId!.Value, request.StartsAt!.Value, request.DurationMinutes, request.ServiceIds, request.IdempotencyKey, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = booking.Id }, BookingsHelper.ToResponse(booking));
    }

    /// <summary>
    /// Получить бронирование по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Бронирование или 404 Not Found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        Booking? booking = await service.GetByIdAsync(id, cancellationToken);
        return booking is null ? NotFound() : Ok(BookingsHelper.ToResponse(booking));
    }
}
