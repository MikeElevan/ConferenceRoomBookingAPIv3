using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

public sealed class RoomRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 100_000)]
    public int Capacity { get; init; }

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal BaseHourlyRate { get; init; }

    public List<RoomServiceRequest> Services { get; init; } = new List<RoomServiceRequest>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (Services.GroupBy(service => service.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            yield return new ValidationResult("Service names must be unique.", new string[] { nameof(Services) });
        }
    }
}
