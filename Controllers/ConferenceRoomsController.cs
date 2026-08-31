using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.Controllers.Helpers;
using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBookingAPIv3.Controllers;

/// <summary>
/// API для управления конференц-залами: CRUD-операции, поиск доступных залов.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/rooms")]
public sealed class ConferenceRoomsController(IConferenceRoomRepository repository, BookingService bookingService) : ControllerBase
{
    /// <summary>
    /// Получить список всех конференц-залов.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список всех залов.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok((await repository.GetRoomsAsync(cancellationToken)).Select(ConferenceRoomsHelper.ToResponse));

    /// <summary>
    /// Получить информацию о конкретном зале по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор зала.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Информация о зале или 404 Not Found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await repository.GetRoomAsync(id, cancellationToken) is { } room ? Ok(ConferenceRoomsHelper.ToResponse(room)) : NotFound();

    /// <summary>
    /// Создать новый конференц-зал.
    /// </summary>
    /// <param name="request">Данные для создания зала.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Созданный зал с  Location header.</returns>
    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ConferenceRoom room = ConferenceRoomsHelper.ToEntity(request);
        await repository.AddRoomAsync(room, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = room.Id }, ConferenceRoomsHelper.ToResponse(room));
    }

    /// <summary>
    /// Частично обновить информацию о зале (только указанные поля).
    /// Использует optimistic concurrency для предотвращения lost update.
    /// </summary>
    /// <param name="id">Идентификатор зала.</param>
    /// <param name="request">Поля для обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>204 No Content при успехе, 404 если зал не найден, 409 при конфликте версий.</returns>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Patch(Guid id, RoomPatchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await repository.PatchRoomAsync(id, room => ConferenceRoomsHelper.ApplyPatch(room, request), cancellationToken))
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Удалить конференц-зал.
    /// Удаление невозможно, если на зал есть активные бронирования.
    /// </summary>
    /// <param name="id">Идентификатор зала.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>204 No Content при успехе, 404 если зал не найден, 409 если есть бронирования.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await repository.DeleteRoomAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Найти доступные залы для бронирования в указанный интервал времени.
    /// </summary>
    /// <param name="request">Параметры поиска: время начала, окончания и минимальная вместимость.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список доступных залов.</returns>
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> FindAvailable([FromQuery] AvailabilityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok((await bookingService.SearchAsync(request.StartsAt!.Value, request.EndsAt!.Value, request.Capacity, cancellationToken)).Select(ConferenceRoomsHelper.ToResponse));
    }
}
