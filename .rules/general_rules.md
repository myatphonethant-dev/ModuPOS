# Project AI Agent Instructions

## Core Coding Rules

- **Surgical Edits:** Modify only the files and lines necessary to fulfill the request. Do not reformat adjacent functions or clean up unrelated dead code unless asked.
- **YAGNI & Simplicity:** Use the simplest solution that works. Use standard library and existing helpers before introducing new packages.
- **No Speculative Abstractions:** Do not create interfaces or factories for single implementations.
- **Style Consistency:** Match the naming conventions, error-handling patterns, and syntax of surrounding code.

## Verification Workflow

1. Before declaring a task complete, run the project's build and relevant tests.
2. If tests fail, diagnose the root cause—do not patch symptoms or weaken assertions to force a pass.
3. If dependencies or configurations need to change, state why explicitly before applying.

