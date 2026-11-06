# Reporting datasource errors and warnings

Datasource authors can report recognized problems through `Musoq.Schema.Diagnostics`.
A report has a stable provider code, provider-defined category, severity, public message,
optional suggested fixes, safe structured details, argument/input location, and retry guidance.
Core attaches the source identity and operation; providers do not invent SQL source positions.

## Public errors

Throw `DataSourceException` when a source cannot produce a valid result. This is supported
in metadata discovery, planning, construction, synchronous/asynchronous collection,
query-scoped rows, background `CollectChunks`, deferred enumeration, and cleanup.

```csharp
var report = new DataSourceDiagnostic(
    "REMOTE.RATE_LIMIT", "rate-limit", "The remote service rate limit was reached.",
    new DataSourceDiagnosticOptions
    {
        FailureKind = DataSourceFailureKind.RateLimited,
        SuggestedFixes = ["Wait before retrying the query."],
        CanRetry = true,
        RetryAfter = TimeSpan.FromSeconds(30)
    });
throw new DataSourceException(report, privateSdkException);
```

Use the shared failure kinds for input, configuration, access, missing resources,
rate limits, availability, timeouts, and provider defects. Provider codes and categories
remain extensible: a new provider issue does not require a new Core diagnostic code.
Core reports errors as `MQ7020` with `providerCode`, `providerCategory`, `failureKind`,
source identity, operation, and correction/retry guidance in the standard diagnostic envelope.
Existing specific Core errors, cancellation, and mixed aggregate failures retain their handling.

Messages, categories, suggested fixes and details are explicitly public. Author these strings;
do not copy arbitrary SDK exception messages, response bodies, paths, credentials, or record contents.
Core bounds and escapes text and redacts sensitive keyed details and known path values.
This does not identify secrets that a provider embeds in arbitrary prose without identifying them.
The optional inner exception remains private diagnostic/debugging information, not public guidance.
Input line/column values are one based; byte/character offsets and argument indexes are zero based.
Input locations never become fabricated SQL offsets.

## Warnings during collection

Warnings do not throw. The datasource decides whether continuing is valid, honoring its existing
strict/tolerant options. A warning does not change which rows Core accepts or returns.

```csharp
context.ReportWarning(new DataSourceDiagnostic(
    "CSV.RECORD_SKIPPED", "input-format", "A malformed record was skipped.",
    new DataSourceDiagnosticOptions
    {
        Severity = DataSourceDiagnosticSeverity.Warning,
        SuggestedFixes = ["Correct the record or enable strict parsing."]
    }));
```

Contexts constructed directly without a warning reporter are no-ops. Compiled source contexts
receive a collector owned by the current execution. It is independent of profiling metrics,
thread-safe, and retained through deferred enumeration. Reporting warnings is an exceptional path;
ordinary row reads do not format diagnostics.

For metadata and planning, return reports in `SourceDescriptor.ReportedDiagnostics` and
`SourcePlanResult.ReportedDiagnostics`. They are exposed in compilation and inspection diagnostics.
Return an error report or throw `DataSourceException` to reject the operation.

## Consuming runtime warnings

Supply a fresh collector for each execution, including concurrent runs of the same compiled query:

```csharp
var warnings = new DataSourceWarningCollector();
var table = compiled.Run(new TypedQueryRunOptions { DataSourceWarnings = warnings });
_ = table.Count; // Finish deferred row collection before taking the final snapshot.
var diagnostics = warnings.Snapshot();
```

The same options work with `RunAsync` and typed deferred results. Snapshots are detached standard
Core diagnostics and can be converted into `MusoqErrorEnvelope` values by the host.
A snapshot taken before enumeration may be incomplete. Warnings remain available after failures.
Core does not retry queries automatically; `CanRetry` and `RetryAfter` are advisory.

Repeated identical reports from the same source are retained once with `occurrenceCount`.
Different provider codes, source occurrences, details, fixes or input locations remain distinct.
At most 100 distinct warnings are retained. Additional reports increment `SuppressedCount`;
a snapshot includes one `CORE.WARNINGS_SUPPRESSED` summary when needed. Reports repeating retained
warnings continue to update their occurrence count. Suppressed reports are not retained in memory.

An error after some rows have been produced is still a failed query. Hosts streaming rows must
identify them as partial results and send a failed terminal result instead of reporting success.
This Core change supplies the diagnostics; host HTTP classification and UI presentation are separate.
