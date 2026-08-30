using ConferenceRoomBookingAPIv3.Application;
using ConferenceRoomBookingAPIv3.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Infrastructure;

/// <summary>
/// One place that turns exceptions into ProblemDetails responses, instead of a try/catch
/// repeated across controllers. This is also, deliberately, the only place that logs an
/// exception: ASP.NET Core's own ExceptionHandlerMiddleware logs every caught exception at
/// Error level *before* calling any registered IExceptionHandler, regardless of whether it ends
/// up handled here — left alone, that produces a duplicate log entry for every 400/404/409 this
/// handler already accounts for. That built-in log is suppressed for this category in
/// appsettings.json (see Logging:LogLevel), and this handler takes over logging every exception
/// exactly once, at a severity that matches what actually happened, including the fallback case
/// where nothing below recognizes the exception and it becomes a genuine 500.
/// </summary>
public sealed class BookingExceptionHandler(ILogger<BookingExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string Detail, LogLevel LogLevel)? mapped = exception switch
        {
            BookingException bookingException => Map(bookingException),
            ArgumentException argumentException =>
                (StatusCodes.Status400BadRequest, "invalid_request", argumentException.Message, LogLevel.Information),

            // A genuine optimistic-concurrency conflict (someone else changed/deleted the same
            // row first) is a conflict a client can meaningfully react to — refetch and retry.
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "concurrency_conflict",
                    "The requested change conflicts with another change made to the same data at the same time.",
                    LogLevel.Warning),

            // Any other DbUpdateException — a constraint violation we don't already translate
            // into a typed BookingException closer to the source, a genuinely unexpected database
            // error, etc. — is deliberately NOT mapped here, so it falls through to the 500 path
            // below. Guessing a specific 4xx for an error we don't recognize would risk telling
            // the client it made a mistake when the server did.
            _ => null
        };

        if (mapped is not { } result)
        {
            logger.LogError(exception, "Unhandled exception; returning 500.");
            return false;
        }

        logger.Log(result.LogLevel, exception, "Request failed with {StatusCode}: {Title}", result.Status, result.Title);

        httpContext.Response.StatusCode = result.Status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = result.Status, Title = result.Title, Detail = result.Detail, Instance = httpContext.Request.Path },
            cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail, LogLevel LogLevel) Map(BookingException exception) => exception.Code switch
    {
        ErrorCode.RoomNotFound or ErrorCode.ServiceNotFound =>
            (StatusCodes.Status404NotFound, exception.Code.ToValue(), exception.Message, LogLevel.Information),
        ErrorCode.BookingConflict or ErrorCode.RoomHasBookings or ErrorCode.ServiceNameConflict =>
            (StatusCodes.Status409Conflict, exception.Code.ToValue(), exception.Message, LogLevel.Information),
        _ => (StatusCodes.Status400BadRequest, exception.Code.ToValue(), exception.Message, LogLevel.Information)
    };
}
