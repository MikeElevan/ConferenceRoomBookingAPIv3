namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

/// <summary>
/// Ответ с информацией о созданном бронировании.
/// </summary>
/// <param name="Id">Идентификатор бронирования.</param>
/// <param name="RoomId">Идентификатор зала.</param>
/// <param name="StartsAt">Дата и время начала.</param>
/// <param name="EndsAt">Дата и время окончания.</param>
/// <param name="RoomCost">Стоимость аренды зала.</param>
/// <param name="ServicesCost">Стоимость услуг.</param>
/// <param name="TotalCost">Итоговая стоимость.</param>
/// <param name="Services">Список выбранных услуг.</param>
public sealed record BookingResponse(Guid Id, Guid RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, decimal RoomCost, decimal ServicesCost, decimal TotalCost, IReadOnlyList<ServiceResponse> Services);
