using ConferenceRoomBookingAPIv3.Constants;

namespace ConferenceRoomBookingAPIv3.Application;

public sealed class BookingException(ErrorCode code, string message) : Exception(message)
{
    public ErrorCode Code { get; } = code;
}
