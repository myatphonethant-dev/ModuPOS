# Agent Instructions

These instructions apply to the all the ModuPOS projects.

# Documentation Guidelines

To know more about this project, please navigate to /docs.
To know more about the project's rules, please navigate to /rules.
If any changes are made to the project, please update the documentation.

## Human Oversight

All agent work is subject to human review and approval.

- Do not interpret a request to implement a change as permission to commit or publish it.
- Do not bypass required human decisions, repository protections, reviews, or approval gates.
- Clearly report incomplete work, assumptions, risks, and validation failures. Do not present uncertain work as complete.

## Planning Changes

Before making a nontrivial code change:

1. Inspect the relevant implementation, tests, repository guidance, and current working-tree state.
2. Define a concise plan that covers the intended behavior, affected surfaces, regression tests, and validation.
3. Present the plan before implementation when the user requests a plan or when scope, behavior, data migration, compatibility, security, or architecture requires a human decision.
4. Obtain human direction instead of guessing when multiple materially different behaviors are reasonable.
5. Keep changes focused on the approved request and update the plan if investigation reveals a meaningful scope change.

Small, unambiguous, low-risk changes may proceed without a separate approval pause, but they still require human review before any commit or publication.

## Required Standards

Before changing code, read and follow the coding standards in the current project.

- Preserve nullable reference type safety and use file-scoped namespaces.
- Document every non-private C# entity with XML documentation.
- Keep C# lines within 225 characters.
- Use explicit `// Arrange`, `// Act`, and `// Assert` sections in every test.
- Follow the repository's existing naming, formatting, error-handling, and test patterns.
- Add or update focused regression tests for behavior changes.
- Do not weaken analyzers, warnings, tests, or coding-standard checks to make a change pass.

## Required Validation

Run the smallest relevant tests while developing. For example, before declaring a code change complete in .NET, run:

```powershell
dotnet build Lineup.slnx -c Release --no-restore
dotnet test Lineup.slnx -c Release --no-build --no-restore
dotnet format Lineup.slnx --verify-no-changes --no-restore
git diff --check
```

Restore dependencies first only when they are missing or dependency manifests changed.

Documentation-only changes do not require a build or test run unless they affect generated documentation or validation tooling.

## Reviewing Changes

Before reporting implementation complete:

1. Review the entire diff, including staged and unstaged changes, without reverting unrelated human work.
2. Confirm every changed line supports the requested outcome.
3. Check error paths, cancellation, nullable safety, logging, compatibility, and relevant edge cases.
4. Verify tests assert the requested behavior rather than an indirect proxy.
5. Run the required validation gate and resolve failures caused by the change.
6. Report the files and behavior changed, validation performed, remaining risks, and any work requiring human attention.

Agent self-review supplements but never replaces human code review. Stop after presenting the working-tree changes unless the user explicitly authorizes a subsequent Git or publication action.

# Commit attribution

Use the selected person's verified Git name and email. If identity is not available from repository history, ask for it. Authentication tokens do not determine commit authorship. Do not change global Git identity settings. Never add an agent as a contributor.

Keep SECRETS.md local and ignored by Git. Never stage, commit, print, or copy its credentials into tracked files or commit messages.

# Commit messages

Use Conventional Commit messages in the form `type(scope): description`, such as `fix(docker): restore local database connectivity` or `docs(workdlow): define commit convention `. Keep each task in a separate commit, grouping only changes needed for that task.
