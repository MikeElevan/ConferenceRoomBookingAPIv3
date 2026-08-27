namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

public sealed class RetryOptions
{
    public int MaxRetryCount { get; init; } = 3;
    public int MaxRetryDelaySeconds { get; init; } = 5;
}
