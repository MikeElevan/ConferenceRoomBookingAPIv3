using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.DomainModels;
using ConferenceRoomBookingAPIv3.Application.Interfaces;

namespace ConferenceRoomBookingAPIv3.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями: поиск доступных залов и создание бронирований.
/// </summary>
public sealed class BookingService(
    IConferenceRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    IPricingService pricing)
{
    /// <summary>
    /// Поиск доступных конференц-залов для бронирования.
    /// </summary>
    /// <param name="startsAt">Дата и время начала бронирования.</param>
    /// <param name="endsAt">Дата и время окончания бронирования.</param>
    /// <param name="capacity">Минимальная требуемая вместимость.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список доступных залов, удовлетворяющих критериям.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если capacity &lt;= 0.</exception>
    /// <exception cref="ArgumentException">Если endsAt &lt;= startsAt.</exception>
    public async Task<IReadOnlyList<ConferenceRoom>> SearchAsync(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The ending time must be later than the starting time.", nameof(endsAt));
        }

        return await roomRepository.GetAvailableRoomsAsync(startsAt, endsAt, capacity, cancellationToken);
    }

    /// <summary>
    /// Создание нового бронирования конференц-зала.
    /// Рассчитывает стоимость на основе тарифа зала и выбранных услуг.
    /// </summary>
    /// <param name="roomId">Идентификатор зала.</param>
    /// <param name="startsAt">Дата и время начала бронирования.</param>
    /// <param name="durationMinutes">Продолжительность в минутах.</param>
    /// <param name="serviceIds">Идентификаторы выбранных услуг.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Созданное бронирование с рассчитанной стоимостью.</returns>
    /// <exception cref="ArgumentNullException">Если serviceIds is null.</exception>
    /// <exception cref="ArgumentException">Если roomId is empty или durationMinutes &lt;= 0.</exception>
    /// <exception cref="BookingException">Если зал не найден (RoomNotFound) или услуга не найдена (ServiceNotFound).</exception>
    /// <exception cref="BookingException">Если время занято (BookingConflict).</exception>
    public async Task<Booking> CreateAsync(Guid roomId, DateTimeOffset startsAt, int durationMinutes, IEnumerable<Guid> serviceIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIds);
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("The room identifier cannot be empty.", nameof(roomId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes);

        ConferenceRoom room = await roomRepository.GetRoomAsync(roomId, cancellationToken)
            ?? throw new BookingException(ErrorCode.RoomNotFound, ErrorMessages.RoomNotFound);
        DateTimeOffset endsAt = startsAt.AddMinutes(durationMinutes);
        HashSet<Guid> selectedIds = serviceIds.ToHashSet();
        List<RoomService> selectedServices = room.Services.Where(service => selectedIds.Contains(service.Id)).ToList();

        if (selectedServices.Count != selectedIds.Count)
        {
            throw new BookingException(ErrorCode.ServiceNotFound, ErrorMessages.ServiceNotFound);
        }

        Guid bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            RoomId = roomId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Services = selectedServices.Select(service => new BookingServiceSnapshot
            {
                BookingId = bookingId,
                ServiceId = service.Id,
                Name = service.Name,
                Price = service.Price
            }).ToList(),
            RoomCost = pricing.CalculateRoomCost(room.BaseHourlyRate, startsAt, endsAt),
            ServicesCost = selectedServices.Sum(service => service.Price)
        };

        if (!await bookingRepository.TryAddBookingAsync(booking, cancellationToken))
        {
            throw new BookingException(ErrorCode.BookingConflict, ErrorMessages.BookingConflict);
        }

        return booking;
    }
}
