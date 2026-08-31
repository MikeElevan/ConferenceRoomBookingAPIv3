using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Оборёртка над EF Core ExecutionStrategy и транзакциями,
/// чтобы репозиторий не зависел от инфраструктурных деталей EF Core напрямую.
/// </summary>
public interface IBookingTransactionExecutor
{
    /// <summary>
    /// Выполняет операцию создания бронирования с защитой от конфликтов.
    /// Использует серийную транзакцию и стратегию повторов для обеспечения атомарности.
    /// При гонке двух запросов с одинаковым IdempotencyKey возвращает бронирование,
    /// созданное первым запросом, чтобы повторный POST вернул тот же результат.
    /// </summary>
    /// <returns>Сохранённое бронирование (новое или ранее созданное по ключу идемпотентности);
    /// null, если на запрошенный интервал есть конфликт по времени.</returns>
    Task<Booking?> TryAddBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}