namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Configuration for the fake authentication handler used only in the Development environment.
/// Lets a developer change the simulated user's name and roles from appsettings.Development.json
/// without touching code or requiring a real JWT.
/// </summary>
public sealed class DevelopmentAuthOptions
{
    public const string SectionName = "DevelopmentAuth";

    public string UserName { get; init; } = "dev-user";
    public List<string> Roles { get; init; } = new();
}
