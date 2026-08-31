namespace ConferenceRoomBookingAPIv3.DomainModels;

/// <summary>
/// Дополнительная услуга, доступная в конференц-зале (проектор, Wi-Fi, звук и т.д.).
/// Неизменяемый (immutable) объект — все свойства задаются только при создании.
/// </summary>
public sealed class RoomService
{
    /// <summary>Уникальный идентификатор услуги.</summary>
    public Guid Id { get; init; }

    /// <summary>Название услуги.</summary>
    public required string Name { get; init; }

    /// <summary>Стоимость услуги.</summary>
    public decimal Price { get; init; }
}
