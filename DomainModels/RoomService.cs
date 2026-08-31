namespace ConferenceRoomBookingAPIv3.DomainModels;

/// <summary>
/// Дополнительная услуга, доступная в конференц-зале (проектор, Wi-Fi, звук и т.д.).
/// </summary>
public sealed class RoomService
{
    /// <summary>Уникальный идентификатор услуги.</summary>
    public Guid Id { get; init; }

    /// <summary>Название услуги.</summary>
    public required string Name { get; set; }

    /// <summary>Стоимость услуги.</summary>
    public decimal Price { get; set; }
}
