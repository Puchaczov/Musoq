# Query telemetry

Musoq exposes three complementary execution signals. They have different owners
and deliberately different granularity:

| Signal | Owner | Meaning |
| --- | --- | --- |
| `PhaseChanged` | Engine | A once-only entry marker for an applicable query clause or query scope. `Begin`, `From`, `Where`, `GroupBy`, and `Select` are emitted when the corresponding work starts; `End` terminates the scope. These are not duration brackets. |
| `DataSourceProgress` | Data source | Source-owned lifecycle notifications and source totals. A provider can report metadata, opening/closing, known totals, or other source-specific work. |
| `QueryProgress` | Engine | An approximate count of rows consumed from physical source chunks. It is published per source and for the query, subject to `QueryProgressOptions.RowsPerUpdate` and `MinimumInterval`. |

`QueryProgress` is opt-in: subscribe to the `QueryProgress` event on a compiled
query, or pass `QueryProgress` in `TypedQueryRunOptions`. With no subscriber,
generated source setup keeps the original chunk enumerable. With a subscriber,
the engine wraps source chunks and reports their `Count`, so an interim snapshot
can lead row-level processing by one source chunk.

The terminal snapshot has `IsFinal = true`, a query-wide
`SourceContextId == null`, and is emitted before the query's `QueryPhase.End`
marker. Counts are monotonic within one query run. Progress is synchronous with
the consuming thread, so handler exceptions follow the normal callback
exception path.

There is one query-wide progress stream per run. CTE and set-operation child
query IDs are exposed through `PhaseChanged`; their source chunks contribute to
the root `QueryProgress` counters rather than creating separate progress
streams. The terminal flush also runs when enumeration is disposed, cancelled,
or fails, so a partially consumed source still receives its final snapshot.

The default cadence is 16,384 consumed rows or 250 milliseconds, whichever
comes first, followed by one terminal flush. `MinimumInterval` and the clock
provider are configurable for deterministic tests and specialized hosts.

## Performance contract

The disabled path is intentionally cheap: generated code keeps the original
`.Chunks` enumerable, performs only the runtime callback check at the source
boundary, and does not create a progress reporter. The default options object
is shared when no per-run options are supplied, so a no-subscriber run does
not allocate progress configuration state.

The active path counts a whole chunk with atomic counters and takes the
publication lock only when a row or time gate is reached. Handlers run
synchronously at publication points; there is no callback, lock, or clock
call inside a generated per-row loop.

As a representative qualification on .NET 10.0.11, 1,000,000 rows with
4,096-row chunks and a three-iteration ShortRun measured 271.0 ms for the
baseline, 274.3 ms (1.01x) for the progress-capable run without a subscriber,
and 275.3 ms (1.02x) with default progress enabled. Managed allocations were
58.28 MB, 58.28 MB, and 58.29 MB. These are qualification measurements rather
than a universal hardware guarantee; repeat the benchmark on the deployment
hardware when setting a release-specific budget.
