namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Настройки стратегии повторов для резилентных операций.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Максимальное количество попыток.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Задержка между попытками (мс).</summary>
    public int DelayMilliseconds { get; init; } = 200;
}