namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

/// <summary>
/// Ответ с отчётом по бронированиям за период.
/// </summary>
/// <param name="From">Начало периода.</param>
/// <param name="To">Конец периода.</param>
/// <param name="BookingCount">Количество бронирований.</param>
/// <param name="Revenue">Общая выручка.</param>
/// <param name="UtilizationPercent">Процент утилизации залов.</param>
/// <param name="Rooms">Детализация по залам.</param>
/// <param name="Services">Детализация по услугам.</param>
public sealed record BookingReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int BookingCount,
    decimal Revenue,
    double UtilizationPercent,
    IReadOnlyList<RoomReportResponse> Rooms,
    IReadOnlyList<ServiceReportResponse> Services);

/// <summary>
/// Статистика по конкретному залу в отчёте.
/// </summary>
/// <param name="RoomId">Идентификатор зала.</param>
/// <param name="RoomName">Название зала.</param>
/// <param name="BookingCount">Количество бронирований.</param>
/// <param name="Revenue">Выручка.</param>
/// <param name="BookedHours">Забронированных часов.</param>
/// <param name="UtilizationPercent">Процент утилизации.</param>
public sealed record RoomReportResponse(Guid RoomId, string RoomName, int BookingCount, decimal Revenue, double BookedHours, double UtilizationPercent);

/// <summary>
/// Статистика по конкретной услуге в отчёте.
/// </summary>
/// <param name="ServiceId">Идентификатор услуги.</param>
/// <param name="ServiceName">Название услуги.</param>
/// <param name="UsageCount">Количество использований.</param>
/// <param name="Revenue">Выручка.</param>
public sealed record ServiceReportResponse(Guid ServiceId, string ServiceName, int UsageCount, decimal Revenue);