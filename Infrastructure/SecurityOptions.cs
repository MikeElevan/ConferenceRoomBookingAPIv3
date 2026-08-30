namespace ConferenceRoomBookingAPIv3.Infrastructure;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
}