using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

public sealed class ReportRequest : IValidatableObject
{
    private const int MaximumRangeDays = 366;
    [Required]
    public DateTimeOffset? From { get; init; }

    [Required]
    public DateTimeOffset? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (From.HasValue && To.HasValue && To <= From)
        {
            yield return new ValidationResult("To must be later than From.", new string[] { nameof(To) });
        }
        if (From.HasValue && To.HasValue && To - From > TimeSpan.FromDays(MaximumRangeDays))
        {
            yield return new ValidationResult($"The report range must not exceed {MaximumRangeDays} days.", new string[] { nameof(To) });
        }
    }
}
