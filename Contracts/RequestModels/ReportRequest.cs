using System.ComponentModel.DataAnnotations;
using ConferenceRoomBookingAPIv3.Constants;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Запрос на получение отчёта по бронированиям за период.
/// </summary>
public sealed class ReportRequest : IValidatableObject
{
    /// <summary>Начало периода.</summary>
    [Required]
    public DateTimeOffset? From { get; init; }

    /// <summary>Конец периода (не более 366 дней от From).</summary>
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

