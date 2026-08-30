using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Services;

public sealed class ReportService(IConferenceRoomRepository roomRepository, IBookingRepository bookingRepository)
{
    private const int MaximumRangeDays = 366;

    public async Task<BookingReportResponse> GetBookingReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(to));
        }
        if (to - from > TimeSpan.FromDays(MaximumRangeDays))
        {
            throw new ArgumentException($"The report range must not exceed {MaximumRangeDays} days.", nameof(to));
        }

        IReadOnlyList<ConferenceRoom> rooms = await roomRepository.GetRoomsAsync(cancellationToken);
        List<Booking> selectedBookings = (await bookingRepository.GetBookingsInRangeAsync(from, to, cancellationToken)).ToList();
        List<RoomReportResponse> roomReports = rooms.Select(room => CreateRoomReport(room, selectedBookings, from, to)).ToList();
        Dictionary<Guid, ServiceReportResponse> serviceReports = new Dictionary<Guid, ServiceReportResponse>();

        foreach (Booking booking in selectedBookings)
        {
            foreach (BookingServiceSnapshot service in booking.Services)
            {
                if (!serviceReports.TryGetValue(service.ServiceId, out ServiceReportResponse? report))
                {
                    report = new ServiceReportResponse(service.ServiceId, service.Name, 0, 0m);
                }
                serviceReports[service.ServiceId] = report with
                {
                    UsageCount = report.UsageCount + 1,
                    Revenue = report.Revenue + service.Price
                };
            }
        }

        double totalAvailableHours = roomReports.Sum(item => (to - from).TotalHours);
        double utilizationPercent = totalAvailableHours == 0d ? 0d :
            Math.Round(roomReports.Sum(item => item.BookedHours) / totalAvailableHours * 100d, 2);
        decimal revenue = selectedBookings.Sum(booking => booking.RoomCost + booking.ServicesCost);
        return new BookingReportResponse(from, to, selectedBookings.Count, revenue, utilizationPercent,
            roomReports, serviceReports.Values.OrderByDescending(item => item.Revenue).ToList());
    }

    private static RoomReportResponse CreateRoomReport(ConferenceRoom room, IReadOnlyList<Booking> bookings, DateTimeOffset from, DateTimeOffset to)
    {
        List<Booking> roomBookings = bookings.Where(booking => booking.RoomId == room.Id).ToList();
        double bookedHours = roomBookings.Sum(booking => GetOverlap(booking.StartsAt, booking.EndsAt, from, to).TotalHours);
        double availableHours = (to - from).TotalHours;
        decimal revenue = roomBookings.Sum(booking => booking.RoomCost + booking.ServicesCost);
        return new RoomReportResponse(room.Id, room.Name, roomBookings.Count, revenue, bookedHours,
            availableHours == 0d ? 0d : Math.Round(bookedHours / availableHours * 100d, 2));
    }

    private static TimeSpan GetOverlap(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset from, DateTimeOffset to)
    {
        DateTimeOffset overlapStart = startsAt > from ? startsAt : from;
        DateTimeOffset overlapEnd = endsAt < to ? endsAt : to;
        return overlapEnd > overlapStart ? overlapEnd - overlapStart : TimeSpan.Zero;
    }
}
