namespace ModuPOS.Shared.Results;

using System.Text.Json.Serialization;

[method: JsonConstructor]
public class Result(bool isSuccess, string message)
{
    public bool IsSuccess { get; init; } = isSuccess;

    public string Message { get; init; } = message;

    public bool IsFailure => !IsSuccess;

    public static Result Success(string message = "Success") => new(true, message);

    public static Result Failure(string message) => new(false, message);
}

[method: JsonConstructor]
public class Result<T>(bool isSuccess, string message, T? data) : Result(isSuccess, message)
{
    public T? Data { get; init; } = data;

    public static Result<T> Success(T data, string message = "Success") => new(true, message, data);

    public new static Result<T> Failure(string message) => new(false, message, default);
}