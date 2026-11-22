# ASP.NET Core Middleware

Middleware forms an ordered request/response pipeline. Each component may run logic before and after the next component, or short-circuit by returning without calling `next`.

## Ordering

Inspect the complete `Program.cs` pipeline before inserting middleware. Position depends on which requests a component must observe or protect. Preserve established framework ordering and verify at least these invariants:

- Exception handling is early enough to catch failures from downstream components.
- HTTPS/HSTS and static-file behavior follow the application's hosting requirements.
- CORS runs in the routing/security portion of the pipeline required by the app's endpoint model.
- Authentication runs before authorization.
- Middleware that depends on the selected endpoint runs after routing has selected it and before endpoint execution.
- Middleware placed after a short-circuiting component will not see short-circuited requests.

Do not copy a generic ordering list without checking the app's target framework, endpoint style, reverse proxy, static files, CORS, authentication, and exception-handler configuration.

## Custom Middleware

Use inline middleware only for small, local behavior. Use a named middleware class for reusable or substantial logic:

```csharp
public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation(
                "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds:F1} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}

app.UseMiddleware<RequestTimingMiddleware>();
```

Use structured `ILogger` calls rather than `Console.WriteLine`. Avoid synchronous I/O, blocking calls, or large allocations in the request path. Pass request cancellation to downstream asynchronous work.

Middleware instances constructed at application startup must not capture scoped services in their constructors. Resolve scoped services in `InvokeAsync` parameters or use `IMiddleware` with an appropriate registration when the behavior requires scoped dependencies.

## Safe Request/Response Logging

Do not log unrestricted bodies, query strings, headers, cookies, or form data. They can contain passwords, bearer tokens, payment data, and PII.

Prefer an allowlist: endpoints place reviewed, non-sensitive fields into a structured logging model or `HttpContext.Items`; middleware records only those fields after the response completes. Adding a field requires a sensitivity review. Limit payload size and cardinality, and keep diagnostic internals out of client responses.

## Verification

Test both the request and response path, exception behavior, short-circuit behavior, and ordering relative to routing and security. For logging middleware, verify sensitive fields are absent—not only that expected fields are present.
