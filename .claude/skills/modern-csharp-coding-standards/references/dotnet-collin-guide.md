---
name: dotnet-collin
description: Dotnet Collin workflow notes for Collin-specific .NET projects. Use when the user names dotnet-collin, works on the Collin .NET codebase, creates or fixes `.slnx` solution layout, updates README project documentation, works with EF Core or Dapper data access, implements N-layered architecture, Result Pattern, dependency injection, ASP.NET Core middleware, or IHttpClientFactory, documents or implements JWT Dynamic RBAC, untangles or structurally refactors a codebase, applies feature-oriented architecture or stage config conventions, designs UI styles, themes, color roles or appearance previews, requests /meta-tags or a metatags.io metadata workflow for a .NET app, or asks to capture a reusable Collin project convention.
---

# Dotnet Collin

## Before Using This Skill

- At the start of each task using this skill, run Git commands from the installed skill directory. Check `git status --short --branch` and confirm `origin` points to `https://github.com/sannlynnhtun-coding/dotnet-collin`, then run `git fetch origin` to check for updates.
- When the working tree is clean, the current branch is `main`, and the update can fast-forward, run `git pull --ff-only origin main`. Re-read `SKILL.md` and any needed references after pulling so the task uses the updated guidance.
- Preserve staged, unstaged, and untracked local changes. If any exist, the branch differs, or histories diverge, do not pull automatically; report the pending update and continue with the local skill where possible. Do not automatically stash, reset, clean, switch branches, rebase, or overwrite local work to update the skill.
- If authentication or connectivity prevents checking or pulling, report that freshness could not be verified and use the available local skill where possible. Never claim it is up to date without a successful remote check.

## Workflow

- Read the touched project files first: `.sln`, `.slnx`, `.csproj`, `Program.cs`, appsettings, tests, and nearby code.
- When creating a new `.slnx`, create a dedicated folder for it first. If generated files land outside the folder, create the folder and move the `.slnx`, projects, config, and related files into it before continuing.
- Prefer existing project patterns over new abstractions, packages, or folder structures.
- Use `dotnet` CLI commands already implied by the repo; add a new tool only when the project already uses it or the user asks.
- Keep changes small and verify with the narrowest relevant command, such as `dotnet test`, `dotnet build`, or a targeted test project.

## Git Task Completion

- After each completed implementation task and its relevant checks, commit only that task's changes and push the current task branch to its configured remote. The user authorizes this commit/push workflow; honor any later review-only, no-commit, no-push, or branch restrictions. Do not create empty commits for explanation-only tasks.
- Use one focused Conventional Commit per task: `type(scope): imperative summary`, such as `feat(customer): add customer search`, `fix(login): handle expired token`, or `docs(skill): clarify workflow`. Use `!` and a `BREAKING CHANGE:` footer for intentional breaking changes.
- Review the staged diff, preserve unrelated local/staged work, and run required checks before committing. Use the user's exact requested branch name; never automatically prepend `codex/`. Never force-push or rewrite history without explicit authorization.
- Verify the pushed branch's remote commit matches the local commit and report the commit ID and checks. If checks, access, conflicts, or connectivity block completion, retain the work and report the pending step. A task commit/push does not authorize tagging, release publishing, or deployment.

## Show Result First

- Before editing, explain the understood request, proposed resulting behavior or output, affected files, and material impacts in Burmese unless the user requests another language. Clearly label the proposal as a preview, not an implemented or verified result.
- For preview/review-first requests, show the proposed result and wait before implementation. When implementation is already authorized, present the preview and continue without asking for permission again.
- After implementation, report what changed, checks actually performed and their results, and remaining limitations. Never present an unrun check as passing.

## Coding and Refactoring Do / Don't

- Read [references/coding-workflow.md](references/coding-workflow.md) for implementation and refactoring: scope tracing, domain validation, explicit typed control flow, Result propagation, dependency injection, readability, and focused checks.
- Wallet conventions are separate: read [references/wallet-project-rules.md](references/wallet-project-rules.md) only for `mob-wallet-agent-merchant` work or when explicitly requested. They are not defaults for other .NET projects; the target project's current instructions remain authoritative.

## Agent Rules

- Understand before coding: read project structure, requirements, conventions, and architecture; ask when business logic is unclear.
- Make the smallest necessary change, preserve existing behavior, and never invent schema, API responses, config values, environment variables, secrets, or business rules.
- Respect existing architecture and patterns, including feature-oriented architecture, Clean Architecture, CQRS, repository/service/result patterns, MVC/Minimal API style, and existing data-access choices.
- Write production ASP.NET Core code with validation, error handling, logging, `CancellationToken`, async/await, dependency injection, authentication, authorization, and secret-safe configuration.
- Review performance and EF/SQL work for N+1 queries, unnecessary `ToList()`, over-fetching, missing pagination/indexes, repeated database calls, and avoid `.Result`, `.Wait()`, or `Task.Run` inside requests.
- Before done, check syntax, namespaces, dependencies, nullable warnings, build/test results, security impact, side effects, breaking changes, and touched tests/docs.

## Display Formatting

