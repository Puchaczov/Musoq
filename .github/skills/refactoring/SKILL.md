---
name: refactoring
description: 'Use when refactoring Musoq code for maintainability: compact code, remove duplication, simplify control flow, apply DRY/SOLID, extract reusable abstractions, improve naming, preserve behavior, and run repo validation.'
argument-hint: 'target files, module, code smell, or refactoring goal'
---

# Refactoring

## Outcome

Make existing code smaller, clearer, and easier for humans to review and maintain while preserving behavior unless the user explicitly asks for a behavior change.

## Workflow

1. Establish the scope: identify the target files, module, code smell, constraints, and behavior that must remain unchanged.
2. Load `.copilot_session_summary.md`, `.github/copilot-instructions.md`, and the applicable per-project instructions before editing. Check the worktree so unrelated user changes are preserved.
3. Read the surrounding code before deciding on a design. Find existing patterns, call sites, tests, and duplicate implementations.
4. Map the maintainability problem in concrete terms: duplication, long methods, nested control flow, unclear naming, magic values, dead code, leaky abstractions, mutable state, or oversized responsibilities.
5. Choose the smallest refactoring that improves the code at the root cause. Prefer local simplification over new abstractions unless the abstraction removes real duplication or matches an established project pattern.
6. Edit in small behavior-preserving steps. Keep the diff focused on the requested area and avoid opportunistic rewrites outside the scope.
7. Validate with the narrowest relevant tests first, then run the broader validation required by the repository instructions.
8. Re-read `.github/copilot-instructions.md` and verify the modified code follows the repository rules before final validation.
9. Self-review the diff for readability, duplication, SOLID/DRY fit, naming, control flow, public contract safety, and unnecessary churn.
10. Report what changed, how behavior was protected, and any remaining risks or follow-up candidates.

## Decision Points

- If the requested refactor would change public APIs, serialized shapes, generated output contracts, or user-visible behavior, ask before making that change.
- If two code blocks look similar but represent different domain concepts, keep them separate or extract only the truly shared primitive.
- If duplication appears for the second time in the same change, extract a method, type, or reusable data structure instead of copying again.
- If a method mixes distinct responsibilities, split it along the natural behavior boundaries rather than introducing flags or boolean parameters.
- If a new abstraction has only one use and does not clarify a boundary, inline or simplify instead.
- If performance optimization becomes part of the refactor, establish a baseline before changing performance-sensitive code and compare after.
- If tests are already failing, determine whether the failures are related to the refactor before treating them as blockers.

## Musoq Gates

- Read the relevant project `copilot-instructions.md` before touching code in `src/dotnet/Musoq.Parser`, `Musoq.Evaluator`, `Musoq.Converter`, `Musoq.Schema`, `Musoq.Plugins`, `Musoq.Playground`, or `Musoq.Benchmarks`.
- Read `musoq_enchanced_architecture.md` before touching IR planner, Execution IR, physical planning, or renderer code.
- Keep optimization decisions in `QueryPlanner`, planner-owned helpers, physical planning, or Execution IR. Do not hide query-level strategy choices in renderers or generated C#.
- Before final validation, re-read `.github/copilot-instructions.md` and fix any compliance issues found in the changed code.
- After code changes, run the relevant focused tests first, then the full solution command unless the user explicitly narrows validation or the environment blocks it:

```bash
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --logger "trx"
```

- For source changes that require compilation before tests, build with:

```bash
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet
```

- Update `.copilot_session_summary.md` at the end with completed work, validation status, next steps, and context notes.

## Refactoring Tactics

- Replace nested conditionals with guard clauses and early returns.
- Extract pure functions for repeated calculations, predicates, and conversions.
- Prefer clear domain names over comments that explain unclear variables or methods.
- Replace magic values with named constants or well-named local variables.
- Remove dead code, commented-out code, redundant branches, unused locals, and unnecessary indirection.
- Use parameter objects or records when related values travel together through several calls.
- Keep interfaces small and avoid adding new interfaces for a single implementation without a concrete need.
- Prefer immutable data and readonly state where values do not change after construction.
- Preserve existing style, formatting, dependency choices, and test patterns.

## Completion Checks

- The refactor is behavior-preserving unless the requested outcome explicitly says otherwise.
- The code is easier to read in the diff than before: fewer branches, less repetition, clearer names, or a sharper responsibility boundary.
- Any new abstraction has a clear purpose, a good name, and removes meaningful duplication or complexity.
- Public contracts, data shapes, and compatibility assumptions are preserved or explicitly approved by the user.
- Relevant focused tests and the Musoq full validation gate were run according to repository guidance, or any inability to run them is reported.
- Unrelated user changes remain untouched.