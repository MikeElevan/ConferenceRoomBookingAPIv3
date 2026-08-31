using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Services;

/// <summary>
/// Фабрика создания <see cref="Booking"/>.
/// Централизует генерацию идентификатора и формирования снимков выбранных услуг,
/// избавляя сервис бронирования от деталей построения доменного объекта.
/// </summary>
public static class BookingFactory
{
    /// <summary>
    /// Создаёт бронирование с рассчитанной стоимостью и снимками выбранных услуг.
    /// </summary>
    /// <param name="roomId">Идентификатор зала.</param>
    /// <param name="startsAt">Дата и время начала бронирования.</param>
    /// <param name="endsAt">Дата и время окончания бронирования.</param>
    /// <param name="idempotencyKey">Ключ идемпотентности (опционально).</param>
    /// <param name="selectedServices">Выбранные услуги зала.</param>
    /// <param name="roomCost">Рассчитанная стоимость аренды зала.</param>
    public static Booking Create(
        Guid roomId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? idempotencyKey,
        IReadOnlyList<RoomService> selectedServices,
        decimal roomCost)
    {
        Guid bookingId = Guid.NewGuid();
        return new Booking
        {
            Id = bookingId,
            RoomId = roomId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            IdempotencyKey = idempotencyKey,
            Services = selectedServices.Select(service => new BookingServiceSnapshot
            {
                BookingId = bookingId,
                ServiceId = service.Id,
                Name = service.Name,
                Price = service.Price
            }).ToList(),
            RoomCost = roomCost,
            ServicesCost = selectedServices.Sum(service => service.Price)
        };
    }
}