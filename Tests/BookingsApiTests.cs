using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ConferenceRoomBookingAPIv3.IntegrationTests;

public sealed class BookingsApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;

    public BookingsApiTests()
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task BookingReport_ReturnsRevenueAndUtilization()
    {
        DateTimeOffset from = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset to = DateTimeOffset.UtcNow.AddDays(1);
        string requestUri = $"/api/v1/reports/bookings?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

        HttpResponseMessage response = await client.GetAsync(requestUri);
        JsonElement report = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(report.GetProperty("bookingCount").GetInt32() >= 0);
        Assert.True(report.GetProperty("rooms").GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateBooking_WithAvailableRoom_ReturnsCreatedBooking()
    {
        (Guid roomId, Guid serviceId) = await GetRoomAndServiceAsync();
        DateTimeOffset startsAt = DateTimeOffset.UtcNow.AddHours(3);
        object request = CreateBookingRequest(roomId, startsAt, serviceId);

        using (HttpContent content = JsonContent.Create(request))
        {
            HttpResponseMessage response = await client.PostAsync("/api/v1/bookings", content);
            JsonElement booking = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(roomId, booking.GetProperty("roomId").GetGuid());
            Assert.Equal(booking.GetProperty("roomCost").GetDecimal() + booking.GetProperty("servicesCost").GetDecimal(), booking.GetProperty("totalCost").GetDecimal());
        }
    }

    [Fact]
    public async Task CreateBooking_WithOverlappingInterval_ReturnsConflict()
    {
        (Guid roomId, Guid serviceId) = await GetRoomAndServiceAsync();
        DateTimeOffset startsAt = DateTimeOffset.UtcNow.AddHours(4);

        using (HttpContent firstContent = JsonContent.Create(CreateBookingRequest(roomId, startsAt, serviceId)))
        {
            HttpResponseMessage firstResponse = await client.PostAsync("/api/v1/bookings", firstContent);
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        }

        using (HttpContent secondContent = JsonContent.Create(CreateBookingRequest(roomId, startsAt.AddMinutes(15), serviceId)))
        {
            HttpResponseMessage secondResponse = await client.PostAsync("/api/v1/bookings", secondContent);
            JsonElement error = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
            Assert.Equal("booking_conflict", error.GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task CreateBooking_WithUnknownRoom_ReturnsNotFound()
    {
        object request = CreateBookingRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(5), Guid.NewGuid());

        using (HttpContent content = JsonContent.Create(request))
        {
            HttpResponseMessage response = await client.PostAsync("/api/v1/bookings", content);
            JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("room_not_found", error.GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task CreateBooking_WithDuplicateServices_ReturnsBadRequest()
    {
        (Guid roomId, Guid serviceId) = await GetRoomAndServiceAsync();
        object request = CreateBookingRequest(roomId, DateTimeOffset.UtcNow.AddHours(6), serviceId, serviceId);

        using (HttpContent content = JsonContent.Create(request))
        {
            HttpResponseMessage response = await client.PostAsync("/api/v1/bookings", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    private async Task<(Guid RoomId, Guid ServiceId)> GetRoomAndServiceAsync()
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/rooms");
        JsonElement rooms = await response.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement room = rooms[0];
        Guid roomId = room.GetProperty("id").GetGuid();
        Guid serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();
        return (roomId, serviceId);
    }

    private static object CreateBookingRequest(Guid roomId, DateTimeOffset startsAt, params Guid[] serviceIds) => new
    {
        roomId,
        startsAt,
        durationMinutes = 60,
        serviceIds
    };

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}
