namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Расчёт стоимости бронирования на основе почасовых тарифов.
/// </summary>
public interface IPricingService
{
    /// <summary>Вычисляет стоимость аренды зала с учётом пиковых/стандартных/ночных тарифов.</summary>
    /// <param name="hourlyRate">Базовая почасовая ставка.</param>
    /// <param name="startsAt">Начало бронирования.</param>
    /// <param name="endsAt">Конец бронирования.</param>
    decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt);
}
