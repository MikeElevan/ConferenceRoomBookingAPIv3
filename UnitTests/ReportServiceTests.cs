using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;
using Xunit;

namespace ConferenceRoomBookingAPIv3.UnitTests;

public sealed class ReportServiceTests
{
    private static readonly DateTimeOffset RangeStart = DateTimeOffset.Parse("2024-09-01T00:00:00+00:00");
    private static readonly DateTimeOffset RangeEnd = DateTimeOffset.Parse("2024-09-02T00:00:00+00:00");

    private static Booking CreateBooking(Guid roomId, DateTimeOffset startsAt, DateTimeOffset endsAt, decimal roomCost = 1000m, decimal servicesCost = 100m) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        StartsAt = startsAt,
        EndsAt = endsAt,
        RoomCost = roomCost,
        ServicesCost = servicesCost,
        Services = new List<BookingServiceSnapshot>()
    };

    private static Booking CreateBookingWithServices(Guid roomId, DateTimeOffset startsAt, DateTimeOffset endsAt, decimal roomCost, params BookingServiceSnapshot[] services) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        StartsAt = startsAt,
        EndsAt = endsAt,
        RoomCost = roomCost,
        ServicesCost = services.Sum(s => s.Price),
        Services = services.ToList()
    };

    private static ConferenceRoom CreateRoom(Guid id, string name) => new()
    {
        Id=id,
        Name=name,
        Capacity=10,
        BaseHourlyRate=1000m,
        Services=new List<RoomService>()
    };

    [Fact]
    public async Task GetBookingReportAsync_ReturnsEmptyReport_WhenNoRoomsAndNoBookings()
    {
        ReportService subject = new(new FakeRoomRepository(Array.Empty<ConferenceRoom>()), new FakeBookingRepository(Array.Empty<Booking>()));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(0, report.BookingCount);
        Assert.Empty(report.Rooms);
        Assert.Empty(report.Services);
    }

    [Fact]
    public async Task GetBookingReportAsync_ReturnsZeroUtilization_WhenRoomHasNoBookings()
    {
        ConferenceRoom room = CreateRoom(Guid.NewGuid(), "Room A");
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(Array.Empty<Booking>()));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        RoomReportResponse roomReport = Assert.Single(report.Rooms);
        Assert.Equal(0d, roomReport.BookedHours);
        Assert.Equal(0d, roomReport.UtilizationPercent);
        Assert.Equal(0d, report.UtilizationPercent);
    }

    [Fact]
    public async Task GetBookingReportAsync_ReturnsCorrectBookingCount()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking1 = CreateBooking(roomId, RangeStart.AddHours(10), RangeStart.AddHours(12));
        Booking booking2 = CreateBooking(roomId, RangeStart.AddHours(14), RangeStart.AddHours(16));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking1, booking2 }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(2, report.BookingCount);
    }

    [Theory]
    [InlineData(10, 14, 4d)]
    [InlineData(0, 24, 24d)]
    [InlineData(0, 12, 12d)]
    [InlineData(12, 24, 12d)]
    public async Task GetBookingReportAsync_CalculatesBookedHours_WhenBookingFullyWithinRange(int startHour, int endHour, double expectedHours)
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart.AddHours(startHour), RangeStart.AddHours(endHour));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(expectedHours, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_ClipsToRangeStart_WhenBookingStartsBeforeRange()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart.AddHours(-2), RangeStart.AddHours(2));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(2d, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_ClipsToRangeEnd_WhenBookingEndsAfterRange()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeEnd.AddHours(-2), RangeEnd.AddHours(2));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(2d, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_CoversEntireRange_WhenBookingExtendsBeyondBothBoundaries()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart.AddHours(-5), RangeEnd.AddHours(5));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(24d, report.Rooms[0].BookedHours);
    }

    [Theory]
    [InlineData(-10, -5)]
    [InlineData(25, 30)]
    public async Task GetBookingReportAsync_ReturnsZeroBookedHours_WhenBookingDoesNotOverlapRange(int startHour, int endHour)
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart.AddHours(startHour), RangeStart.AddHours(endHour));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(0d, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_ReturnsZeroBookedHours_WhenBookingEndsExactlyAtRangeStart()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart.AddHours(-5), RangeStart);
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(0d, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_ReturnsZeroBookedHours_WhenBookingStartsExactlyAtRangeEnd()
    {
        ReportService subject = new(new FakeRoomRepository(Array.Empty<ConferenceRoom>()), new FakeBookingRepository(Array.Empty<Booking>()));
        await Assert.ThrowsAsync<ArgumentException>(() => subject.GetBookingReportAsync(RangeEnd, RangeStart));
    }

    [Fact]
    public async Task GetBookingReportAsync_Returns100PercentUtilization_WhenRoomFullyBooked()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart, RangeEnd);
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(100d, report.Rooms[0].UtilizationPercent);
    }

    [Fact]
    public async Task GetBookingReportAsync_Returns50PercentUtilization_WhenRoomHalfBooked()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeStart, RangeStart.AddHours(12));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(50d, report.Rooms[0].UtilizationPercent);
    }

    [Fact]
    public async Task GetBookingReportAsync_CalculatesUtilizationAcrossMultipleRooms()
    {
        Guid roomId1 = Guid.NewGuid();
        Guid roomId2 = Guid.NewGuid();
        ConferenceRoom room1 = CreateRoom(roomId1, "Room A");
        ConferenceRoom room2 = CreateRoom(roomId2, "Room B");
        Booking booking1 = CreateBooking(roomId1, RangeStart, RangeStart.AddHours(6));
        Booking booking2 = CreateBooking(roomId2, RangeStart, RangeStart.AddHours(6));
        ReportService subject = new(new FakeRoomRepository(new[] { room1, room2 }), new FakeBookingRepository(new[] { booking1, booking2 }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(25d, report.Rooms[0].UtilizationPercent);
        Assert.Equal(25d, report.Rooms[1].UtilizationPercent);
        Assert.Equal(25d, report.UtilizationPercent);
    }

    [Fact]
    public async Task GetBookingReportAsync_ExcludesBookingsOutsideRange()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking bookingInRange = CreateBooking(roomId, RangeStart, RangeStart.AddHours(6));
        Booking bookingOutOfRange = CreateBooking(roomId, RangeEnd.AddHours(1), RangeEnd.AddHours(7));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { bookingInRange, bookingOutOfRange }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(6d, report.Rooms[0].BookedHours);
        Assert.Equal(1, report.Rooms[0].BookingCount);
    }

    [Fact]
    public async Task GetBookingReportAsync_CalculatesTotalRevenue_IncludingRoomAndServicesCost()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBookingWithServices(
            roomId, RangeStart, RangeStart.AddHours(2), 2000m,
            new BookingServiceSnapshot { ServiceId = Guid.NewGuid(), Name = "Projector", Price = 500m },
            new BookingServiceSnapshot { ServiceId = Guid.NewGuid(), Name = "WiFi", Price = 100m });
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(2600m, report.Revenue);
        Assert.Equal(2600m, report.Rooms[0].Revenue);
    }

    [Fact]
    public async Task GetBookingReportAsync_AggregatesServiceUsageAcrossBookings()
    {
        Guid serviceId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking1 = CreateBookingWithServices(
            roomId, RangeStart, RangeStart.AddHours(1), 1000m,
            new BookingServiceSnapshot { ServiceId = serviceId, Name = "Projector", Price = 500m });
        Booking booking2 = CreateBookingWithServices(
            roomId, RangeStart.AddHours(2), RangeStart.AddHours(3), 1000m,
            new BookingServiceSnapshot { ServiceId = serviceId, Name = "Projector", Price = 500m });
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking1, booking2 }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        ServiceReportResponse serviceReport = Assert.Single(report.Services);
        Assert.Equal(2, serviceReport.UsageCount);
        Assert.Equal(1000m, serviceReport.Revenue);
    }

    [Fact]
    public async Task GetBookingReportAsync_OrdersServicesByRevenueDescending()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Guid cheapServiceId = Guid.NewGuid();
        Guid expensiveServiceId = Guid.NewGuid();
        Booking booking = CreateBookingWithServices(
            roomId, RangeStart, RangeStart.AddHours(1), 1000m,
            new BookingServiceSnapshot { ServiceId = cheapServiceId, Name = "WiFi", Price = 100m },
            new BookingServiceSnapshot { ServiceId = expensiveServiceId, Name = "Catering", Price = 5000m });
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(2, report.Services.Count);
        Assert.Equal("Catering", report.Services[0].ServiceName);
        Assert.Equal(5000m, report.Services[0].Revenue);
        Assert.Equal("WiFi", report.Services[1].ServiceName);
        Assert.Equal(100m, report.Services[1].Revenue);
    }

    [Fact]
    public async Task GetBookingReportAsync_BookingAfterRange_CountedAsZeroBookedHours()
    {
        Guid roomId = Guid.NewGuid();
        ConferenceRoom room = CreateRoom(roomId, "Room A");
        Booking booking = CreateBooking(roomId, RangeEnd, RangeEnd.AddHours(5));
        ReportService subject = new(new FakeRoomRepository(new[] { room }), new FakeBookingRepository(new[] { booking }));
        BookingReportResponse report = await subject.GetBookingReportAsync(RangeStart, RangeEnd);
        Assert.Equal(0d, report.Rooms[0].BookedHours);
    }

    [Fact]
    public async Task GetBookingReportAsync_ThrowsWhenRangeExceedsMaximum()
    {
        ReportService subject = new(new FakeRoomRepository(Array.Empty<ConferenceRoom>()), new FakeBookingRepository(Array.Empty<Booking>()));
        await Assert.ThrowsAsync<ArgumentException>(() => subject.GetBookingReportAsync(RangeStart, RangeStart.AddDays(367)));
    }

    private sealed class FakeRoomRepository : IConferenceRoomRepository
    {
        private readonly IReadOnlyList<ConferenceRoom> rooms;
        public FakeRoomRepository(IReadOnlyList<ConferenceRoom> rooms) => this.rooms = rooms;
        public Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(rooms.FirstOrDefault(r => r.Id == id));
        public Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default) => Task.FromResult(rooms);
        public Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default) => Task.FromResult(rooms);
        public Task<ConferenceRoom> AddRoomAsync(ConferenceRoom value, CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        private readonly IReadOnlyList<Booking> bookings;
        public FakeBookingRepository(IReadOnlyList<Booking> bookings) => this.bookings = bookings;
        public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(bookings.Where(b => b.RoomId == roomId).ToList());
        public Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(bookings.Where(b => b.StartsAt < to && b.EndsAt > from).ToList());
        public Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(bookings.FirstOrDefault(b => b.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RoomBookingStats> stats = bookings
                .Where(b => b.StartsAt < to && from < b.EndsAt)
                .GroupBy(b => b.RoomId)
                .Select(g => new RoomBookingStats(
                    g.Key,
                    g.Count(),
                    g.Sum(b => b.RoomCost + b.ServicesCost),
                    g.Sum(b => GetOverlapHours(b.StartsAt, b.EndsAt, from, to))))
                .ToList();
            return Task.FromResult<IReadOnlyList<RoomBookingStats>>(stats);
        }

        public Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ServiceStats> stats = bookings
                .Where(b => b.StartsAt < to && from < b.EndsAt)
                .SelectMany(b => b.Services)
                .GroupBy(s => s.ServiceId)
                .Select(g => new ServiceStats(
                    g.Key,
                    g.First().Name,
                    g.Count(),
                    g.Sum(s => s.Price)))
                .ToList();
            return Task.FromResult<IReadOnlyList<ServiceStats>>(stats);
        }

        private static double GetOverlapHours(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset from, DateTimeOffset to)
        {
            DateTimeOffset overlapStart = startsAt > from ? startsAt : from;
            DateTimeOffset overlapEnd = endsAt < to ? endsAt : to;
            return overlapEnd > overlapStart ? (overlapEnd - overlapStart).TotalHours : 0d;
        }
    }
}