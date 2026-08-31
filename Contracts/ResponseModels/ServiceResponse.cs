namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

/// <summary>
/// Ответ с информацией об услуге в зале.
/// </summary>
/// <param name="Id">Идентификатор услуги.</param>
/// <param name="Name">Название услуги.</param>
/// <param name="Price">Стоимость.</param>
public sealed record ServiceResponse(Guid Id, string Name, decimal Price);
