namespace ConferenceRoomBookingAPIv3.DomainModels;

public sealed class Booking
{
    public Guid Id { get; init; }
    public Guid RoomId { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    public List<BookingServiceSnapshot> Services { get; init; } = new List<BookingServiceSnapshot>();
    public decimal RoomCost { get; init; }
    public decimal ServicesCost { get; init; }
    public decimal TotalCost => RoomCost + ServicesCost;
}
