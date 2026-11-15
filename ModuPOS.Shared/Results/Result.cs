namespace ModuPOS.Shared.Results;

/// <summary>A stable, client-visible failure: <paramref name="Code"/> is part of the API contract.</summary>
public sealed record Error(
    string Code,
    string Message,
    int Status,
    IReadOnlyDictionary<string, object?>? Extensions = null);

public readonly struct Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);

    public static Result<T> Fail(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Ok(value);

    public static implicit operator Result<T>(Error error) => Fail(error);
}
