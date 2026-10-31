# Formatting Musoq scripts

`Musoq.Formatter` is a .NET 10 library for formatting complete raw Musoq scripts.
It depends only on `Musoq.Parser`; it does not load providers, bind functions, or
execute queries. Command-line handling, file access, Markdown extraction, and
Musoq.Cloud integration belong to callers.

## Public API

Reference `Musoq.Formatter` and call its stateless synchronous entry point:

```csharp
using Musoq.Formatter;

var source = "select Name,Stars from #packages.rows() p where p.Stars >= 10";
var result = MusoqFormatter.Format(source, new FormattingOptions { LineEnding = LineEnding.Lf });
```

On success, `result.IsSuccess` is true and `result.FormattedText` contains:

```sql
SELECT Name, Stars
FROM #packages.rows() p
WHERE
    p.Stars >= 10
```

`FormattingResult.Diagnostics` is a read-only snapshot of existing parser
`Diagnostic` values in original-input order. Any syntax error fails the entire
script: `IsSuccess` is false, `FormattedText` is null, and diagnostics explain
the failure. The formatter never returns partial output. Null source throws
`ArgumentNullException`; invalid options throw `ArgumentOutOfRangeException`.
Unexpected implementation exceptions propagate. Internal syntax-coverage or
layout defects use the existing `MQ9001_InternalCompilerError` diagnostic with
phase `Internal`, located in the original input.

Whitespace-only input succeeds with an empty string. Comments-only input
preserves the comments. Every nonempty output ends in one generated newline.
Options and results are immutable; calls may share options concurrently.

## Options

| Property | Default | Supported values |
|----------|---------|------------------|
| `IndentSize` | `4` | Positive number of spaces |
| `LineWidth` | `100` | Positive target width in UTF-16 code units |
| `KeywordCasing` | `KeywordCasing.Upper` | `Upper`, `Lower`, `Preserve` |
| `LineEnding` | `LineEnding.Preserve` | `Preserve`, `Lf`, `CrLf` |

Passing null options selects defaults. Records support `with` expressions for
creating variations. No configuration-file format or public rule framework is
required. Future options can extend this typed contract.

Ordinary comma-separated lists stay inline when the complete containing line
fits, including indentation and closing text. Otherwise they wrap one item per
line. Schema fields, CTE definitions, and named window definitions use expanded
layout. Width is a preference: the formatter preserves indivisible content
even when a line exceeds it. Keywords use invariant casing; identifiers,
function/provider names, type spelling, and literals retain their original text.
Contextual controls such as UNPIVOT USING/KEEP follow keyword casing, while
same-spelled generated column names and aliases remain protected identifiers.

`Preserve` selects the first LF or CRLF outside protected literal/comment
contents, with LF fallback. Explicit options control generated whitespace only.
Embedded newlines and spaces within literals and comments remain unchanged.

## Supported source and preservation

The library handles SELECT-first and FROM-first queries; expressions, CASE,
predicates, structured arguments, arrays and records; joins and APPLY; VALUES,
subqueries, CTEs and recursive CTEs; grouping, set operations, windows, QUALIFY,
ordering, pagination, PIVOT and UNPIVOT; parameters, variables, enums, utility
commands, TABLE, COUPLE, settings; and complete binary/text interpretation
schemas with nested constructs.

Only external whitespace and designated structural keyword casing may change.
Literal spelling, identifier spelling, explicit parentheses, optional syntax,
operators, separators, clause order, and exact comment contents are retained.
Comments stay at their source boundary; line comments force a following break.
Generated output is parsed again before success is returned. Reformatting
successful output with the same options yields the same string.

The [coverage inventory](../src/dotnet/Musoq.Formatter.Tests/Fixtures/SyntaxCoverage.json) records language
families and parser-node dispositions. The formatter does not repair invalid
scripts or implement proposed grammar from the AI interpretation specification.
Range formatting and formatter on/off directives are outside this API.

## General source-preserving parser API

`Musoq.Parser.Parser.ParseSyntax(string source)` reuses the ordinary grammar and
returns `Musoq.Parser.Syntax.SyntaxParseResult`. It provides original
`SourceText`, nullable `Root`, ordered `Tokens`, `Diagnostics`, and `Success`.
Tokens carry `TokenType`, contextual `Role`, `Span`, and exact `Text`, including
whitespace and comments. On success, joining token text or calling
`Root.ToFullString()` reconstructs the exact input.

Syntax nodes contain a `Kind`, source `Span`, and read-only `Children` describing
grammar regions. They complement the normalized execution AST. The parser
contains no formatting options, layout metadata, or formatter modes. Existing
parsing entry points remain compatible; callers need no migration.

`ILexer` is unchanged. Qualified source names use ordinary tokenization, including
bracketed names and `*`; expressions and invocation syntax remain invalid there.
Skipped trivia retains preceding significant-token context. The existing public
`NextOf` API remains available. Source capture is wired through internal parser
and schema constructors and a construction-time lexer observation callback.
`Peek` suppresses observations and restores state after success or failure.
Ordinary `ComposeAll()` retains legacy exception wrapping; `ParseSyntax` converts
typed input errors to diagnostics and lets unexpected implementation failures
propagate independently of capture state.

