using ConferenceRoomBookingAPIv3.Application.Interfaces;
using ConferenceRoomBookingAPIv3.Application.Services;
using ConferenceRoomBookingAPIv3.DomainModels;
using Xunit;

namespace ConferenceRoomBookingAPIv3.UnitTests;

public sealed class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_CopiesSelectedServiceDetailsIntoBookingSnapshot()
    {
        Guid roomId = Guid.NewGuid();
        Guid serviceId = Guid.NewGuid();
        RoomService service = new() { Id = serviceId, Name = "Projector", Price = 500m };
        ConferenceRoom room = new()
        {
            Id = roomId, Name = "Room", Capacity = 1, BaseHourlyRate = 1000m,
            Services = new List<RoomService> { service }
        };
        CapturingBookingRepository bookings = new();
        BookingService subject = new(new RoomRepository(room), bookings, new FixedPricingService());

        Booking booking = await subject.CreateAsync(roomId, DateTimeOffset.UtcNow.AddHours(1), 60, new[] { serviceId });
        service.Name = "Renamed projector";
        service.Price = 900m;

        BookingServiceSnapshot snapshot = Assert.Single(booking.Services);
        Assert.Equal(booking.Id, snapshot.BookingId);
        Assert.Equal(serviceId, snapshot.ServiceId);
        Assert.Equal("Projector", snapshot.Name);
        Assert.Equal(500m, snapshot.Price);
        Assert.Equal(500m, booking.ServicesCost);
        Assert.Same(booking, bookings.LastBooking);
    }

    private sealed class FixedPricingService : IPricingService
    {
        public decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt) => hourlyRate;
    }

    private sealed class CapturingBookingRepository : IBookingRepository
    {
        public Booking? LastBooking { get; private set; }
        public Task<bool> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            LastBooking = booking;
            return Task.FromResult(true);
        }
        public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
        public Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
    }

    private sealed class RoomRepository(ConferenceRoom room) : IConferenceRoomRepository
    {
        public Task<ConferenceRoom?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == room.Id ? room : null);
        public Task<IReadOnlyList<ConferenceRoom>> GetRoomsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConferenceRoom>>(new[] { room });
        public Task<IReadOnlyList<ConferenceRoom>> GetAvailableRoomsAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConferenceRoom>>(new[] { room });
        public Task<ConferenceRoom> AddRoomAsync(ConferenceRoom value, CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task<bool> PatchRoomAsync(Guid id, Action<ConferenceRoom> patch, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteRoomAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
