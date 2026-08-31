namespace ConferenceRoomBookingAPIv3.DomainModels;

/// <summary>
/// Конференц-зал — основная сущность для бронирования.
/// Содержит информацию о вместимости, базовой почасовой ставке и доступных услугах.
/// </summary>
public sealed class ConferenceRoom
{
    /// <summary>Уникальный идентификатор зала.</summary>
    public Guid Id { get; init; }

    /// <summary>Название зала.</summary>
    public required string Name { get; set; }

    /// <summary>Максимальная вместимость зала (количество человек).</summary>
    public int Capacity { get; set; }

    /// <summary>Базовая почасовая ставка в валюте системы.</summary>
    public decimal BaseHourlyRate { get; set; }

    /// <summary>Версия строки для optimistic concurrency (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>Список дополнительных услуг, доступных в этом зале.</summary>
    public List<RoomService> Services { get; set; } = new List<RoomService>();
}
