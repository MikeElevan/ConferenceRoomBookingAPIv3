namespace ConferenceRoomBookingAPIv3.Constants;

/// <summary>
/// Коды ошибок, возвращаемые в ErrorResponse.Code.
/// </summary>
public enum ErrorCode
{
    /// <summary>Зал не найден.</summary>
    RoomNotFound,

    /// <summary>Услуга не найдена в зале.</summary>
    ServiceNotFound,

    /// <summary>Конфликт бронирования (зал уже забронирован).</summary>
    BookingConflict,

    /// <summary>Нельзя удалить зал с активными бронированиями.</summary>
    RoomHasBookings,

    /// <summary>Конфликт имени услуги при одновременном обновлении.</summary>
    ServiceNameConflict,

    /// <summary>Неизвестная ошибка.</summary>
    Unknown
}

/// <summary>
/// Расширения для преобразования ErrorCode в строковое значение.
/// </summary>
public static class ErrorCodeExtensions
{
    /// <summary>Преобразует код ошибки в snake_case строку для API.</summary>
    public static string ToValue(this ErrorCode errorCode) => errorCode switch
    {
        ErrorCode.RoomNotFound => "room_not_found",
        ErrorCode.ServiceNotFound => "service_not_found",
        ErrorCode.BookingConflict => "booking_conflict",
        ErrorCode.RoomHasBookings => "room_has_bookings",
        ErrorCode.ServiceNameConflict => "service_name_conflict",
        _ => "unknown_error"
    };
}
