using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Services;

/// <summary>
/// Сервис построения отчётов по бронированиям.
/// Генерирует статистику использования залов и выручки по услугам за указанный период.
/// Использует агрегацию на стороне БД для минимального потребления памяти.
/// </summary>
public sealed class ReportService(IConferenceRoomRepository roomRepository, IBookingRepository bookingRepository)
{
    /// <summary>
    /// Получить отчёт по бронированиям за указанный период.
    /// </summary>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода (не более 366 дней).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Отчёт с общей статистикой, детализацией по залам и услугам.</returns>
    /// <exception cref="ArgumentException">Если to <= from или период превышает 366 дней.</exception>
    public async Task<BookingReportResponse> GetBookingReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(to));
        }
        if (to - from > TimeSpan.FromDays(ReportLimits.MaximumRangeDays))
        {
            throw new ArgumentException($"The report range must not exceed {ReportLimits.MaximumRangeDays} days.", nameof(to));
        }

        IReadOnlyList<ConferenceRoom> rooms = await roomRepository.GetRoomsAsync(cancellationToken);
        IReadOnlyList<RoomBookingStats> roomStats = await bookingRepository.GetRoomBookingStatsAsync(from, to, cancellationToken);
        IReadOnlyList<ServiceStats> serviceStats = await bookingRepository.GetServiceStatsAsync(from, to, cancellationToken);

        Dictionary<Guid, RoomBookingStats> roomStatsMap = roomStats.ToDictionary(s => s.RoomId);

        int totalBookingCount = roomStats.Sum(s => s.BookingCount);
        decimal totalRevenue = roomStats.Sum(s => s.Revenue);

        double totalAvailableHours = rooms.Count * (to - from).TotalHours;
        double totalBookedHours = roomStats.Sum(s => s.BookedHours);
        double utilizationPercent = totalAvailableHours == 0d ? 0d :
            Math.Round(totalBookedHours / totalAvailableHours * 100d, 2);

        List<RoomReportResponse> roomReports = rooms
            .Select(room => CreateRoomReport(room, roomStatsMap.TryGetValue(room.Id, out RoomBookingStats? stats) ? stats : null, from, to))
            .ToList();

        List<ServiceReportResponse> serviceReports = serviceStats
            .Select(s => new ServiceReportResponse(s.ServiceId, s.ServiceName, s.UsageCount, s.Revenue))
            .OrderByDescending(item => item.Revenue)
            .ToList();

        return new BookingReportResponse(from, to, totalBookingCount, totalRevenue, utilizationPercent,
            roomReports, serviceReports);
    }

    private static RoomReportResponse CreateRoomReport(ConferenceRoom room, RoomBookingStats? stats, DateTimeOffset from, DateTimeOffset to)
    {
        int bookingCount = stats?.BookingCount ?? 0;
        decimal revenue = stats?.Revenue ?? 0m;
        double bookedHours = stats?.BookedHours ?? 0d;
        double availableHours = (to - from).TotalHours;
        double utilizationPercent = availableHours == 0d ? 0d : Math.Round(bookedHours / availableHours * 100d, 2);

        return new RoomReportResponse(room.Id, room.Name, bookingCount, revenue, bookedHours, utilizationPercent);
    }
}
