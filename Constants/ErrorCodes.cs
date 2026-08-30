namespace ConferenceRoomBookingAPIv3.Constants;

public enum ErrorCode
{
    RoomNotFound,
    ServiceNotFound,
    BookingConflict,
    RoomHasBookings,
    ServiceNameConflict,
    Unknown
}

public static class ErrorCodeExtensions
{
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
