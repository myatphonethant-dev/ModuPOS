# Agent Rules for .NET Backend

- Always create a plan before implementing.
- Each API endpoint must have its own request model and its own response model.
- The request model must include all endpoint inputs from route, query string, and body.
- API endpoints must return response models, not EF Core entity models directly.
- Strictly separate "Get" (listing/pagination) operations from "Search" (keyword filtering) operations.
- Do not combine generic listing and keyword searching logic within a single internal method or API endpoint.
- Create distinct methods for distinct operations (e.g., implement `Get[Entity]Async` for pagination and `Search[Entity]Async` for keyword searches).
- Listing endpoints and listing pages must use pagination and order results ascending by default.
- Do not share request or response models across API endpoints.
- Shared models are allowed only when their properties match the EF Core entity exactly.
- If an API contract needs different properties than the EF Core entity, create an endpoint-specific request or response model.
- All API endpoints and I/O-bound operations must be asynchronous, utilizing `async/await` and returning `Task<T>`.
- API endpoints must return precise standard HTTP status codes reflecting the outcome (e.g., 200 OK, 201 Created, 400 Bad Request, 404 Not Found).
- Use object initializers (`{ ... }`) instead of positional constructors when instantiating DTOs and response models.
- Use traditional constructors with `private readonly` fields instead of primary constructors in services.
- Perform consistent null checks on request parameters in services (e.g., `if (request is null) return ...Failure("Request cannot be null");`).
