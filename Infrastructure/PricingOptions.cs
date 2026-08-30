namespace ConferenceRoomBookingAPIv3.Infrastructure;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>
    /// IANA time zone identifier that defines what "12:00", "18:00" etc. mean for the pricing
    /// windows in <see cref="ConferenceRoomBookingAPIv3.Application.Services.PricingService"/>.
    /// This must be a fixed, server-controlled value — never derived from a client-supplied
    /// offset — or a caller could pick a favorable discount window just by choosing the offset
    /// on <c>StartsAt</c>/<c>EndsAt</c> in the booking request.
    /// </summary>
    public string TimeZoneId { get; init; } = "Europe/Kyiv";

    // Window boundaries and multipliers are business policy, not implementation detail — they
    // live in config for the same reason TimeZoneId does: someone will change them without a
    // recompile long before anyone changes how the loop that applies them works.

    public int MorningDiscountStartHour { get; init; } = 6;
    public int StandardStartHour { get; init; } = 9;
    public int PeakStartHour { get; init; } = 12;
    public int PeakEndHour { get; init; } = 14;
    public int StandardEndHour { get; init; } = 18;
    public int EveningDiscountEndHour { get; init; } = 23;

    public decimal MorningDiscountMultiplier { get; init; } = 0.90m;
    public decimal StandardMultiplier { get; init; } = 1.00m;
    public decimal PeakMultiplier { get; init; } = 1.15m;
    public decimal EveningDiscountMultiplier { get; init; } = 0.80m;

    /// <summary>
    /// Rate applied outside all other windows, i.e. EveningDiscountEndHour–MorningDiscountStartHour
    /// (23:00–06:00 by default). The spec this project was built from never says what a night
    /// booking should cost — defaulting this to the standard rate keeps that an explicit,
    /// named, overridable decision instead of an accident of whichever branch happened to be
    /// the ternary chain's fallback.
    /// </summary>
    public decimal NightMultiplier { get; init; } = 1.00m;
}
