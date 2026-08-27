namespace ConferenceRoomBookingAPIv3.DomainModels;

public sealed class RoomService
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
