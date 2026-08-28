namespace ConferenceRoomBookingAPIv3.Constants;

public static class ErrorMessages
{
    public const string RoomNotFound = "Conference room was not found.";
    public const string ServiceNotFound = "One or more services are not available in this room.";
    public const string BookingConflict = "The room is already booked for the requested interval.";
    public const string RoomHasBookings = "The room cannot be deleted because it still has bookings.";
}
