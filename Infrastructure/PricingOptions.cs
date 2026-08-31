namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Наценки/скидки по временным окнам для <see cref="Application.Services.PricingService"/>.
/// </summary>
public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>
    /// IANA-идентификатор часового пояса, определяющий, что означают «12:00», «18:00» и т.д.
    /// для тарифных окон. Должен быть фиксированным серверным значением — никогда не берётся из
    /// клиентского offset, иначитель может выбрать выгодное окно через offset в StartsAt/EndsAt.
    /// </summary>
    public string TimeZoneId { get; init; } = "Europe/Kyiv";

    /// <summary>Начало утренней скидки (час).</summary>
    public int MorningDiscountStartHour { get; init; } = 6;

    /// <summary>Начало стандартного тарифа (час).</summary>
    public int StandardStartHour { get; init; } = 9;

    /// <summary>Начало пикового тарифа (час).</summary>
    public int PeakStartHour { get; init; } = 12;

    /// <summary>Конец пикового тарифа (час).</summary>
    public int PeakEndHour { get; init; } = 14;

    /// <summary>Конец стандартного тарифа (час).</summary>
    public int StandardEndHour { get; init; } = 18;

    /// <summary>Конец вечерней скидки (час).</summary>
    public int EveningDiscountEndHour { get; init; } = 23;

    /// <summary>Множитель утренней скидки.</summary>
    public decimal MorningDiscountMultiplier { get; init; } = 0.90m;

    /// <summary>Множитель стандартного тарифа.</summary>
    public decimal StandardMultiplier { get; init; } = 1.00m;

    /// <summary>Множитель пикового тарифа.</summary>
    public decimal PeakMultiplier { get; init; } = 1.15m;

    /// <summary>Множитель вечерней скидки.</summary>
    public decimal EveningDiscountMultiplier { get; init; } = 0.80m;

    /// <summary>
    /// Множитель ночного тарифа (EveningDiscountEndHour – MorningDiscountStartHour,
    /// по умолчанию 23:00–06:00).
    /// </summary>
    public decimal NightMultiplier { get; init; } = 1.00m;
}
