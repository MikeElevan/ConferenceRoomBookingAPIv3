using ConferenceRoomBookingAPIv3.Application;
using ConferenceRoomBookingAPIv3.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Infrastructure;

public sealed class BookingExceptionHandler(ILogger<BookingExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title, string detail) = exception switch
        {
            BookingException bookingException => Map(bookingException),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, "invalid_request", argumentException.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "persistence_conflict", "The request conflicts with the current persisted state."),
            _ => default
        };

        if (status == default) return false;

        logger.LogWarning(exception, "Request failed with {StatusCode}: {Title}", status, title);
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = httpContext.Request.Path }, cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail) Map(BookingException exception) => exception.Code switch
    {
        ErrorCode.RoomNotFound or ErrorCode.ServiceNotFound => (StatusCodes.Status404NotFound, exception.Code.ToValue(), exception.Message),
        ErrorCode.BookingConflict or ErrorCode.RoomHasBookings or ErrorCode.ServiceNameConflict => (StatusCodes.Status409Conflict, exception.Code.ToValue(), exception.Message),
        _ => (StatusCodes.Status400BadRequest, exception.Code.ToValue(), exception.Message)
    };
}
