namespace ConferenceRoomBookingAPIv3.DomainModels;

/// <summary>
/// Бронирование конференц-зала.
/// Содержит информацию о времени, зале, выбранных услугах и рассчитанной стоимости.
/// </summary>
public sealed class Booking
{
    /// <summary>Уникальный идентификатор бронирования.</summary>
    public Guid Id { get; init; }

    /// <summary>Идентификатор забронированного зала.</summary>
    public Guid RoomId { get; init; }

    /// <summary>Дата и время начала бронирования.</summary>
    public DateTimeOffset StartsAt { get; init; }

    /// <summary>Дата и время окончания бронирования.</summary>
    public DateTimeOffset EndsAt { get; init; }

    /// <summary>Снимок выбранных услуг с ценами на момент бронирования.</summary>
    public List<BookingServiceSnapshot> Services { get; init; } = new List<BookingServiceSnapshot>();

    /// <summary>Стоимость аренды зала (без услуг).</summary>
    public decimal RoomCost { get; init; }

    /// <summary>Общая стоимость выбранных услуг.</summary>
    public decimal ServicesCost { get; init; }

    /// <summary>Ключ идемпотентности для предотвращения дублирования при повторных запросах.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Итоговая стоимость бронирования (RoomCost + ServicesCost).</summary>
    public decimal TotalCost => RoomCost + ServicesCost;
}
