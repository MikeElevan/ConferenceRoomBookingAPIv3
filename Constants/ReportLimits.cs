namespace ConferenceRoomBookingAPIv3.Constants;

public static class ReportLimits
{
    /// <summary>
    /// Upper bound on how wide a report's [From, To) range may be. Enforced in two places on
    /// purpose: <see cref="ConferenceRoomBookingAPIv3.Contracts.RequestModels.ReportRequest"/>
    /// for a fast, specific 400 at the API boundary, and
    /// <see cref="ConferenceRoomBookingAPIv3.Application.Services.ReportService"/> for callers
    /// that reach the service without going through that DTO. Both read this constant so the
    /// two checks can't drift apart.
    /// </summary>
    public const int MaximumRangeDays = 366;
}
