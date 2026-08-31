namespace ConferenceRoomBookingAPIv3.Contracts.ResponseModels;

/// <summary>
/// Стандартизированный ответ при ошибке.
/// </summary>
/// <param name="Code">Код ошибки (ErrorCode).</param>
/// <param name="Message">Читаемое описание ошибки.</param>
public sealed record ErrorResponse(string Code, string Message);
