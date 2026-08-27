namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

public sealed record BookingResponse(Guid Id, Guid RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, decimal RoomCost, decimal ServicesCost, decimal TotalCost, IReadOnlyList<ServiceResponse> Services);
