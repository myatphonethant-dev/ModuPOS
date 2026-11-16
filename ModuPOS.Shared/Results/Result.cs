namespace ModuPOS.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorMessage) => new(false, errorMessage);

    public static Result<T> Success<T>(T data) => Result<T>.Success(data);
    public static Result<T> Failure<T>(string errorMessage) => Result<T>.Failure(errorMessage);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public new static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);

    public static implicit operator Result<T>(T data) => Success(data);
}
