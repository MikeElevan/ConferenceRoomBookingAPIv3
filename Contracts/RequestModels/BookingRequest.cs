using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

public sealed class BookingRequest : IValidatableObject
{
    [Required]
    public Guid? RoomId { get; init; }

    [Required]
    public DateTimeOffset? StartsAt { get; init; }

    [Range(1, 10_080)]
    public int DurationMinutes { get; init; }

    public List<Guid> ServiceIds { get; init; } = new List<Guid>();

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
