using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBookingAPIv3.Contracts.RequestModels;

/// <summary>
/// Запрос на создание конференц-зала.
/// </summary>
public sealed class RoomRequest : IValidatableObject
{
    /// <summary>Название зала (1-100 символов).</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Вместимость зала (1-100000 человек).</summary>
    [Range(1, 100_000)]
    public int Capacity { get; init; }

    /// <summary>Базовая почасовая ставка.</summary>
    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal BaseHourlyRate { get; init; }

    /// <summary>Список услуг в зале.</summary>
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
