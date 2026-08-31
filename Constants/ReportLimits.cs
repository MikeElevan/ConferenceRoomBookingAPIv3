namespace ConferenceRoomBookingAPIv3.Constants;

/// <summary>
/// Ограничения бизнес-логики.
/// </summary>
public static class ReportLimits
{
    /// <summary>
    /// Максимальная ширина диапазона отчёта в днях. Проверяется в двух местах:
    /// <see cref="ConferenceRoomBookingAPIv3.Contracts.RequestModels.ReportRequest"/> —
    /// для быстрого 400 на границе API, и
    /// <see cref="ConferenceRoomBookingAPIv3.Application.Services.ReportService"/> — для вызовов,
    /// минующих DTO. Оба места читают эту константу, чтобы проверки не расходились.
    /// </summary>
    public const int MaximumRangeDays = 366;
}
