# Result Pattern

Use a result type for expected business outcomes that callers must handle explicitly, such as validation failures, missing records, conflicts, duplicates, or a failed business rule.

Do not convert infrastructure failures, network failures, cancellation, or programming defects into ordinary business results. Let those flow through the project's exception handling and observability path.

## Boundary

The Business layer returns a transport-neutral result. The Presentation layer maps it to HTTP semantics:

```text
Service: Result<T> with success data or a typed business error
Controller/endpoint: Result<T> -> 200/201/400/404/409/etc.
```

Do not return `IActionResult`, `IResult`, HTTP status codes, or framework response types from a business service.

## Suggested Shape

Adapt to the project's existing result implementation before creating another one. A small implementation needs an unambiguous success state, optional success value, and a machine-readable error kind plus safe client message:

```csharp
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    public static Result<T> Success(T value) =>
        new(true, value, null, ErrorType.None);

    public static Result<T> Failure(string error, ErrorType errorType) =>
        new(false, default, error, errorType);
}
```

Keep factories and invariants consistent: success requires a value when the domain requires one, success must not carry an error, and failure must not be labeled `None`. If the codebase already uses error codes, structured validation details, or a non-generic `Result`, preserve that model.

## Usage Rules

- A service returns `NotFound` when absence is an expected domain outcome, not `null` with no explanation.
- Map error types in one consistent presentation-layer location where practical, rather than repeating divergent mappings across controllers.
- Return client-safe messages. Log diagnostic details separately and never place secrets, SQL, stack traces, or internal identifiers in the result.
- Test both success and each meaningful failure mapping.
- Prefer exhaustive mapping. When a new error type is added, its HTTP mapping should be explicit.
