namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

/// <summary>
/// Настройки подключения к хранилищу данных.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>Провайдер хранилища: «InMemory» или «SqlServer».</summary>
    public string Provider { get; init; } = "InMemory";

    /// <summary>Имя строки подключения в ConnectionStrings.</summary>
    public string ConnectionStringName { get; init; } = "DefaultConnection";

    /// <summary>Настройки повторов для SQL Server.</summary>
    public RetryOptions Retry { get; init; } = new();
}
