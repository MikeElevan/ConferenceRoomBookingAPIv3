using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Запрос на частичное обновление конференц-зала.
/// Все поля опциональны — обновляются только указанные.
/// </summary>
public sealed class RoomPatchRequest : IValidatableObject
{
    /// <summary>Новое название зала (1-100 символов).</summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    /// <summary>Новая вместимость зала (1-100000 человек).</summary>
    [Range(1, 100_000)]
    public int? Capacity { get; init; }

    /// <summary>Новая базовая почасовая ставка.</summary>
    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal? BaseHourlyRate { get; init; }

    /// <summary>Новый список услуг (заменяет существующий).</summary>
    public List<RoomServiceRequest>? Services { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (Name is null && Capacity is null && BaseHourlyRate is null && Services is null)
        {
            yield return new ValidationResult("At least one property must be provided.");
        }

        if (Services is not null &&
            Services.GroupBy(service => service.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            yield return new ValidationResult("Service names must be unique.", new string[] { nameof(Services) });
        }
    }
}
