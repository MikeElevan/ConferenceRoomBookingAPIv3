using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Услуга в составе запроса на создание/изменение зала.
/// </summary>
public sealed class RoomServiceRequest
{
    /// <summary>Название услуги (1-100 символов).</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Стоимость услуги.</summary>
    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal Price { get; init; }
}
