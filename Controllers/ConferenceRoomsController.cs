using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ConferenceRoomBookingAPIv3.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rooms")]
public sealed class ConferenceRoomsController(IConferenceRoomRepository repository, BookingService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok((await repository.GetRoomsAsync(cancellationToken)).Select(ToResponse));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await repository.GetRoomAsync(id, cancellationToken) is { } room ? Ok(ToResponse(room)) : NotFound();

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ConferenceRoom room = ToEntity(request);
        await repository.AddRoomAsync(room, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = room.Id }, ToResponse(room));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Update(Guid id, RoomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await repository.UpdateRoomAsync(ToEntity(request, id), cancellationToken))
        {
            return NotFound();
        }
            
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await repository.DeleteRoomAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> FindAvailable([FromQuery] AvailabilityRequest request, CancellationToken cancellationToken) =>
        await FindAvailableAsync(request, cancellationToken);

    private async Task<ActionResult<IReadOnlyList<RoomResponse>>> FindAvailableAsync(AvailabilityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok((await bookingService.SearchAsync(request.StartsAt!.Value, request.EndsAt!.Value, request.Capacity, cancellationToken)).Select(ToResponse));
    }

    private static ConferenceRoom ToEntity(RoomRequest request, Guid? id = null) => new()
    {
        Id=id??Guid.NewGuid(),
        Name=request.Name.Trim(),
        Capacity=request.Capacity,
        BaseHourlyRate=request.BaseHourlyRate,
        Services=request.Services.Select(service => new RoomService
        {
            Id=Guid.NewGuid(),
            Name=service.Name.Trim(),
            Price=service.Price
        }).ToList()
    };

    private static RoomResponse ToResponse(ConferenceRoom room) =>
        new(room.Id, room.Name, room.Capacity, room.BaseHourlyRate,
            room.Services.Select(service => new ServiceResponse(service.Id, service.Name, service.Price)).ToList());
}
