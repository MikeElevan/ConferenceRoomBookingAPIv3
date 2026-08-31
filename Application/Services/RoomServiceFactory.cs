using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Services;

/// <summary>
/// Фабрика создания <see cref="RoomService"/>.
/// Централизует создание и клонирование услуг зала,
/// устраняя дублирование логики в репозиториях и хелперах.
/// </summary>
public static class RoomServiceFactory
{
    /// <summary>Создаёт новую услугу с уникальным идентификатором.</summary>
    /// <param name="name">Название услуги (будет обрезано от пробелов).</param>
    /// <param name="price">Стоимость услуги.</param>
    public static RoomService Create(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Price = price
    };

    /// <summary>Создаёт услугу с указанным идентификатором (для обновления существующей).</summary>
    /// <param name="id">Сохраняемый идентификатор услуги.</param>
    /// <param name="name">Название услуги (будет обрезано от пробелов).</param>
    /// <param name="price">Стоимость услуги.</param>
    public static RoomService CreateWithId(Guid id, string name, decimal price) => new()
    {
        Id = id,
        Name = name.Trim(),
        Price = price
    };

    /// <summary>Клонирует услугу, сохраняя её идентификатор.</summary>
    public static RoomService Clone(RoomService source) =>
        CreateWithId(source.Id, source.Name, source.Price);
}