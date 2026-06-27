## Code Quality & Maintainability Standards

**Your job is not just to deliver working features — it is to deliver MAINTAINABLE code.** Every human who reads your output must be able to understand, modify, and extend it without a guide. Code is read ~10x more than it's written, so optimize for the reader, not the writer. Always ask: "Would a teammate new to this codebase understand what I just wrote without asking me?"

### Control Flow
- **Fail fast / return early.** Validate at the top and bail out. Don't wrap the entire method body in a success branch — the happy path should have the least indentation.
- **No `else` after `return` / `throw` / `continue`.** It's redundant nesting. The early exit already handled the branch.
- **Ternary is for simple assignments only.** If the ternary has side effects, method calls, or nests another ternary — use `if`/`else` instead.
- **Prefer `switch` expressions over `switch` statements** when the result is an assignment or return. They're more concise and exhaustiveness-checked by the compiler.
- **Use pattern matching** (`is`, `when`, property patterns) instead of type-check-then-cast chains. Write `if (obj is Foo foo)` not `if (obj is Foo) { var foo = (Foo)obj; ... }`.

### Methods / Functions
- **If you need to scroll to read a method, it's two methods.** Split on natural abstraction boundaries, not arbitrary line counts.
- **A method that does X *and* Y should be two methods.** "And" in a method name is a code smell — `ValidateAndSave`, `ParseAndTransform` — split them.
- **Boolean parameters are a red flag.** They usually mean the method has two personalities. Prefer two clearly named methods or an enum parameter.
- **More than 3-4 parameters → you're missing a concept.** Introduce a class, record, or parameter object to group related values.
- **Keep cyclomatic complexity low.** If a method has more than ~3 independent branch paths, it likely needs decomposition into smaller focused methods.
- **Prefer pure functions where possible.** A function that takes input and returns output with no side effects is trivially testable and easy to reason about.

### Naming
- **If you need a comment to explain what a variable or method does, the name is wrong.** Fix the name, delete the comment.
- **Don't encode the type in the name.** No `customerList`, `isFlag`, `strName` — the type system already carries that information.
- **Avoid meaningless noise words:** `Manager`, `Helper`, `Processor`, `Handler`, `Utils` — these usually mean you haven't identified what the thing actually *is* or *does*.
- **Abbreviations are not names.** `mgr`, `ctx`, `svc`, `impl` are unclear. Use full words unless the abbreviation is universally understood in the domain (e.g., `AST`, `SQL`).
- **Follow existing project conventions.** Before introducing a new naming pattern, study the neighbors. Consistency across the codebase beats local cleverness.

### State & Side Effects
- **Command-Query Separation (CQS).** A method should either compute and return something OR mutate state — not both. If it does both, split it.
- **No `out` parameters.** Return a tuple, a result type, or a dedicated return object instead. `out` params are a legacy pattern that hurts readability.
- **Null is not a valid business value.** If something is absent, model it explicitly — nullable reference types with clear intent, `Optional<T>`, or a `Result<T>` pattern. Never use `null` to mean "not found" or "not applicable" without making that intent obvious.

### Error Handling
- **Use specific exception types**, not bare `Exception`. Throw `ArgumentNullException`, `InvalidOperationException`, `FormatException`, etc., so callers can handle failures precisely.
- **Never swallow exceptions silently.** Empty `catch { }` blocks hide bugs. At minimum, log the error. Prefer letting exceptions propagate unless you have a concrete recovery strategy.
- **Guard at public boundaries.** Use `ArgumentNullException.ThrowIfNull()` and similar guard clauses at the entry points of public methods. Fail loudly and immediately with a clear message.
- **Throw early, catch late.** Detect errors as close to their source as possible. Handle them at the level that has enough context to do something meaningful.

### Types & Abstractions
- **Prefer composition over inheritance.** Inheritance creates tight coupling. Use interfaces and delegation unless there's a genuine "is-a" relationship.
- **Seal classes by default** unless a class is explicitly designed and documented for extension. Unsealed classes are an implicit promise of extensibility you may not intend.
- **Small interfaces over large ones (ISP).** If a consumer only needs 2 of 8 methods on an interface, the interface is too wide. Split it.
- **Use records for pure data carriers.** If a type has no behavior and just holds values, prefer `record` or `record struct` — you get value equality, `ToString`, and deconstruction for free.
- **Don't over-abstract.** An interface with a single implementation is noise unless you have a concrete reason (testability with mocking, plugin extensibility). Wait for the second use case before extracting an abstraction.

### Dead Weight
- **Delete commented-out code on sight.** That's what git history is for. Commented-out code rots, misleads, and clutters.
- **If a variable is assigned and immediately returned, just return the expression directly** — unless the variable name adds meaningful documentation.
- **Remove `else` branches that only contain a `throw` or `return`.** Restructure so the happy path flows linearly without unnecessary nesting.
- **Remove unused `using` directives, parameters, and local variables.** Dead code is a maintenance tax and a source of confusion.
- **Don't comment method bodies.** If a method's implementation is unclear, refactor it or add a descriptive name instead of commenting.

### Tests
- **One logical assertion per test.** Test one behavior, not the whole feature. If a test fails, you should know exactly what broke from the test name alone.
- **Test names describe the scenario and expected outcome.** Use a consistent pattern like `WhenSomething_OrSomethingElse_ShouldFail` or a clear descriptive sentence.
- **Arrange-Act-Assert structure.** Every test should have a clearly visible setup, a single action, and focused verification. No test logic — no `if`, `for`, or `while` inside tests.
- **No garbage assertions.** Every assertion must validate actual expected behavior. `Assert.IsNotNull(result)` is meaningless if you don't also assert *what* the result contains. No shortcuts.
- **Tests are documentation.** A new contributor should be able to read your tests and understand how the system behaves without reading a single line of production code.

### C#-Specific Conventions
- **Prefer `readonly` fields and properties** where a value doesn't change after construction. Immutability narrows the space of possible bugs.
- **Use target-typed `new`** (`List<int> items = new()`) when the type is obvious from context. Avoid when it hurts clarity.
- **Use collection expressions** (`[1, 2, 3]`) where the compiler supports them and it improves readability.
- **LINQ is for transforms, not side effects.** Never use `.Select()` or `.Where()` to mutate state. Use `foreach` for side effects.
- **`var` is fine when the type is obvious** from the right-hand side (`var list = new List<int>()`). Avoid `var` when the type isn't immediately clear.
- **String interpolation over concatenation.** Prefer `$"Hello {name}"` over `"Hello " + name`. Use `string.Empty` over `""` for clarity of intent.

### General Hygiene
- **Leave the campsite cleaner than you found it** — but scope your improvements to what you touch. Improve the file you're editing, not the entire subsystem. Refactoring unrelated code in the same PR creates noise and merge risk.
- **The copy-paste threshold is two.** If you're about to paste something a second time, stop and extract it. Three copies means three bugs to fix instead of one.
- **Magic numbers and strings are a tax on the next reader.** Name them with constants or well-named variables that explain *why* that value exists.
- **Overview your changes before committing.** Ask yourself: is there anything that looks similar that we could extract? We must not duplicate code. No shortcuts.
