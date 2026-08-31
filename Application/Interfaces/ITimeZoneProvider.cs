namespace ConferenceRoomBookingAPIv3.Application.Interfaces;

/// <summary>
/// Абстракция над системным TimeZoneInfo для улучшения тестируемости PricingService.
/// Позволяет замокать часовой пояс в тестах без зависимости от реальной ОС.
/// </summary>
public interface ITimeZoneProvider
{
    /// <summary>Возвращает TimeZoneInfo по идентификатору.</summary>
    /// <exception cref="TimeZoneNotFoundException">Если часовой пояс не найден.</exception>
    TimeZoneInfo FindTimeZoneById(string id);

    /// <summary>Преобразует время из одного часового пояса в другой.</summary>
    DateTimeOffset ConvertTime(DateTimeOffset dateTime, TimeZoneInfo destinationTimeZone);
}