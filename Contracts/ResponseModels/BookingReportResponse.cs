namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

public sealed record BookingReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int BookingCount,
    decimal Revenue,
    double UtilizationPercent,
    IReadOnlyList<RoomReportResponse> Rooms,
    IReadOnlyList<ServiceReportResponse> Services);

public sealed record RoomReportResponse(Guid RoomId, string RoomName, int BookingCount, decimal Revenue, double BookedHours, double UtilizationPercent);

public sealed record ServiceReportResponse(Guid ServiceId, string ServiceName, int UsageCount, decimal Revenue);