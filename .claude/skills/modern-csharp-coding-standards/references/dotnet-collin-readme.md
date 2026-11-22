# dotnet-collin

A reusable Codex skill for developing and maintaining Collin-style .NET applications. It gives the coding agent project conventions, implementation workflows, and focused reference notes so changes fit the existing codebase.

This repository contains instructions and examples, rather than a runnable .NET application or NuGet package.

## What the skill helps with

- **Task commits:** after relevant checks, commit each completed implementation task with `type(scope): imperative summary`, push the task branch, and verify the remote commit; preserve unrelated work and explicit no-push instructions.
- **Show result first:** explain the proposed output, scope, and impact in Burmese before editing; pause for preview-only requests and continue when implementation is already authorized.
- **Coding do/don’t:** explicit validation and Result flow, behavior-preserving refactoring, readable code, and focused verification; wallet-specific conventions stay separate.
- **Project workflow:** inspect existing code first, organize `.slnx` solutions, make small changes, and run relevant build or test checks.
- **Codebase untangling:** map hidden dependencies, unnecessary coupling, surplus code, and special cases; refactor within the requested scope and verify behavior.
- **Architecture:** feature-oriented projects, three-layer Presentation → Business → Database applications, and transport-neutral business results.
- **ASP.NET Core:** dependency injection and service lifetimes, middleware ordering, safe logging, and outbound HTTP with `IHttpClientFactory`.
- **Data access:** EF Core database-first conventions, existing Dapper patterns, pagination, query efficiency, and transaction handling.
- **Authentication and authorization:** JWT authentication and dynamic role-based access control with permission claims or database checks.
- **Configuration:** shared stage configuration, startup ordering, and secret-safe settings.
- **UI and display conventions:** date and number formats, MMK amount formatting, column alignment, dialogs, and naturally sized cards.
- **Design thinking and color theory:** readability, hierarchy, style/theme separation, semantic palette roles, contrast checks, and reversible appearance previews.
- **Documentation:** project overviews and Mermaid workflow/code-flow diagrams for junior developers.
- **`/meta-tags`:** metatags.io development previews, reusable HTML metadata, consistent page/social copy and live verification without an in-app editor.

The skill favors existing components and conventions. Its examples are starting points to adapt to a project's requirements, not a requirement to rebuild every project with the same architecture.

## Install

Clone this private repository into your Codex skills directory. Your GitHub account must have access to the repository.

Windows PowerShell:

```powershell
git clone https://github.com/sannlynnhtun-coding/dotnet-collin.git "$env:USERPROFILE\.codex\skills\dotnet-collin"
```

macOS or Linux:

```sh
git clone https://github.com/sannlynnhtun-coding/dotnet-collin.git "$HOME/.codex/skills/dotnet-collin"
```

If that directory already exists, keep your local changes before replacing it. For an existing Git checkout of this repository, update it with `git pull --ff-only` from that directory.

## Use

Name the skill in your Codex request and describe the task:

```text
Use $dotnet-collin to add a paginated product endpoint using this project's existing architecture.
```

```text
Use $dotnet-collin to review middleware ordering and dependency injection lifetimes.
```

```text
Use $dotnet-collin to improve this Blazor page's visual hierarchy and color placement while preserving its palette.
```

```text
Use $dotnet-collin to explain this project's API-to-database flow in the README with Mermaid diagrams.
```

Before using the skill for a task, the agent checks local Git status and fetches the private repository. It pulls with `--ff-only` only on a clean `main` branch when the update can fast-forward, then re-reads the updated guidance. Local edits and untracked files are preserved; a dirty checkout, different branch, diverged history, or network problem is reported without forcing an update. The agent can continue with the local skill when possible.

The agent reads `SKILL.md` and the references relevant to the task. Existing project requirements and explicit user instructions take precedence over general examples.

For structural refactoring, ask `Use $dotnet-collin to untangle this codebase` and optionally name an area. The agent asks whether to include comment and test cleanup before those optional changes.

## Repository contents

For metadata work, use `$dotnet-collin /meta-tags <live URL>`. This is a workflow shorthand inside the skill, not a standalone native slash-command registration. Add "preview first, no code or DB changes" for review-only work, or explicitly request implementation/deployment.

| File | Purpose |
| --- | --- |
| [SKILL.md](SKILL.md) | Main skill instructions and links to topic-specific guidance |
| [agents/openai.yaml](agents/openai.yaml) | Skill display name, short description, and default prompt |
| [Feature-oriented architecture](references/feature-oriented-architecture.md) | Feature structure and API-to-domain-to-UI flow |
| [Coding workflow](references/coding-workflow.md) | Reusable implementation and refactoring do/don’t rules |
| [Wallet project rules](references/wallet-project-rules.md) | Opt-in wallet conventions; not global .NET defaults |
| [Untangle codebase](references/untangle-codebase.md) | Scoped structural refactoring, decision logs, optional cleanup, and regression checks |
| [N-layered architecture](references/n-layered-architecture.md) | Layer boundaries and EF Core/Dapper responsibilities |
| [Result pattern](references/result-pattern.md) | Expected business failures and presentation-layer HTTP mapping |
| [Dependency injection](references/dependency-injection.md) | Registration, injection choices, and lifetime safety |
| [ASP.NET Core middleware](references/aspnet-core-middleware.md) | Pipeline ordering, custom middleware, and safe logging |
| [HTTP client factory](references/http-client-factory.md) | Typed/named clients, cancellation, and outbound error handling |
| [JWT dynamic RBAC](references/jwt-dynamic-rbac.md) | Authentication and permission-checking workflows |
| [Stage configuration](references/stage-config-pattern.md) | Shared configuration loading and startup flow |
| [Design, themes, and color theory](references/design-style-theme-color-theory.md) | Design priorities, palette mapping, contrast, previews, and verification |
| [Meta tags workflow](references/meta-tags.md) | `/meta-tags` inspection, proposal, implementation and live preview checks |
| [HTML metadata template](assets/meta-tags.html) | Parameterized primary, Open Graph and Twitter tags |

## Design workflow

Understand the main task → prioritize readability and hierarchy → separate visual style from palette → map semantic color roles → check contrast → support preview/commit/cancel when needed → verify rendered states and document evidence.

Preserve source palette colors when requested, and reuse the application's existing tokens and components. Passing color-token tests does not establish whole-application accessibility.

## Maintaining the skill

Keep stable, reusable conventions in `SKILL.md`. Put longer explanations and examples in `references/`, and link them from the main skill. Update this README when coverage changes. Keep credentials, real connection strings, and application data out of the repository.

The installed skill directory is the Git checkout. Edit files and run Git commands directly there; no second folder or copy step is needed. Whenever the skill changes, review and check the changes, commit them, and push to `main`. This includes changes to instructions, references, agent metadata, and this README. Preserve unrelated work, fetch remote updates first, and never force-push. Verify the remote commit after pushing; if blocked, keep the local work and report the pending push.
