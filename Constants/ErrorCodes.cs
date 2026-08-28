namespace ConferenceRoomBookingAPIv3.Constants;

public enum ErrorCode
{
    RoomNotFound,
    ServiceNotFound,
    BookingConflict,
    RoomHasBookings,
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
        _ => "unknown_error"
    };
}
