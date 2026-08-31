namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Настройки фейковой аутентификации для Development-окружения.
/// </summary>
public sealed class DevelopmentAuthOptions
{
    public const string SectionName = "DevelopmentAuth";

    /// <summary>Имя пользователя.</summary>
    public string UserName { get; init; } = "dev-user";

    /// <summary>Роли пользователя.</summary>
    public List<string> Roles { get; init; } = new();
}
