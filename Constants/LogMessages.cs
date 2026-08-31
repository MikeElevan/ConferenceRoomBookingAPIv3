namespace ConferenceRoomBookingAPIv3.Constants;

/// <summary>
/// Шаблоны сообщений логирования HTTP-запросов.
/// </summary>
public static class LogMessages
{
    public const string Request = "HTTP request {Method} {Path} started. RequestId: {RequestId}";
    public const string Response = "HTTP response {StatusCode} for {Method} {Path} completed in {ElapsedMilliseconds} ms. RequestId: {RequestId}";
    public const string RequestBody = "HTTP request body for {Method} {Path}: {Body}";
    public const string ResponseBody = "HTTP response body for {Method} {Path}: {Body}";
}
