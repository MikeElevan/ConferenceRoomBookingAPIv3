using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Запрос на поиск доступных конференц-залов.
/// </summary>
public sealed class AvailabilityRequest : IValidatableObject
{
    /// <summary>Дата и время начала бронирования.</summary>
    [Required]
    public DateTimeOffset? StartsAt { get; init; }

    /// <summary>Дата и время окончания бронирования.</summary>
    [Required]
    public DateTimeOffset? EndsAt { get; init; }

    /// <summary>Минимальная требуемая вместимость (1-100000).</summary>
    [Required, Range(1, 100_000)]
    public int Capacity { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (StartsAt.HasValue && EndsAt.HasValue && EndsAt <= StartsAt)
        {
            yield return new ValidationResult("EndsAt must be later than StartsAt.", new string[] { nameof(EndsAt) });
        }
    }
}
