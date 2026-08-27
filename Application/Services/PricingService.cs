using ConferenceRoomBookingAPIv3.Application.Interfaces;

namespace ConferenceRoomBookingAPIv3.Application.Services;

public sealed class PricingService : IPricingService
{
    public decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hourlyRate);
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(endsAt));
        }

        decimal total = 0m;
        DateTimeOffset current = startsAt;

        while (current < endsAt)
        {
            DateTimeOffset next = NextBoundary(current);
            if (next > endsAt)
            {
                next = endsAt;
            }

            decimal hours = (decimal)(next - current).TotalHours;
            total += hourlyRate * hours * GetMultiplier(current.TimeOfDay);
            current = next;
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal GetMultiplier(TimeSpan time) =>
        time >= TimeSpan.FromHours(12) && time < TimeSpan.FromHours(14) ? 1.15m
        : time >= TimeSpan.FromHours(18) && time < TimeSpan.FromHours(23) ? 0.80m
        : time >= TimeSpan.FromHours(6) && time < TimeSpan.FromHours(9) ? 0.90m
        : 1m;

    private static DateTimeOffset NextBoundary(DateTimeOffset value)
    {
        var boundaries = new int[] { 6, 9, 12, 14, 18, 23, 24 }
            .Select(hour => value.Date.AddHours(hour));
        DateTime next = boundaries.FirstOrDefault(boundary => boundary > value.DateTime);

        return next == default
            ? new DateTimeOffset(value.Date.AddDays(1), value.Offset)
            : new DateTimeOffset(next, value.Offset);
    }
}
