namespace ConferenceRoomBookingAPIv3.Infrastructure;

public sealed class HttpLoggingOptions
{
    public const string SectionName = "HttpLogging";

    public bool Enabled { get; init; }
    public bool IncludeRequestBody { get; init; }
    public bool IncludeResponseBody { get; init; }
    public int MaxBodyLength { get; init; }
}
