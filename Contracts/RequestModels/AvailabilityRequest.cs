using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

public sealed class AvailabilityRequest : IValidatableObject
{
    [Required]
    public DateTimeOffset? StartsAt { get; init; }

    [Required]
    public DateTimeOffset? EndsAt { get; init; }

    [Range(1, 100_000)]
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