The formatter keeps clause orchestration in `QueryLayout` and recursive
expression composition in `ExpressionLayout`. Focused CASE and window rules use
validated grammar regions; shared list and comment rules retain source
boundaries. `SyntaxIndex` owns bounded region lookup. The document renderer alone
chooses wrapping and emits generated whitespace, distinguishing it from protected
source text. These are internal implementation details, not extension APIs.

## Validation and packaging

The formatter's MSTest project contains all its fixtures and verification code.
It references only Formatter and Parser. Its cases include independently
reviewed exact output, 293 accepted specification examples, six additional
language-coverage fixtures, 128 generated cases,
independent token scanning, normalized parser-structure and field-label
comparisons, width/option boundaries, malformed input, and concurrent calls.
Structural comparisons omit typed source spans, span presence, source
positions on sources/diagnostic commands, generated schema query IDs, computed
Node.Id values, and computed paging Value properties (their Expression is
compared). Return types, field labels, syntax helpers, and semantic fields remain
part of the comparison. Mutation tests verify that the source and structure
oracles reject protected-token and field-label changes. These checks establish parser preservation; they do not claim
runtime execution equivalence. Whole-solution tests provide repository
compatibility validation without adding execution dependencies to the formatter.

Verification uses an explicit typed AST projector, shared by structural snapshots
and observed fixture coverage. Expression, query, script, and schema projections
read named members directly, traverse ordered collections, CASE pairs,
dictionaries and helper records, and recursively parse diagnostic-command queries.
Snapshots are immutable and distinguish numeric kinds and CLR return-type
identities; the parser's NULL marker has its own representation. Unknown node,
helper and scalar shapes fail explicitly. AST text rendering is never a
structural fallback. Exact CLR type equality rejects unreviewed subclasses.

A test-only Roslyn compiler model is initialized once per process from evaluated,
configuration-specific MSBuild source/reference/target paths, language version,
nullable settings and conditional symbols. Missing or invalid inputs fail without
directory scanning. Compiler audits require reviewed declaration dispositions and
check actual accessor references, member labels, dispatch and child traversal.
Member exclusions are qualified by their declaring symbol or source-range type;
similarly named semantic members remain covered. The canonical coverage manifest
also includes the argument-label record struct. In-memory mutations exercise
missing handlers, child traversal, semantic members, runtime reflection, dynamic
member access and forbidden dependencies without changing repository files.

Formatter-owned implementation and tests perform no runtime member or assembly
discovery or reflective invocation. Roslyn symbol inspection and ordinary CLR
type equality are permitted. Framework internals and declarative assembly
attributes are outside this audit. Evaluated project references, package assets
and compiler metadata enforce the dependency boundary. Roslyn is centrally
versioned and private to Formatter.Tests; it is absent from the production
package closure and formatting calls.

A local Windows Release comparison used the same 299 fixtures plus 128 generated
cases, with parsing separated from traversal (five warmups, 20 samples). Median
elapsed time and current-thread allocations per 427-case traversal were:

| Work | Previous verifier | Typed verifier |
|---|---|---|
| Parsing | 15.1 ms / 15.4 MB | 15.3 ms / 15.4 MB |
| Structural snapshot | 52.9 ms / 130.3 MB | 13.1 ms / 11.2 MB |
| Coverage | 36.2 ms / 13.6 MB | 7.3 ms / 3.2 MB |

Compiler initialization and audits were measured separately at approximately
4.2 seconds and 180 MB of current-thread allocations, once per process. Raw
samples and commands are retained in ignored qualification artifacts. Declared
node/helper coverage matched the previous traversal; incidental runtime array
wrappers and source-span types are not declarations. These measurements describe
this verification corpus, not production formatting throughput or peak memory.

The package has its own `MusoqFormatterVersion`, aligned with Parser for this
release train. Its production package dependency is Parser; Roslyn remains
private to the test project. CI runs the ordinary formatter/parser tests,
release-tooling checks, and package creation. See the
[release guide](releasing.md#formatter-package-qualification).

Focused checks:

```powershell
dotnet test src/dotnet/Musoq.Formatter.Tests -c Release --nologo --verbosity quiet --logger "console;verbosity=minimal"
dotnet test src/dotnet/Musoq.Parser.Tests -c Release --nologo --verbosity quiet --logger "console;verbosity=minimal"
pwsh -NoProfile -File scripts/release/Test-ReleaseScripts.ps1
```

Adding grammar requires reviewing source roles, grammar regions, layout rules,
and the coverage inventory together. Add independent preservation and exact
output regressions in Formatter.Tests, and general parsing regressions in
Parser.Tests. Then follow the repository's full production-change qualification
policy. CLI and Cloud integration, package publication, performance claims, and
remote platform qualification require their own work and evidence.
