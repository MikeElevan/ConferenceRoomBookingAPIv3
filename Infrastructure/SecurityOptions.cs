namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Настройки JWT-аутентификации.
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>URL  authority (Identity Provider).</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>Audience для JWT-токена.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Требовать HTTPS metadata для authority.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}