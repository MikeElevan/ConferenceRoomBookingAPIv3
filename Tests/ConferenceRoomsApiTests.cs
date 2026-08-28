using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ConferenceRoomBookingAPIv3.IntegrationTests;

public sealed class ConferenceRoomsApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;

    public ConferenceRoomsApiTests()
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllRooms_ReturnsSeededRooms()
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/rooms");
        JsonElement rooms = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, rooms.ValueKind);
        Assert.Equal(3, rooms.GetArrayLength());
    }

    [Fact]
    public async Task GetRoom_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/rooms/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePatchAndDeleteRoom_UsesExpectedHttpContract()
    {
        object createRequest = new
        {
            name = "Тестовый зал",
            capacity = 20,
            baseHourlyRate = 1200,
            services = new object[]
            {
                new { name = "Экран", price = 150 }
            }
        };

        using (HttpContent content = JsonContent.Create(createRequest))
        {
            HttpResponseMessage createResponse = await client.PostAsync("/api/v1/rooms", content);
            JsonElement createdRoom = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            Guid roomId = createdRoom.GetProperty("id").GetGuid();

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            object patchRequest = new
            {
                name = "Обновлённый зал",
                capacity = 30,
                baseHourlyRate = 1500
            };
            using (HttpContent patchContent = JsonContent.Create(patchRequest))
            {
                HttpResponseMessage patchResponse = await client.PatchAsync($"/api/v1/rooms/{roomId}", patchContent);
                Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);
            }

            HttpResponseMessage patchedRoomResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
            JsonElement patchedRoom = await patchedRoomResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(HttpStatusCode.OK, patchedRoomResponse.StatusCode);
            Assert.Equal("Обновлённый зал", patchedRoom.GetProperty("name").GetString());
            Assert.Equal(30, patchedRoom.GetProperty("capacity").GetInt32());
            Assert.Equal(1500m, patchedRoom.GetProperty("baseHourlyRate").GetDecimal());
            Assert.Equal(1, patchedRoom.GetProperty("services").GetArrayLength());

            HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/rooms/{roomId}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }

    [Fact]
    public async Task PutRoom_IsNotSupported()
    {
        using HttpContent content = JsonContent.Create(new { });
        HttpResponseMessage response = await client.PutAsync($"/api/v1/rooms/{Guid.NewGuid()}", content);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task PatchRoom_WithUnknownId_ReturnsNotFound()
    {
        object patchRequest = new { name = "Новое имя" };

        using (HttpContent content = JsonContent.Create(patchRequest))
        {
            HttpResponseMessage response = await client.PatchAsync($"/api/v1/rooms/{Guid.NewGuid()}", content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task PatchRoom_WithEmptyBody_ReturnsBadRequest()
    {
        using (HttpContent content = JsonContent.Create(new { }))
        {
            HttpResponseMessage response = await client.PatchAsync($"/api/v1/rooms/{Guid.NewGuid()}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task PatchRoom_UpsertsServicesByName()
    {
        object createRequest = new
        {
            name = "Зал с услугами",
            capacity = 12,
            baseHourlyRate = 900,
            services = new object[]
            {
                new { name = "Экран", price = 150 },
                new { name = "Wi-Fi", price = 300 }
            }
        };

        using (HttpContent createContent = JsonContent.Create(createRequest))
        {
            HttpResponseMessage createResponse = await client.PostAsync("/api/v1/rooms", createContent);
            JsonElement createdRoom = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            Guid roomId = createdRoom.GetProperty("id").GetGuid();
            Guid screenId = createdRoom.GetProperty("services").EnumerateArray()
                .Single(service => service.GetProperty("name").GetString() == "Экран")
                .GetProperty("id").GetGuid();
            Guid wifiId = createdRoom.GetProperty("services").EnumerateArray()
                .Single(service => service.GetProperty("name").GetString() == "Wi-Fi")
                .GetProperty("id").GetGuid();

            object patchRequest = new
            {
                services = new object[]
                {
                    new { name = "экран", price = 200 },
                    new { name = "Звук", price = 700 }
                }
            };

            using (HttpContent patchContent = JsonContent.Create(patchRequest))
            {
                HttpResponseMessage patchResponse = await client.PatchAsync($"/api/v1/rooms/{roomId}", patchContent);
                Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);
            }

            HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
            JsonElement patchedRoom = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            Dictionary<string, JsonElement> services = patchedRoom.GetProperty("services").EnumerateArray()
                .ToDictionary(service => service.GetProperty("name").GetString()!, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(3, services.Count);
            Assert.Equal(screenId, services["Экран"].GetProperty("id").GetGuid());
            Assert.Equal(200m, services["Экран"].GetProperty("price").GetDecimal());
            Assert.Equal(wifiId, services["Wi-Fi"].GetProperty("id").GetGuid());
            Assert.Equal(300m, services["Wi-Fi"].GetProperty("price").GetDecimal());
            Assert.Equal(700m, services["Звук"].GetProperty("price").GetDecimal());
        }
    }

    [Fact]
    public async Task PatchRoom_ConcurrentServiceUpserts_KeepAllServices()
    {
        object createRequest = new
        {
            name = "Зал для гонок",
            capacity = 8,
            baseHourlyRate = 500,
            services = new object[]
            {
                new { name = "Экран", price = 150 }
            }
        };

        using (HttpContent createContent = JsonContent.Create(createRequest))
        {
            HttpResponseMessage createResponse = await client.PostAsync("/api/v1/rooms", createContent);
            JsonElement createdRoom = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            Guid roomId = createdRoom.GetProperty("id").GetGuid();

            Task<HttpResponseMessage> firstPatch = PatchServicesAsync(roomId, new { name = "Звук", price = 700 });
            Task<HttpResponseMessage> secondPatch = PatchServicesAsync(roomId, new { name = "Wi-Fi", price = 300 });
            HttpResponseMessage[] patchResponses = await Task.WhenAll(firstPatch, secondPatch);

            Assert.All(patchResponses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));

            HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
            JsonElement patchedRoom = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            HashSet<string> serviceNames = patchedRoom.GetProperty("services").EnumerateArray()
                .Select(service => service.GetProperty("name").GetString()!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(3, serviceNames.Count);
            Assert.Contains("Экран", serviceNames);
            Assert.Contains("Звук", serviceNames);
            Assert.Contains("Wi-Fi", serviceNames);
        }
    }

    private async Task<HttpResponseMessage> PatchServicesAsync(Guid roomId, object service)
    {
        using HttpContent content = JsonContent.Create(new { services = new object[] { service } });
        return await client.PatchAsync($"/api/v1/rooms/{roomId}", content);
    }

    [Fact]
    public async Task FindAvailable_WithValidInterval_ReturnsRooms()
    {
        DateTimeOffset startsAt = DateTimeOffset.UtcNow.AddHours(1);
        DateTimeOffset endsAt = startsAt.AddHours(2);
        string requestUri = $"/api/v1/rooms/available?startsAt={Uri.EscapeDataString(startsAt.ToString("O"))}&endsAt={Uri.EscapeDataString(endsAt.ToString("O"))}&capacity=10";

        HttpResponseMessage response = await client.GetAsync(requestUri);
        JsonElement rooms = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(rooms.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateRoom_WithInvalidData_ReturnsBadRequest()
    {
        object request = new
        {
            name = string.Empty,
            capacity = 0,
            baseHourlyRate = 0,
            services = Array.Empty<object>()
        };

        using (HttpContent content = JsonContent.Create(request))
        {
            HttpResponseMessage response = await client.PostAsync("/api/v1/rooms", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task DeleteRoom_WithExistingBookings_ReturnsConflict()
    {
        object createRequest = new
        {
            name = "Зал с бронированием",
            capacity = 15,
            baseHourlyRate = 1000,
            services = Array.Empty<object>()
        };

        using HttpContent createContent = JsonContent.Create(createRequest);
        HttpResponseMessage createResponse = await client.PostAsync("/api/v1/rooms", createContent);
        JsonElement createdRoom = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Guid roomId = createdRoom.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        object bookingRequest = new
        {
            roomId,
            startsAt = DateTimeOffset.UtcNow.AddHours(10),
            durationMinutes = 60,
            serviceIds = Array.Empty<Guid>()
        };

        using HttpContent bookingContent = JsonContent.Create(bookingRequest);
        HttpResponseMessage bookingResponse = await client.PostAsync("/api/v1/bookings", bookingContent);
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}
