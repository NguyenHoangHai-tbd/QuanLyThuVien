namespace QLyThuVien.Application.Common;

public sealed class AppException : Exception
{
    public AppException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public static AppException BadRequest(string message) => new(message, 400);

    public static AppException Unauthorized(string message = "Unauthorized") => new(message, 401);

    public static AppException Forbidden(string message = "Forbidden") => new(message, 403);

    public static AppException NotFound(string message = "Not found") => new(message, 404);

    public static AppException Conflict(string message) => new(message, 409);
}
