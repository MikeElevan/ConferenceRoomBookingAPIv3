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
}
