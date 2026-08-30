using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Application.Services;

public sealed class PricingService : IPricingService
{
    // Named per Robert C. Martin's guidance to replace magic numbers with intention-revealing
    // constants — these are exactly the values the pricing rules in the spec are most likely to
    // change, so they get one place to change instead of a scattered ternary chain.
    private const int MorningDiscountStartHour = 6;
    private const int StandardStartHour = 9;
    private const int PeakStartHour = 12;
    private const int PeakEndHour = 14;
    private const int StandardEndHour = 18;
    private const int EveningDiscountEndHour = 23;

    private const decimal PeakMultiplier = 1.15m;
    private const decimal EveningDiscountMultiplier = 0.80m;
    private const decimal MorningDiscountMultiplier = 0.90m;
    private const decimal StandardMultiplier = 1.00m;

    private readonly TimeZoneInfo businessTimeZone;

    public PricingService(IOptions<PricingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string timeZoneId = options.Value.TimeZoneId;
        try
        {
            businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Pricing:TimeZoneId '{timeZoneId}' is not a valid time zone identifier.", exception);
        }
    }

    public decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hourlyRate);
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(endsAt));
        }

        // Pricing windows ("peak hours are 12:00-14:00", etc.) are defined in the business's own
        // local time, not in whatever offset a client happens to send. Converting to a single,
        // server-controlled time zone here — before any window comparison — means a caller can no
        // longer shop for a discount by attaching a different offset to the same instant; the
        // request's original Offset is discarded entirely for pricing purposes.
        DateTimeOffset current = TimeZoneInfo.ConvertTime(startsAt, businessTimeZone);
        DateTimeOffset end = TimeZoneInfo.ConvertTime(endsAt, businessTimeZone);

        decimal total = 0m;

        while (current < end)
        {
            DateTimeOffset next = NextBoundary(current);
            if (next > end)
            {
                next = end;
            }

            decimal hours = (decimal)(next - current).TotalHours;
            total += hourlyRate * hours * GetMultiplier(current.TimeOfDay);
            current = next;
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal GetMultiplier(TimeSpan time) =>
        time >= TimeSpan.FromHours(PeakStartHour) && time < TimeSpan.FromHours(PeakEndHour) ? PeakMultiplier
        : time >= TimeSpan.FromHours(StandardEndHour) && time < TimeSpan.FromHours(EveningDiscountEndHour) ? EveningDiscountMultiplier
        : time >= TimeSpan.FromHours(MorningDiscountStartHour) && time < TimeSpan.FromHours(StandardStartHour) ? MorningDiscountMultiplier
        : StandardMultiplier;

    private static DateTimeOffset NextBoundary(DateTimeOffset value)
    {
        IEnumerable<DateTime> boundaries = new[]
            {
                MorningDiscountStartHour, StandardStartHour, PeakStartHour,
                PeakEndHour, StandardEndHour, EveningDiscountEndHour, 24
            }
            .Select(hour => value.Date.AddHours(hour));
        DateTime next = boundaries.FirstOrDefault(boundary => boundary > value.DateTime);

        return next == default
            ? new DateTimeOffset(value.Date.AddDays(1), value.Offset)
            : new DateTimeOffset(next, value.Offset);
    }
}
