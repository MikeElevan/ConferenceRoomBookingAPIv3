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
        CapturingBookingTransactionExecutor executor = new();
        BookingService subject = new(new RoomRepository(room), bookings, executor, new FixedPricingService());

        Booking booking = await subject.CreateAsync(roomId, DateTimeOffset.UtcNow.AddHours(1), 60, new[] { serviceId });

        // Verify the snapshot captured the service details at creation time
        BookingServiceSnapshot snapshot = Assert.Single(booking.Services);
        Assert.Equal(booking.Id, snapshot.BookingId);
        Assert.Equal(serviceId, snapshot.ServiceId);
        Assert.Equal("Projector", snapshot.Name);
        Assert.Equal(500m, snapshot.Price);
        Assert.Equal(500m, booking.ServicesCost);
        Assert.Same(booking, executor.CapturedBooking);
    }

    [Fact]
    public async Task CreateAsync_ReturnsExistingBooking_WhenIdempotencyKeyMatches()
    {
        Guid roomId = Guid.NewGuid();
        Guid serviceId = Guid.NewGuid();
        RoomService service = new() { Id = serviceId, Name = "Projector", Price = 500m };
        ConferenceRoom room = new()
        {
            Id = roomId, Name = "Room", Capacity = 1, BaseHourlyRate = 1000m,
            Services = new List<RoomService> { service }
        };
        string idempotencyKey = "unique-key-123";

        // First booking
        Booking firstBooking = new()
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            StartsAt = DateTimeOffset.UtcNow.AddHours(1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(2),
            IdempotencyKey = idempotencyKey,
            ServicesCost = 500m,
            RoomCost = 1000m
        };

        IdempotentBookingRepository bookings = new(firstBooking);
        CapturingBookingTransactionExecutor executor = new();
        BookingService subject = new(new RoomRepository(room), bookings, executor, new FixedPricingService());

        // Second request with same idempotency key
        Booking result = await subject.CreateAsync(roomId, DateTimeOffset.UtcNow.AddHours(5), 60, new[] { serviceId }, idempotencyKey);

        Assert.Equal(firstBooking.Id, result.Id);
        Assert.Equal(idempotencyKey, result.IdempotencyKey);
        Assert.Single(bookings.AllBookings); // No duplicate created
    }

    [Fact]
    public async Task CreateAsync_ReturnsFirstCommittedBooking_WhenExecutorResolvesIdempotencyRace()
    {
        Guid roomId = Guid.NewGuid();
        Guid serviceId = Guid.NewGuid();
        RoomService service = new() { Id = serviceId, Name = "Projector", Price = 500m };
        ConferenceRoom room = new()
        {
            Id = roomId, Name = "Room", Capacity = 1, BaseHourlyRate = 1000m,
            Services = new List<RoomService> { service }
        };
        string idempotencyKey = "race-key-456";

        // The winner committed before this request reached the executor; the executor (which in
        // SQL Server detects the unique-index collision) returns the already-committed booking.
        Booking firstCommitted = new()
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            StartsAt = DateTimeOffset.UtcNow.AddHours(1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(2),
            IdempotencyKey = idempotencyKey,
            RoomCost = 1000m,
            ServicesCost = 500m
        };
        RaceResolvingBookingTransactionExecutor executor = new(firstCommitted);
        BookingService subject = new(new RoomRepository(room), new CapturingBookingRepository(), executor, new FixedPricingService());

        Booking result = await subject.CreateAsync(roomId, DateTimeOffset.UtcNow.AddHours(3), 60, new[] { serviceId }, idempotencyKey);

        Assert.Equal(firstCommitted.Id, result.Id);
        Assert.Equal(idempotencyKey, result.IdempotencyKey);
    }

    private sealed class RaceResolvingBookingTransactionExecutor(Booking existing) : IBookingTransactionExecutor
    {
        public Task<Booking?> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
            Task.FromResult<Booking?>(existing);
    }

    private sealed class FixedPricingService : IPricingService
    {
        public decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt) => hourlyRate;
    }

    private sealed class CapturingBookingRepository : IBookingRepository
    {
        public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
        public Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Booking?>(null);
        public Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Booking?>(null);
        public Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoomBookingStats>>(Array.Empty<RoomBookingStats>());
        public Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServiceStats>>(Array.Empty<ServiceStats>());
    }

    private sealed class CapturingBookingTransactionExecutor : IBookingTransactionExecutor
    {
        public Booking? CapturedBooking { get; private set; }
        public Task<Booking?> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            CapturedBooking = booking;
            return Task.FromResult<Booking?>(booking);
        }
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

    private sealed class IdempotentBookingRepository : IBookingRepository
    {
        private readonly List<Booking> bookings = new();

        public IdempotentBookingRepository(Booking existingBooking)
        {
            bookings.Add(existingBooking);
        }

        public IReadOnlyList<Booking> AllBookings => bookings;

        public Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(bookings.Where(b => b.RoomId == roomId).ToList());

        public Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>(bookings.Where(b => b.StartsAt < to && b.EndsAt > from).ToList());

        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(bookings.FirstOrDefault(b => b.Id == id));

        public Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(bookings.FirstOrDefault(b => b.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoomBookingStats>>(Array.Empty<RoomBookingStats>());
        public Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServiceStats>>(Array.Empty<ServiceStats>());
    }
}
