using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Запрос на создание бронирования конференц-зала.
/// Поддерживает идемпотентность через IdempotencyKey для безопасных повторных запросов.
/// </summary>
public sealed class BookingRequest : IValidatableObject
{
    /// <summary>Идентификатор зала.</summary>
    [Required]
    public Guid? RoomId { get; init; }

    /// <summary>Дата и время начала бронирования.</summary>
    [Required]
    public DateTimeOffset? StartsAt { get; init; }

    /// <summary>Продолжительность в минутах (1-10080).</summary>
    [Range(1, 10_080)]
    public int DurationMinutes { get; init; }

    /// <summary>Идентификаторы выбранных услуг.</summary>
    public List<Guid> ServiceIds { get; init; } = new List<Guid>();

    /// <summary>
    /// Ключ идемпотентности. При повторной отправке запроса с тем же ключом вернётся
    /// результат первого выполнения вместо создания дубликата. Рекомендуется использовать
    /// GUID, сгенерированный клиентом.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (StartsAt.HasValue && StartsAt.Value < DateTimeOffset.UtcNow.AddMinutes(-1))
        {
            yield return new ValidationResult("StartsAt must not be in the past.", new string[] { nameof(StartsAt) });
        }
        if (ServiceIds.Count != ServiceIds.Distinct().Count())
        {
            yield return new ValidationResult("ServiceIds must not contain duplicates.", new string[] { nameof(ServiceIds) });
        }
    }
}
