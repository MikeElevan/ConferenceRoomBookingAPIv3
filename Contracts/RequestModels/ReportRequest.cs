using System.ComponentModel.DataAnnotations;
using ConferenceRoomBookingAPIv3.Constants;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

public sealed class ReportRequest : IValidatableObject
{
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
        if (From.HasValue && To.HasValue && To - From > TimeSpan.FromDays(ReportLimits.MaximumRangeDays))
        {
            yield return new ValidationResult($"The report range must not exceed {ReportLimits.MaximumRangeDays} days.", new string[] { nameof(To) });
        }
    }
}

