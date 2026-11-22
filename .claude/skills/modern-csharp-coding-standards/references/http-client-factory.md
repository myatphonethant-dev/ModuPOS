# IHttpClientFactory

Use `IHttpClientFactory` for outbound HTTP integrations such as payment, SMS, or partner APIs when that matches the target framework and existing project conventions. It centralizes client configuration while pooling underlying handlers, avoiding per-request handler churn and stale long-lived configuration.

## Choose a Client Form

- **Typed client:** default for a cohesive external API wrapper. It keeps endpoint calls, serialization, and client-specific behavior together.
- **Named client:** use when runtime code selects among several differently configured clients.
- **Basic factory client:** acceptable for small or infrequent integrations without a meaningful wrapper.

Do not add multiple forms for the same integration without a concrete need.

## Typed Client Pattern

```csharp
public sealed class WeatherApiClient(HttpClient client)
{
    public Task<WeatherResponse?> GetWeatherAsync(
        CancellationToken cancellationToken) =>
        client.GetFromJsonAsync<WeatherResponse>("weather", cancellationToken);
}

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["WeatherApi:BaseUrl"]
        ?? throw new InvalidOperationException("Weather API URL is missing."));
});
```

Use validated configuration rather than hardcoded production endpoints. Keep API keys, bearer tokens, and other secrets in an approved secret provider, not source or committed configuration.

## Operational Rules

- Pass `CancellationToken` through every outbound call.
- Check non-success status codes and map expected partner failures deliberately; do not deserialize an error response as a success DTO.
- Set timeouts appropriate to the operation and distinguish caller cancellation from timeout when handling failures.
- Use resilience handlers only when the project supports them and the operation is safe to retry. Never blindly retry non-idempotent requests such as payments.
- Use structured logging without authorization headers, cookies, tokens, sensitive query values, or unrestricted bodies.
- Do not create and dispose a new raw `HttpClient` for every request.
- Do not capture a factory-created client or typed client in a singleton unless its lifetime and handler behavior have been intentionally designed and verified. Prefer injecting the factory into long-lived services when necessary.

## Testing

Keep external API behavior behind the typed client boundary. Test request construction, status handling, serialization, cancellation, and redaction using a controlled `HttpMessageHandler` or the project's existing HTTP test infrastructure; do not call a live third-party API in unit tests.
