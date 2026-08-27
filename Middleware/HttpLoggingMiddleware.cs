using System.Diagnostics;
using System.Text;
using ConferenceRoomBookingAPIv3.Constants;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Middleware;

public sealed class HttpLoggingMiddleware(
    RequestDelegate next,
    ILogger<HttpLoggingMiddleware> logger,
    IOptions<HttpLoggingOptions> options)
{
    private readonly HttpLoggingOptions loggingOptions = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!loggingOptions.Enabled)
        {
            await next(context);
            return;
        }

        string requestId = context.TraceIdentifier;
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.LogInformation(LogMessages.Request, context.Request.Method, context.Request.Path, requestId);

        if (loggingOptions.IncludeRequestBody)
        {
            string requestBody = await ReadRequestBodyAsync(context.Request);
            logger.LogInformation(LogMessages.RequestBody, context.Request.Method, context.Request.Path, requestBody);
        }

        Stream originalResponseBody = context.Response.Body;
        await using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
                responseBody.Position = 0;

                if (loggingOptions.IncludeResponseBody)
                {
                    string responseText = await ReadBodyAsync(responseBody);
                    logger.LogInformation(LogMessages.ResponseBody, context.Request.Method, context.Request.Path, responseText);
                    responseBody.Position = 0;
                }

                await responseBody.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
                logger.LogInformation(
                    LogMessages.Response,
                    context.Response.StatusCode,
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    requestId);
            }
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        string body = await ReadBodyAsync(request.Body);
        request.Body.Position = 0;
        return body;
    }

    private async Task<string> ReadBodyAsync(Stream body)
    {
        using (var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true))
        {
            string content = await reader.ReadToEndAsync();
            return content.Length <= loggingOptions.MaxBodyLength
                ? content
                : content[..loggingOptions.MaxBodyLength];
        }
    }
}
