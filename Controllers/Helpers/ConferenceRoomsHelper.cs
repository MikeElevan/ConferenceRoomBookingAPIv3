using ConferenceRoomBookingAPIv3.Contracts.RequestModels;
using ConferenceRoomBookingAPIv3.Contracts.ResponseModels;
using ConferenceRoomBookingAPIv3.DomainModels;

namespace ConferenceRoomBookingAPIv3.Controllers.Helpers;

public static class ConferenceRoomsHelper
{
    public static ConferenceRoom ToEntity(RoomRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ConferenceRoom
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            BaseHourlyRate = request.BaseHourlyRate,
            Services = ToServices(request.Services)
        };
    }

    public static void ApplyPatch(ConferenceRoom room, RoomPatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Name is not null)
        {
            room.Name = request.Name.Trim();
        }

        if (request.Capacity is not null)
        {
            room.Capacity = request.Capacity.Value;
        }

        if (request.BaseHourlyRate is not null)
        {
            room.BaseHourlyRate = request.BaseHourlyRate.Value;
        }

        if (request.Services is not null)
        {
            UpsertServices(room, request.Services);
        }
    }

    public static RoomResponse ToResponse(ConferenceRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomResponse(
            room.Id,
            room.Name,
            room.Capacity,
            room.BaseHourlyRate,
            room.Services.Select(ToServiceResponse).ToList());
    }

    private static void UpsertServices(ConferenceRoom room, IEnumerable<RoomServiceRequest> services)
    {
        foreach (RoomServiceRequest incoming in services)
        {
            string name = incoming.Name.Trim();
            RoomService? existing = room.Services.FirstOrDefault(service =>
                string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Name = name;
                existing.Price = incoming.Price;
                continue;
            }

            room.Services.Add(new RoomService
            {
                Id = Guid.NewGuid(),
                Name = name,
                Price = incoming.Price
            });
        }
    }

    private static List<RoomService> ToServices(IEnumerable<RoomServiceRequest> services) =>
        services.Select(service => new RoomService
        {
            Id = Guid.NewGuid(),
            Name = service.Name.Trim(),
            Price = service.Price
        }).ToList();

    private static ServiceResponse ToServiceResponse(RoomService service) =>
        new(service.Id, service.Name, service.Price);
}
