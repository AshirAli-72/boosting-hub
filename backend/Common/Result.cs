namespace BoostingHub.backend.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public string[]? Errors { get; init; }

    public static Result Success(string? message = null) =>
        new() { IsSuccess = true, Message = message };

    public static Result Failure(string message, string? errorCode = null, string[]? errors = null) =>
        new() { IsSuccess = false, Message = message, ErrorCode = errorCode, Errors = errors };

    public static Result<T> Success<T>(T data, string? message = null) =>
        new() { IsSuccess = true, Data = data, Message = message };

    public static Result<T> Failure<T>(string message, string? errorCode = null, string[]? errors = null) =>
        new() { IsSuccess = false, Message = message, ErrorCode = errorCode, Errors = errors };
}

public class Result<T> : Result
{
    public T? Data { get; init; }
}
