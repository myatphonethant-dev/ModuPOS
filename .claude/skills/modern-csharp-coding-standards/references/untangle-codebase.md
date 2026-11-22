# Untangle Codebase

Use this workflow only for an explicit untangling or structural-refactor request. An optional named area limits the scope. The aim is to make related changes land together, reduce unnecessary dependencies, and preserve intended behavior. Do not impose a new architecture or expand into unrelated features or bug hunting.

## 1. Set scope and optional cleanup

Read the project's instructions, solution/projects, nearby code, callers, tests, fixtures, configuration, and exports before editing. Identify public contracts, persistence formats, migrations, authorization, and other behavior that restructuring could break. Establish the relevant build/test baseline and record existing failures.

Present these optional additions with these exact descriptions and ask which to include:

| Addition | What it does |
| --- | --- |
| **Comments** | Remove comments that only restate the code. |
| **Tests** | Remove tests that wouldn't catch a real bug, and fix the ones that fail without one. |

Continue read-only investigation while awaiting the answer; perform optional cleanup only when selected. Preserve regression coverage throughout the refactor.

## 2. Inventory before changing code

Ask of each boundary: **should these pieces change together?** Record concrete file/symbol evidence in a decision log for:

- **Hidden links:** duplicated logic or values; globals or files set elsewhere; related feature code scattered across folders; undocumented call order or conventions.
- **Needless links:** callers depending on internals; stable code depending on volatile details; general functions using one caller's vocabulary; shallow modules whose interfaces change with their implementation.
- **Surplus:** pass-through wrappers, unused extension points or configuration, and dead code. Check reflection, DI registration, serialization, generated code, and external consumers before calling .NET code unused.
- **Special cases:** one-value branches, caller-specific parameters, swallowed exceptions, retries/sleeps, and disabled tests. Determine the requirement behind each; their presence alone does not justify removal.

Use Git history as evidence of what changes together. Summarize large areas or inspect a dependency graph when that makes the investigation clearer.

## 3. Design the smallest complete fixes

Group each proposed fix above the problems it solves in the decision log. Prefer a shared root-cause fix over patching each special case. Keep pieces that change together together, separate pieces that change for different reasons, and depend on stable contracts. Reuse existing project patterns; add abstractions only when the current requirements justify them.

For example:

- Restore dialog focus to the previous element through the existing dialog component.
  - One caller waits before restoring focus.
  - Another sets a global flag to suppress restoration.

Do not collapse required Presentation/Business/Database boundaries merely to colocate files. Preserve validation, cancellation, error handling, security, accessibility, transaction behavior, and external contracts.

## 4. Implement and verify

Complete the decision log within the authorized scope. Update all affected callers, tests, fixtures, configuration, and exports. Remove special cases made unnecessary by the root fix; retain those still required. Verify each meaningful change with the narrowest relevant build or behavioral test, then run the appropriate end-to-end scope checks.

If **Comments** was selected, remove comments that restate implementation. Keep explanations of external constraints; remove workaround comments only when their underlying workaround is gone.

If **Tests** was selected, do that cleanup last. Remove tests that cannot fail or merely mirror implementation only after confirming useful behavior coverage remains. Rewrite tests tied to internals around observable behavior. Preserve tests that catch real regressions.

## 5. Review and finish

Use subagents to independently review the scoped changes for remaining hidden links, needless links, surplus, and unexplained special cases. Address findings worth fixing and repeat the relevant checks after changes. Do not expand beyond scope just to reach zero findings.

Finish when typical changes have clear ownership, dependencies are justified, remaining special cases have known requirements, reviewers have no actionable findings within scope, and intended behavior is verified. Report checks performed, pre-existing failures, remaining limitations, and any intentional behavior changes. Never claim unchanged behavior or passing tests without evidence.
