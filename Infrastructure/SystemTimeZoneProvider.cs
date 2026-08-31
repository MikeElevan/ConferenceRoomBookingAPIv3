using ConferenceRoomBookingAPIv3.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Стандартная реализация ITimeZoneProvider, использующая TimeZoneInfo из ОС.
/// </summary>
public sealed class SystemTimeZoneProvider : ITimeZoneProvider
{
    /// <inheritdoc />
    public TimeZoneInfo FindTimeZoneById(string id) =>
        TimeZoneInfo.FindSystemTimeZoneById(id);

    /// <inheritdoc />
    public DateTimeOffset ConvertTime(DateTimeOffset dateTime, TimeZoneInfo destinationTimeZone) =>
        TimeZoneInfo.ConvertTime(dateTime, destinationTimeZone);
}

/// <summary>
/// Методы расширения для регистрации сервисов Time Zone в DI-контейнере.
/// </summary>
public static class TimeZoneProviderServiceCollectionExtensions
{
    /// <summary>Добавляет SystemTimeZoneProvider в контейнер зависимостей.</summary>
    public static IServiceCollection AddSystemTimeZoneProvider(this IServiceCollection services)
    {
        services.AddSingleton<ITimeZoneProvider, SystemTimeZoneProvider>();
        return services;
    }
}