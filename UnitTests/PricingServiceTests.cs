using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConferenceRoomBookingAPIv3.UnitTests;

public sealed class PricingServiceTests
{
    private readonly PricingService pricingService =
        new(Options.Create(new PricingOptions { TimeZoneId="Europe/Kyiv" }));

    [Theory]
    [InlineData("2024-09-02T09:00:00+03:00", 1, 2000, 2000)]   // standard hours, no adjustment
    [InlineData("2024-09-02T12:00:00+03:00", 1, 2000, 2300)]   // peak hours, +15%
    [InlineData("2024-09-02T18:00:00+03:00", 1, 2000, 1600)]   // evening hours, -20%
    [InlineData("2024-09-02T06:00:00+03:00", 1, 2000, 1800)]   // morning hours, -10%
    public void CalculateRoomCost_AppliesTheExpectedWindowMultiplier(
        string startsAt, int durationHours, decimal hourlyRate, decimal expectedCost)
    {
        DateTimeOffset start = DateTimeOffset.Parse(startsAt);
        DateTimeOffset end = start.AddHours(durationHours);

        decimal cost = pricingService.CalculateRoomCost(hourlyRate, start, end);

        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void CalculateRoomCost_SplitsAcrossMultipleWindowsProportionally()
    {
        // 11:00-13:00 Kyiv time: one hour standard (09-18, excluding peak) + one hour peak (12-14).
        DateTimeOffset start = DateTimeOffset.Parse("2024-09-02T11:00:00+03:00");
        DateTimeOffset end = start.AddHours(2);

        decimal cost = pricingService.CalculateRoomCost(2000m, start, end);

        Assert.Equal(2000m+2300m, cost);
    }

    [Fact]
    public void CalculateRoomCost_IsInvariantToTheOffsetOfTheSameInstant()
    {
        // Same absolute instant (13:00 Kyiv / peak hours), expressed with two different client
        // offsets. Before the fix, GetMultiplier read TimeOfDay straight off the client-supplied
        // DateTimeOffset, so a caller could pick a favorable window just by choosing the offset —
        // this pins down that the two must now price identically.
        DateTimeOffset kyivInstant = DateTimeOffset.Parse("2024-09-02T13:00:00+03:00");
        DateTimeOffset sameInstantDifferentOffset = kyivInstant.ToOffset(TimeSpan.FromHours(14));

        decimal costFromKyivOffset = pricingService.CalculateRoomCost(2000m, kyivInstant, kyivInstant.AddHours(1));
        decimal costFromSpoofedOffset = pricingService.CalculateRoomCost(
            2000m, sameInstantDifferentOffset, sameInstantDifferentOffset.AddHours(1));

        Assert.Equal(costFromKyivOffset, costFromSpoofedOffset);
        Assert.Equal(2300m, costFromSpoofedOffset); // still peak-priced, not discounted
    }

    [Fact]
    public void CalculateRoomCost_ThrowsWhenEndIsNotAfterStart()
    {
        DateTimeOffset start = DateTimeOffset.Parse("2024-09-02T10:00:00+03:00");

        Assert.Throws<ArgumentException>(() => pricingService.CalculateRoomCost(2000m, start, start));
    }

    [Fact]
    public void CalculateRoomCost_ThrowsForNonPositiveHourlyRate()
    {
        DateTimeOffset start = DateTimeOffset.Parse("2024-09-02T10:00:00+03:00");

        Assert.Throws<ArgumentOutOfRangeException>(() => pricingService.CalculateRoomCost(0m, start, start.AddHours(1)));
    }

    [Fact]
    public void CalculateRoomCost_AccountsForDaylightSavingFallBackWithinBooking()
    {
        // 2024-10-27 is when Europe/Kyiv falls back from EEST (+03:00) to EET (+02:00) at local
        // 04:00 → 03:00, so local 03:00-04:00 occurs twice and that calendar day has 25 real
        // hours, not 24. Booking 00:00-05:00 local stays inside the "night" window the whole way
        // (constant multiplier), so a wrong answer here can only come from getting the elapsed
        // *duration* wrong, not from picking the wrong window — exactly what a fixed-offset
        // implementation would get wrong.
        DateTimeOffset start = new(2024, 10, 27, 0, 0, 0, TimeSpan.FromHours(3));  // EEST, pre-transition
        DateTimeOffset end = new(2024, 10, 27, 5, 0, 0, TimeSpan.FromHours(2));    // EET, post-transition

        decimal cost = pricingService.CalculateRoomCost(1000m, start, end);

        // 6 real elapsed hours, not the naive wall-clock difference of 5 — the repeated
        // 03:00-04:00 hour must be counted once for real, not dropped.
        Assert.Equal(6000m, cost);
    }

    [Fact]
    public void CalculateRoomCost_AccountsForDaylightSavingSpringForwardWithinBooking()
    {
        // 2024-03-31: Europe/Kyiv springs forward from EET (+02:00) to EEST (+03:00) at local
        // 03:00 → 04:00, so local 03:00-04:00 never happens that day and the calendar day has
        // only 23 real hours. Same night-window trick as the fall-back test above: constant
        // multiplier throughout, isolating the duration calculation as the only thing being
        // tested.
        DateTimeOffset start = new(2024, 3, 31, 0, 0, 0, TimeSpan.FromHours(2));   // EET, pre-transition
        DateTimeOffset end = new(2024, 3, 31, 5, 0, 0, TimeSpan.FromHours(3));     // EEST, post-transition

        decimal cost = pricingService.CalculateRoomCost(1000m, start, end);

        // 4 real elapsed hours, not the naive wall-clock difference of 5 — the skipped
        // 03:00-04:00 hour must not be charged for.
        Assert.Equal(4000m, cost);
    }
}
