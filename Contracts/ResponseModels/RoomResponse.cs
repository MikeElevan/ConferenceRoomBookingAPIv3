namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

public sealed record RoomResponse(Guid Id, string Name, int Capacity, decimal BaseHourlyRate, IReadOnlyList<ServiceResponse> Services);
