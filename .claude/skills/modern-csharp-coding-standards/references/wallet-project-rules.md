# Wallet Project Rules (Opt-in)

Source: `mob-wallet-agent-merchant/AGENTS.md`, reviewed 13-11-2026. This is a project-specific reference, not global .NET policy or permission to modify, release, or deploy a project. Apply only to that wallet project or on explicit request, and read its current AGENTS.md and referenced protocol docs before implementation. Do not apply these restrictions to other projects by default.

## Domain validation and utilities

- The domain layer owns CRUD validation; API DTOs are transport contracts and Blazor annotations provide UX feedback only.
- Do not reject strings/identifiers by length in FeatureValidationPolicy classes or add MaximumIdLength, MaximumSearchTermLength, MaximumUserNameLength, MaximumFullNameLength, MaximumRoleCodeLength, MaximumBranchCodeLength, MaximumStaffIdLength, MaximumPhoneNumberLength, or MaximumEmailLength validation constants. Operational limits such as MaximumPageSize and cryptographic key sizes remain valid.
- Keep shared utilities, page-size options/control/validation in the Shared project's DevCode.cs; reuse them. Applicable static utilities use an extension-method `this` first parameter.
- Preserve existing development credentials/configuration/documentation values during refactoring unless a credential change is requested. This does not permit new committed secrets, production secrets, or disclosure of sensitive values.

## API, HTTP, health, and EF

- Each new/modified first-party business endpoint accepts exactly one named request model and returns Result<TResponse> with a named endpoint-specific response model. Lists and pagination may be inside it; primitives, raw collections, anonymous payloads, and bare PagedResult<T> must not cross the HTTP boundary. Vendor callbacks, SSR navigation, and operational health endpoints are exempt.
- Use AddHttpClient, inject IHttpClientFactory, and call CreateClient for outbound HTTP. Do not instantiate HttpClient directly or inject it into application services. This overrides the generic typed-client preference only in this project.
- Every web project registers health checks and maps /health with MapHealthChecks, allowing anonymous probes even with fallback authorization. Keep it process-only; use a separate readiness route for dependencies.
- Every EF scaffold command includes --no-onconfiguring. Use FirstOrDefaultAsync rather than SingleOrDefaultAsync. Use EF only for straightforward CRUD; use stored procedures for complex queries, reports, and multi-table joins. Do not add LINQ join, Join, or GroupJoin.

## Server-to-server encryption

For IBanking -> Gateway -> Backend, follow the current docs/ebmb-encryption.md wire format; do not invent the protocol from this summary.

- Encrypt business requests/responses on both hops with platform AesGcm (AES-256-GCM). Independent key rings protect each hop; Gateway decrypts/re-encrypts. Keep crypto operations, key validation, and associated-data construction in WalletIbmb.Shared/DevCode.cs. Base64 is encoding, never encryption or a plaintext fallback.
- Use random 32-byte keys, a fresh 12-byte nonce per encryption under a key, and a 16-byte tag. Never reuse a key/nonce pair.
- Use versioned named encrypted request/response envelopes and Result<NamedEncryptedResponseModel>, carrying keyId, nonce, ciphertext, and tag. Authenticate HTTP method, route, direction, key ID, and applicable response status as associated data. Business decisions trust the authenticated inner result, not outer metadata.
- Load ActiveKeyId and key dictionary from protected deployment configuration; never commit new keys. Validate at startup and fail closed. Encrypt with the active key and decrypt by keyId; never silently try another key.
- Distribute rotation keys to both peers before activation. Retain old decrypt keys until in-flight messages clear, then remove them.
- Reject unsupported versions, unknown keys, malformed envelopes, and authentication failures generically. Never log keys, plaintext, ciphertext, tags, or sensitive payloads.
- Keep HTTPS and authentication. Require shared replay/idempotency protection before financial operations; encryption alone does not prevent replay. Test round trips, tampering, incorrect associated data, rotation, and both hops.

## Logging and Serilog

- Every emitted log uses `[ProjectName] ExactEmittingMethodName => Message` with structured placeholders, concise action/context, and safe identifiers. Do not interpolate structured values or use context-free Error/Success messages.
- Every web Program.cs configures Serilog text logs at Information to console and an hourly rolling logs/{Assembly.GetEntryAssembly()?.GetName().Name}.log file, registers builder.Services.AddSerilog(), logs startup/fatal termination with `[ProjectName] Main => ...`, and calls Log.CloseAndFlush() in finally. Setup examples show only Serilog-related code.

## Naming and enum defaults

Use PascalCase SQL table names. New enums declare None = 0 first and valid business choices start at 1. Do not silently renumber existing persisted/serialized enum values during refactoring; make any required migration explicit.

## Release workflow (only when authorized)

Inspect the latest release's artifact names, release names, and note conventions. Keep project/package versions consistent. Use focused Conventional Commits and a dedicated chore(release): prepare vX.Y.Z commit. Resolve validation failures before tagging/publishing; run relevant tests and a release build, and build/sign/checksum every expected artifact. Where locally installable, install and verify version/status. Push the branch and annotated version tag, publish matching GitHub release notes/assets, verify remote asset digests, and confirm clean synchronized Git state. This checklist itself does not authorize installation, tagging, pushing, or publishing.
