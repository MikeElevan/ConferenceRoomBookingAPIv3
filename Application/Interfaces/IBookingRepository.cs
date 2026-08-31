using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Статистика бронирований по комнате для отчёта.
/// </summary>
public sealed record RoomBookingStats(
    Guid RoomId,
    int BookingCount,
    decimal Revenue,
    double BookedHours);

/// <summary>
/// Статистика использования услуги для отчёта.
/// </summary>
public sealed record ServiceStats(
    Guid ServiceId,
    string ServiceName,
    int UsageCount,
    decimal Revenue);

/// <summary>
/// Persistence operations for the booking aggregate only. Split out from
/// <see cref=\"IConferenceRoomRepository\"/> so that consumers who only need bookings (or only
/// need rooms) depend on — and can be mocked against — exactly the surface they use.
/// </summary>
public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирование по идентификатору.</summary>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ищет бронирование по ключу идемпотентности.
    /// Используется для возврата результата при повторных запросах с тем же ключом.
    /// </summary>
    Task<Booking?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Агрегирует статистику бронирований по комнатам за период.
    /// Выполняется на стороне БД для минимального потребления памяти.
    /// </summary>
    Task<IReadOnlyList<RoomBookingStats>> GetRoomBookingStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Агрегирует статистику использования услуг за период.
    /// Выполняется на стороне БД для минимального потребления памяти.
    /// </summary>
    Task<IReadOnlyList<ServiceStats>> GetServiceStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
