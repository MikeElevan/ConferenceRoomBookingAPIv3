namespace ConferenceRoomBookingAPIv3.DomainModels;

/// <summary>Immutable service details charged as part of a booking.</summary>
public sealed class BookingServiceSnapshot
{
    public Guid BookingId { get; init; }
    public Guid ServiceId { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
}
