namespace ConferenceRoomBookingAPIv3.DomainModels;

public sealed class ConferenceRoom
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<RoomService> Services { get; set; } = new List<RoomService>();
}
