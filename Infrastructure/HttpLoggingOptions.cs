namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// Настройки логирования HTTP-запросов/ответов в <see cref="Middleware.HttpLoggingMiddleware"/>.
/// </summary>
public sealed class HttpLoggingOptions
{
    public const string SectionName = "HttpLogging";

    /// <summary>Включить логирование.</summary>
    public bool Enabled { get; init; }

    /// <summary>Логировать тело запроса.</summary>
    public bool IncludeRequestBody { get; init; }

    /// <summary>Логировать тело ответа.</summary>
    public bool IncludeResponseBody { get; init; }

    /// <summary>Максимальная длина тела для логирования (символов).</summary>
    public int MaxBodyLength { get; init; }
}
