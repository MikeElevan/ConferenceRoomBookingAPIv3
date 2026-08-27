namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string Provider { get; init; } = "InMemory";
    public string ConnectionStringName { get; init; } = "DefaultConnection";
    public RetryOptions Retry { get; init; } = new();
}
