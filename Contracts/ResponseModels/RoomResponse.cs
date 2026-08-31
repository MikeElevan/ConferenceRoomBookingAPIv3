namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

/// <summary>
/// Ответ с информацией о конференц-зале.
/// </summary>
/// <param name="Id">Идентификатор зала.</param>
/// <param name="Name">Название зала.</param>
/// <param name="Capacity">Вместимость.</param>
/// <param name="BaseHourlyRate">Базовая почасовая ставка.</param>
/// <param name="Services">Список услуг.</param>
public sealed record RoomResponse(Guid Id, string Name, int Capacity, decimal BaseHourlyRate, IReadOnlyList<ServiceResponse> Services);
