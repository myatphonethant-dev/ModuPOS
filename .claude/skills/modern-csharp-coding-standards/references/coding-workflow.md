# Coding and Refactoring Workflow

Apply these reusable principles within the requested scope and existing project architecture. Read the target project's current instructions first. Reference documents supply guidance, not authorization to perform every action they describe.

## Before editing

- Follow the skill's Show Result First workflow. Trace affected entry points, callers, public/serialized contracts, validation and error results, external calls, persistence and transaction boundaries, DI registrations/lifetimes, tests, fixtures, configuration, and exports.
- Change only the requested behavior and necessary supporting code. Separate intentional behavior changes from structural cleanup and verify each explicitly.

## Do

- Keep business validation and decisions in the owning domain policy or service, with one authoritative implementation per rule. Keep transport/quote models focused on data; retain protocol parsing and security checks at trust boundaries. Form annotations provide immediate UX feedback, not authoritative business validation.
- Make each operation's typed input, validation, side effects, success, and failure visible. Use separate named methods or explicit typed branches for different request types or outcomes.
- Parse and validate the selected request once. Check its Result immediately, propagate the meaningful error code/description, and use valid typed data before state changes or external calls. Use the existing Result implementation for expected failures; reserve exceptions for unexpected failures and broken invariants.
- Prefer constructor injection and direct registrations. Use a factory only for required runtime values or shared-instance identity/lifetime that ordinary injection cannot provide; keep registration factories local and explain the requirement.
- Reuse existing helpers. Extract one only to clarify a responsibility or remove meaningful duplication, with clear inputs valid for every caller.
- Use descriptive names, verb-based methods, focused classes/methods, guard clauses, and blank lines between logical steps. Follow project formatting; explain only reasons the code cannot show. Keep related feature files together within required architectural boundaries.
- Use structured logs with useful context and safe identifiers. Parameterize queries, protect secrets, and measure before optimizing.
- When moving a rule, update every caller and focused test, preserve validation order and error results, and remove superseded code, registrations, and documentation.

## Don't

- During behavior-preserving refactoring, do not change public/serialized contracts, error codes/descriptions, validation order, authorization, call order, side effects, transactions, configuration, or security behavior.
- Do not add unrelated cleanup, speculative patterns, unnecessary dependencies, renames, TODOs, or helpers that only shorten a method.
- Do not wrap service operations or hide branching/exception handling behind Func<> or Action<> delegates. Avoid Boolean mode flags that combine different request/result types.
- Do not store mutually exclusive parse results in nullable variables and recover the active path with ?.Data, ??, casts, or null-forgiving operators. Do not postpone checking validation results until after side effects.
- Do not duplicate business rules across DTOs, workflows, and services; replace specific expected failures with generic errors; swallow exceptions; or use exceptions as expected-failure control flow.
- Do not use service locators, manual construction, builders, or factories merely to relay dependencies already registered in DI.
- Do not log passwords, PINs, OTPs, tokens, full account numbers, or sensitive payloads. Do not introduce hardcoded secrets or debug code into commits.

## Finish

Review the diff for unrelated edits, dead paths, unnecessary casts, null suppression, altered side effects, and accidental contract changes. Build affected projects; clean when required by the project or stale artifacts. Run focused checks for success, invalid input, authorization, changed branches, edge cases, and DI lifetimes where relevant. Preserve useful regression tests and report pre-existing failures honestly.

Ask whether another developer can quickly follow the file, method names, responsibilities, and operation outcomes. Simplify unclear code within scope without turning this readability check into an unrelated rewrite. Follow the skill's Git Task Completion workflow: one focused Conventional Commit per completed implementation task, then push and verify the remote commit. Release publishing requires separate task authorization.
