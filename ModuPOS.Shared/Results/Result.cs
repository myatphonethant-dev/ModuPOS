using System.Text.Json.Serialization;

namespace ModuPOS.Shared.Results;

/// <summary>
/// Represents the outcome of an operation without payload data, conveying success or failure status and a message.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the message describing the operational outcome.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="message">The outcome message.</param>
    [JsonConstructor]
    public Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    /// <param name="message">An optional confirmation message.</param>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success(string message = "Success") => new(true, message);

    /// <summary>
    /// Creates a failure operation result.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string message) => new(false, message);
}

/// <summary>
/// Represents the outcome of an operation containing strongly-typed payload data.
/// </summary>
/// <typeparam name="T">The type of payload data.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the operational payload data, or default if failed.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="message">The outcome message.</param>
    /// <param name="data">The payload data.</param>
    [JsonConstructor]
    public Result(bool isSuccess, string message, T? data) : base(isSuccess, message)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a successful operation result containing payload data.
    /// </summary>
    /// <param name="data">The payload data.</param>
    /// <param name="message">An optional confirmation message.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T data, string message = "Success") => new(true, message, data);

    /// <summary>
    /// Creates a failure operation result with an error message and default payload.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public new static Result<T> Failure(string message) => new(false, message, default);
}
