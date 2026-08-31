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
    IBookingTransactionExecutor bookingTransactionExecutor,
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
    /// <returns>Созданное бронирование с рассчитанной стоимостью; при повторном запросе
    /// с тем же IdempotencyKey — ранее созданное бронирование.</returns>
    /// <exception cref="ArgumentNullException">Если serviceIds is null.</exception>
    /// <exception cref="ArgumentException">Если roomId is empty или durationMinutes &lt;= 0.</exception>
    /// <exception cref="BookingException">Если зал не найден (RoomNotFound) или услуга не найдена (ServiceNotFound).</exception>
    /// <exception cref="BookingException">Если время занято (BookingConflict).</exception>
    public async Task<Booking> CreateAsync(Guid roomId, DateTimeOffset startsAt, int durationMinutes, IEnumerable<Guid> serviceIds, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIds);
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("The room identifier cannot be empty.", nameof(roomId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes);

        // Idempotency: при наличии ключа проверяем, было ли уже выполнено бронирование
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            Booking? existing = await bookingRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        ConferenceRoom room = await roomRepository.GetRoomAsync(roomId, cancellationToken)
            ?? throw new BookingException(ErrorCode.RoomNotFound, ErrorMessages.RoomNotFound);
        DateTimeOffset endsAt = startsAt.AddMinutes(durationMinutes);
        HashSet<Guid> selectedIds = serviceIds.ToHashSet();
        List<RoomService> selectedServices = room.Services.Where(service => selectedIds.Contains(service.Id)).ToList();

        if (selectedServices.Count != selectedIds.Count)
        {
            throw new BookingException(ErrorCode.ServiceNotFound, ErrorMessages.ServiceNotFound);
        }

        Booking booking = BookingFactory.Create(
            roomId, startsAt, endsAt, idempotencyKey, selectedServices,
            pricing.CalculateRoomCost(room.BaseHourlyRate, startsAt, endsAt));

        Booking? savedBooking = await bookingTransactionExecutor.TryAddBookingAsync(booking, cancellationToken);
        if (savedBooking is null)
        {
            throw new BookingException(ErrorCode.BookingConflict, ErrorMessages.BookingConflict);
        }

        // При гонке двух POST с одинаковым IdempotencyKey здесь окажется бронирование,
        // созданное первым запросом — повторный запрос вернёт тот же результат (идемпотентность).
        return savedBooking;
    }

    /// <summary>
    /// Получить бронирование по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Бронирование или null, если бронирование не найдено.</returns>
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The booking identifier cannot be empty.", nameof(id));
        }

        return bookingRepository.GetByIdAsync(id, cancellationToken);
    }
}
