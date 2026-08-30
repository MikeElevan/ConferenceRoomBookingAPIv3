using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.Controllers.Helpers;
using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBookingAPIv3.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rooms")]
public sealed class ConferenceRoomsController(IConferenceRoomRepository repository, BookingService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok((await repository.GetRoomsAsync(cancellationToken)).Select(ConferenceRoomsHelper.ToResponse));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await repository.GetRoomAsync(id, cancellationToken) is { } room ? Ok(ConferenceRoomsHelper.ToResponse(room)) : NotFound();

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ConferenceRoom room = ConferenceRoomsHelper.ToEntity(request);
        await repository.AddRoomAsync(room, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = room.Id }, ConferenceRoomsHelper.ToResponse(room));
    }

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

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await repository.DeleteRoomAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> FindAvailable([FromQuery] AvailabilityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok((await bookingService.SearchAsync(request.StartsAt!.Value, request.EndsAt!.Value, request.Capacity, cancellationToken)).Select(ConferenceRoomsHelper.ToResponse));
    }
}
