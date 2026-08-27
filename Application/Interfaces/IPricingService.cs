namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

public interface IPricingService
{
    decimal CalculateRoomCost(decimal hourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt);
}
