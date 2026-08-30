using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConferenceRoomBookingAPIv3.IntegrationTests;

public sealed class PricingServiceTests
{
    private readonly PricingService pricingService =
        new(Options.Create(new PricingOptions { TimeZoneId = "Europe/Kyiv" }));

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

        Assert.Equal(2000m + 2300m, cost);
    }

    [Fact]
    public void CalculateRoomCost_IsInvariantToTheOffsetOfTheSameInstant()
    {
        DateTimeOffset kyivInstant = DateTimeOffset.Parse("2024-09-02T13:00:00+03:00");
        DateTimeOffset sameInstantDifferentOffset = kyivInstant.ToOffset(TimeSpan.FromHours(14));

        decimal costFromKyivOffset = pricingService.CalculateRoomCost(2000m, kyivInstant, kyivInstant.AddHours(1));
        decimal costFromSpoofedOffset = pricingService.CalculateRoomCost(
            2000m, sameInstantDifferentOffset, sameInstantDifferentOffset.AddHours(1));

        Assert.Equal(costFromKyivOffset, costFromSpoofedOffset);
        Assert.Equal(2300m, costFromSpoofedOffset);
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
}
