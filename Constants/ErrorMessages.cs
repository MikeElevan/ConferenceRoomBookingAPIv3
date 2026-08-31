namespace ConferenceRoomBookingAPIv3.Constants;

/// <summary>
/// Читаемые сообщения об ошибках, возвращаемые в ErrorResponse.Message.
/// </summary>
public static class ErrorMessages
{
    /// <summary>Зал не найден.</summary>
    public const string RoomNotFound = "Conference room was not found.";

    /// <summary>Одна или несколько услуг недоступны в этом зале.</summary>
    public const string ServiceNotFound = "One or more services are not available in this room.";

    /// <summary>Зал уже забронирован на указанный интервал.</summary>
    public const string BookingConflict = "The room is already booked for the requested interval.";

    /// <summary>Нельзя удалить зал с активными бронированиями.</summary>
    public const string RoomHasBookings = "The room cannot be deleted because it still has bookings.";

    /// <summary>Другая услуга с таким именем была добавлена одновременно.</summary>
    public const string ServiceNameConflict = "Another service with this name was added to the room at the same time.";
}
