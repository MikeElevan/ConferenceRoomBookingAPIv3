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
    public async Task CreateUpdateAndDeleteRoom_UsesExpectedHttpContract()
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

            object updateRequest = new
            {
                name = "Обновлённый зал",
                capacity = 25,
                baseHourlyRate = 1500,
                services = Array.Empty<object>()
            };

            using (HttpContent updateContent = JsonContent.Create(updateRequest))
            {
                HttpResponseMessage updateResponse = await client.PutAsync($"/api/v1/rooms/{roomId}", updateContent);
                Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
            }

            object patchRequest = new { capacity = 30 };
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
            Assert.Equal(0, patchedRoom.GetProperty("services").GetArrayLength());

            HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/rooms/{roomId}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/rooms/{roomId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
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

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}
