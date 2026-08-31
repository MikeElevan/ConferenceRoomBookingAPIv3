using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Application.Services;

/// <summary>
/// Сервис расчёта стоимости бронирования на основе временных окон.
/// Поддерживает динамическое ценообразование с учётом часового пояса и перехода на летнее/зимнее время.
/// </summary>
public sealed class PricingService : IPricingService
{
    private readonly TimeZoneInfo businessTimeZone;

    private readonly int morningDiscountStartHour;
    private readonly int standardStartHour;
    private readonly int peakStartHour;
    private readonly int peakEndHour;
    private readonly int standardEndHour;
    private readonly int eveningDiscountEndHour;

    private readonly decimal morningDiscountMultiplier;
    private readonly decimal standardMultiplier;
    private readonly decimal peakMultiplier;
    private readonly decimal eveningDiscountMultiplier;
    private readonly decimal nightMultiplier;

    public PricingService(IOptions<PricingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        PricingOptions value = options.Value;

        string timeZoneId = value.TimeZoneId;
        try
        {
            businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Pricing:TimeZoneId '{timeZoneId}' is not a valid time zone identifier.", exception);
        }

        ValidateHourBoundary(value.MorningDiscountStartHour, nameof(value.MorningDiscountStartHour));
        ValidateHourBoundary(value.StandardStartHour, nameof(value.StandardStartHour));
        ValidateHourBoundary(value.PeakStartHour, nameof(value.PeakStartHour));
        ValidateHourBoundary(value.PeakEndHour, nameof(value.PeakEndHour));
        ValidateHourBoundary(value.StandardEndHour, nameof(value.StandardEndHour));
        ValidateHourBoundary(value.EveningDiscountEndHour, nameof(value.EveningDiscountEndHour));

        if (!(value.MorningDiscountStartHour < value.StandardStartHour
            && value.StandardStartHour <= value.PeakStartHour
            && value.PeakStartHour < value.PeakEndHour
            && value.PeakEndHour <= value.StandardEndHour
            && value.StandardEndHour < value.EveningDiscountEndHour))
        {
            throw new InvalidOperationException(
                "Pricing window hours must satisfy MorningDiscountStartHour < StandardStartHour <= " +
                "PeakStartHour < PeakEndHour <= StandardEndHour < EveningDiscountEndHour.");
        }

        morningDiscountStartHour = value.MorningDiscountStartHour;
        standardStartHour = value.StandardStartHour;
        peakStartHour = value.PeakStartHour;
        peakEndHour = value.PeakEndHour;
        standardEndHour = value.StandardEndHour;
        eveningDiscountEndHour = value.EveningDiscountEndHour;

        morningDiscountMultiplier = value.MorningDiscountMultiplier;
        standardMultiplier = value.StandardMultiplier;
        peakMultiplier = value.PeakMultiplier;
        eveningDiscountMultiplier = value.EveningDiscountMultiplier;
        nightMultiplier = value.NightMultiplier;
    }

    /// <summary>
    /// Рассчитать стоимость аренды зала с учётом временных окон ценообразования.
    /// Стоимость вычисляется по часам: каждый отрезок времени умножается на множитель соответствующего окна.
    /// </summary>
    /// <param name="hourlyRate">Базовая почасовая ставка зала.</param>
    /// <param name="startsAt">Дата и время начала бронирования.</param>
    /// <param name="endsAt">Дата и время окончания бронирования.</param>
    /// <returns>Общая стоимость аренды, округлённая до 2 знаков.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если hourlyRate &lt;= 0.</exception>
    /// <exception cref="ArgumentException">Если endsAt &lt;= startsAt.</exception>
    /// <remarks>
    /// Время конвертируется в локальное по часовому поясу из PricingOptions.TimeZoneId.
    /// Корректно обрабатывает переход на летнее/зимнее время (DST).
    /// </remarks>
    public decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hourlyRate);
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(endsAt));
        }

        DateTime currentLocal = TimeZoneInfo.ConvertTime(startsAt, businessTimeZone).DateTime;
        DateTime endLocal = TimeZoneInfo.ConvertTime(endsAt, businessTimeZone).DateTime;

        decimal total = 0m;

        while (currentLocal < endLocal)
        {
            DateTime nextLocal = NextBoundary(currentLocal);
            if (nextLocal > endLocal)
            {
                nextLocal = endLocal;
            }

            decimal hours = (decimal)(ToInstant(nextLocal) - ToInstant(currentLocal)).TotalHours;
            total += hourlyRate * hours * GetMultiplier(currentLocal.TimeOfDay);
            currentLocal = nextLocal;
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private DateTimeOffset ToInstant(DateTime localTime) =>
        new(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), businessTimeZone.GetUtcOffset(localTime));

    private decimal GetMultiplier(TimeSpan time) =>
        time >= TimeSpan.FromHours(peakStartHour) && time < TimeSpan.FromHours(peakEndHour) ? peakMultiplier
        : time >= TimeSpan.FromHours(standardEndHour) && time < TimeSpan.FromHours(eveningDiscountEndHour) ? eveningDiscountMultiplier
        : time >= TimeSpan.FromHours(morningDiscountStartHour) && time < TimeSpan.FromHours(standardStartHour) ? morningDiscountMultiplier
        : time >= TimeSpan.FromHours(eveningDiscountEndHour) || time < TimeSpan.FromHours(morningDiscountStartHour) ? nightMultiplier
        : standardMultiplier;

    private DateTime NextBoundary(DateTime value)
    {
        IEnumerable<DateTime> boundaries = new[]
            {
                morningDiscountStartHour, standardStartHour, peakStartHour,
                peakEndHour, standardEndHour, eveningDiscountEndHour, 24
            }
            .Select(hour => value.Date.AddHours(hour));

        // The list always ends with hour 24 (midnight of the next day), which is strictly later
        // than any wall-clock time within `value`'s own day — so this always finds a match.
        // There is deliberately no "not found" fallback: one would be dead code that could only
        // mask a real bug in the boundary list above.
        return boundaries.First(boundary => boundary > value);
    }

    private static void ValidateHourBoundary(int hour, string name)
    {
        if (hour < 0 || hour > 24)
        {
            throw new InvalidOperationException($"Pricing:{name} must be between 0 and 24, but was {hour}.");
        }
    }
}
