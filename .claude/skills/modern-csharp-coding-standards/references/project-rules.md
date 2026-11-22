# Project rules
Created 2026-11-13 at the user's request after confirmation that no previous rules existed.

Follow AGENTS.md for repository conventions, domain validation, shared utilities, named API contracts, encryption, logging, testing and releases. User instructions take precedence.

## Feature service exception handling and logging

Every method in a concrete `*Service.cs` service implementation must use `try`/`catch` exception handling and structured logging, including feature, report, core banking, transport and supporting services. Interface contracts and database entity files are not service implementations.

- Write Information logs for method start, processing, completion and returning data. Cover early returns as well as the normal completion path; log only safe result metadata, such as counts or non-sensitive identifiers, never sensitive return payloads.
- Write a Warning log when validation fails, before returning the validation failure.
- In each exception catch block, write exactly one Error log with the caught exception and useful, non-sensitive context. Follow the Result pattern below for failure handling; never silently swallow exceptions or return success after a failure.
- Follow the required `[ProjectName] MethodName => Log message` prefix, using the exact emitting method name and structured placeholders. Never log passwords, PINs, OTPs, tokens, encryption material or full account numbers.

## Result pattern for operation failures

Use the existing shared `Result<T>` pattern to represent success or failure explicitly. Do not throw exceptions for expected business or validation failures, such as invalid input, missing records, duplicate records, insufficient funds or disallowed operations. Return the appropriate failure result with a safe, meaningful message instead. Do not use exceptions as control flow or return `null`, an empty collection or a default value to disguise a failure.

Callers must check the result before accessing its data or continuing dependent operations, and propagate failures without converting them into success. When changing a service to return a result, update its interface, callers and tests together. Keep first-party HTTP responses consistent with the named `Result<TResponse>` contract rule in AGENTS.md.

Unexpected infrastructure or library exceptions must be caught at the operation boundary, logged according to the service logging rule and converted to a safe `Result<T>.SystemError(...)`; do not expose exception details in the returned message. Preserve transaction rollback, resource cleanup and platform-required cancellation propagation. Expected validation failures use Warning logs rather than being thrown and caught as exceptions.

Apply BoardUI-inspired design throughout Admin and IBanking while retaining Blazor and existing components, as requested by the user. Preserve existing routes/aliases, policies, feature flags, data, development configuration and implemented business outcomes. Do not invent banking features absent from this checkout.

Use a consistent BoardUI-inspired visual language for layouts, navigation, controls and exceptional states. Existing MudBlazor, FlyonUI, Quill and report widgets may remain to preserve functionality. This is inspired styling, not a claim to use official BoardUI React components. Preserve existing license notices.

Require 100% BoardUI-inspired component styling throughout both Admin and IBanking, including every textbox, search field, select, textarea, checkbox, button, table, dialog, navigation element and exceptional state. Reuse existing Blazor components and the shared styles in `WalletIbmb.Shared/Styles/boardui-inspired.css`; use `gtb-input` and `gtb-input-label` for textboxes and their labels. Do not introduce unstyled controls or rely on vendor defaults alone. Style retained third-party widgets consistently while preserving their behavior, accessible labels, keyboard interaction, focus indicators and validation feedback.

Preserve rich-text formatting, report exports/printing and permission-tree behavior; UI restyling must not replace their engines. Retain backend layers and ASP.NET hosts; keep credentials, tokens, key rings, authorization and downstream encrypted calls on the server. Browser operations are explicit same-origin named contracts, never arbitrary proxies.

Make incremental changes, preserve rollback artifacts, and remove old UI only after equivalent behavioral tests pass. Record actual outcomes against specs/001-boardui-react-migration/. Require complete route/control coverage, relevant domain/security tests, both published bundles, anonymous health checks and rollback evidence for acceptance. Planning does not authorize release publication.

## Reusable confirmation, alert and toast components

Admin and IBanking must use reusable Blazor components for confirmation messages, alerts and toast notifications. Reuse and consolidate existing components, with shared BoardUI-inspired styling from `WalletIbmb.Shared/Styles/boardui-inspired.css`; do not duplicate notification markup or behavior in feature pages.

Implement notification state, confirmation/cancellation, dismissal and toast timing in C#/Razor. Do not use custom JavaScript, inline JavaScript handlers, JavaScript interop or JavaScript notification libraries for these behaviors. The Blazor framework runtime is permitted. Preserve existing form submissions, antiforgery, authorization, redirects and business outcomes. Keep accessible labels, keyboard operation, focus management and screen-reader announcements. Confirmation cancellation must never submit or mutate data; render message text safely rather than as raw HTML.

Alert message boxes and confirmation message boxes must appear as popups, never inline cards. Use the shared components throughout Admin and IBanking. Routine success toasts remain nonmodal corner notifications; errors must use an alert popup.

For static SSR, prefer declarative native HTML controls within these Razor components for disclosure and explicit dismissal. Keep confirmations nonmodal unless true modal focus containment is provided. Do not add global interactivity or notification timers just to display feedback.