- Use `dd-MM-yyyy` for dates and `dd-MM-yyyy hh:mm tt` for date-times in C# display code.
- Show numbers and amounts with thousand separators; MMK amounts must be suffix style, for example `1,000.00 MMK`, not `MMK 1000`.
- Use readable table/report headers with spaces, for example `CreatedDate` becomes `Created Date`.
- Align text columns left, amount/numeric columns right, and keep only serial/action/checkbox columns centered.
- Keep large dialogs within the viewport and use a vertical scrollbar for overflowing dialog content.
- Provide a close button in both the header and footer of dialogs.
- For card collections with different content lengths, let each card keep its natural height and keep horizontal and vertical gaps consistent. Use independent column stacking when needed so shorter cards do not leave extra space before the next card; do not force equal card heights or aligned row bottoms.

## Design Thinking, Styles, Themes, and Color Theory

- For UI design or appearance changes, read `references/design-style-theme-color-theory.md` before implementation.
- Follow the flow: understand the main task -> prioritize readability and hierarchy -> separate style from palette -> map semantic color roles -> verify contrast -> implement preview/commit/cancel when needed -> check rendered states and document evidence.
- Reuse existing components, tokens, palettes, and appearance controls. Add style/theme selectors or presets only when requested or required by the existing product.
- Preserve source colors when requested; report palette limitations and actual verification without claiming complete accessibility from token tests alone.

## /meta-tags

- Treat `/meta-tags` as this skill's named workflow for reviewing or implementing page metadata with metatags.io; read [references/meta-tags.md](references/meta-tags.md).
- Use [assets/meta-tags.html](assets/meta-tags.html) as the reusable HTML template. Preserve the project's URL and branding unless the user requests changes.
- Use metatags.io as a development preview/generation tool, not an embedded app editor or runtime dependency. Respect review-only requests; implementation and deployment follow the user's existing authorization.

## Stage Config

- Prefer the Collin stage config pattern for shared app configuration: root `Config/appsettings.json` contains stable defaults and `Stage`; root `Config/custom-settings-{stage}.json` contains stage-specific values.
- Add a shared `AddStageConfig(WebApplicationBuilder builder)` extension when multiple .NET apps need the same config. It should load `Config/appsettings.json`, read `Stage`, then load `custom-settings-{stage}.json`.
- If apps live below folders like `src/<Project>`, make the extension search parent folders for `Config` instead of hardcoding one relative path.
- Call `builder.AddStageConfig()` immediately after `WebApplication.CreateBuilder(args)` in each web app.
- Remove duplicated per-app `appsettings*.json` values once shared config owns them; keep per-app files only for values that are genuinely app-specific.
- Do not store secrets in committed stage config. Use placeholders or local ignored overrides when real credentials are needed.
- For step-by-step code flow, read `references/stage-config-pattern.md`.

## Documentation

- When explaining a project for junior developers, update `README.md` with a clear project overview, workflow, Mermaid flowchart, and Mermaid code flow chart.

## Feature-Oriented Architecture

- For feature-oriented .NET project structure, API-to-domain-to-UI code flow, and junior-developer diagrams, read `references/feature-oriented-architecture.md`.

## Untangle Codebase

- When the user explicitly requests codebase untangling or a structural refactor, read [references/untangle-codebase.md](references/untangle-codebase.md) and complete that workflow within the requested scope.
- Ordinary feature and bug-fix requests do not authorize a whole-codebase refactor. Preserve existing architecture unless restructuring is part of the request.

## N-Layered Architecture

- When the project uses or requests the Collin three-layer flow (`Presentation -> Business -> Database`), read `references/n-layered-architecture.md`.
- Do not introduce a repository layer merely to wrap EF Core or Dapper. Preserve one when it already contains meaningful abstraction or the project requires it.

## Result Pattern

- For predictable business failures represented without exceptions, read `references/result-pattern.md`.
- Keep HTTP concepts in the presentation layer: services return business results and controllers or endpoints map them to status codes.

## ASP.NET Core Application Patterns

- For service registration, constructor injection, lifetime selection, and lifetime-safety rules, read `references/dependency-injection.md`.
- For request-pipeline ordering, custom middleware, short-circuiting, request timing, or safe request/response logging, read `references/aspnet-core-middleware.md`.
- For outbound HTTP integrations, typed/named clients, handler reuse, and cancellation, read `references/http-client-factory.md`.

## Data Access

- Use EF Core by default.
- Prefer EF Core database-first for Collin projects. Use EF Core InMemory for lightweight/test/demo databases and MSSQL for real persistence unless the existing project already uses another provider.
- Keep existing Dapper, ADO.NET, raw SQL, or EF usage unless the user asks to change it or the project clearly requires it.

## JWT Dynamic RBAC

- For JWT Dynamic RBAC implementation, explanation, README, or junior-developer code-flow work, read `references/jwt-dynamic-rbac.md`.

## Knowledge Capture

- When the user says a new Collin workflow or feature rule needs to be remembered, update this skill.
- Store only stable, reusable guidance here. Put long commands, schemas, or examples in `references/` only after they become too large for this file.
- Delete stale rules instead of keeping historical notes.
- The installed skill directory is the Git checkout of the private repository at `https://github.com/sannlynnhtun-coding/dotnet-collin`. Edit and run Git commands directly from this skill directory; do not maintain a second copy. Before updating, inspect local changes and fetch remote changes; preserve unrelated edits and resolve conflicts without overwriting work. Whenever this skill changes (including `SKILL.md`, `references/`, `agents/`, or its README), review the diff, check reference links, commit only the skill changes, push to `main` without force, and verify the remote commit matches. The user has authorized these commits and pushes for future skill updates; no additional confirmation is needed. If access, conflicts, or connectivity block completion, retain local changes and report that the push is pending.
