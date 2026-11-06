# From log errors to deployment history

This is the runnable investigation from the [main README](../../../../README.md). All inputs are checked in here. The log records and deployments are synthetic; no accounts or API keys are needed to query them.

## Prerequisites

Use a compatible [Musoq CLI and local server](https://github.com/Puchaczov/Musoq.CLI#install--update) with the **Search** and **SeparatedValues** providers. The binary example in the main README additionally requires **System**. These examples were checked with the runtime and provider versions recorded below; the CLI installer supports selecting a release channel or exact version.

If a required provider is missing:

```shell
musoq data-sources install Musoq.DataSources.Search --channel alpha
musoq data-sources install Musoq.DataSources.SeparatedValues --channel alpha
musoq data-sources install Musoq.DataSources.System --channel alpha
```

Run the following commands from the repository root, with the local server started from that directory:

```shell
musoq serve
```

Source paths resolve in the server's working directory. Starting a client in a different directory does not move an already-running server. For an existing server elsewhere, use the absolute-path guidance below.

## Inputs

- [checkout.log](checkout.log): one INFO record and two ERROR records for checkout.
- [payments.log](payments.log): one WARN record and one ERROR record for payments.
- [deployments.csv](deployments.csv): two deployments per service, including a payments deployment after all three errors.

The logs use the layout `timestamp level service message`. They are UTF-8 with CRLF line endings. The local `.gitignore` keeps the two log fixtures visible despite the repository-wide log exclusion.

## Find the errors

[find-errors.sql](find-errors.sql) returns file paths, line numbers, and matching physical lines:

```shell
musoq run docs/examples/readme/investigation/find-errors.sql --from-file --format json
```

Expect checkout.log lines 2 and 3, then payments.log line 2. The search needle is ` ERROR `, including surrounding spaces. `LineText` includes the CRLF terminator.

## Parse the matching records

[parse-errors.sql](parse-errors.sql) declares the text format and projects Service, ErrorTime, and Message:

```shell
musoq run docs/examples/readme/investigation/parse-errors.sql --from-file --format json
```

Each `until ' '` field consumes its following space. `rest trim` removes the line terminator from Message. The timestamp is explicitly converted to `datetime` before comparison. The fixture uses one shared clock and timestamp format.

The two checkout records precede the payments record in the result, ordered by Service and ErrorTime.

## Relate errors to deployments

[errors-and-deployments.sql](errors-and-deployments.sql) combines the search and text declaration with the CSV source:

```shell
musoq run docs/examples/readme/investigation/errors-and-deployments.sql --from-file --format json
```

Expected output:

```json
[
    {"Service":"checkout","ErrorTime":"2026-10-11T10:05:00","Version":"v1","Message":"retry failed"},
    {"Service":"checkout","ErrorTime":"2026-10-11T10:20:00","Version":"v2","Message":"timeout"},
    {"Service":"payments","ErrorTime":"2026-10-11T10:12:00","Version":"p1","Message":"payment failed"}
]
```

The join requires both the same service and a deployment time at or before the error. It chooses the nearest qualifying time. The first checkout error must not pick the more recent payments deployment; the payments error must not pick the future p2 deployment. Association in time does not establish causation.

All three stages have explicit ordered expectations in [expected-results.json](expected-results.json). For the search result, normalize only the known fixture-root prefix and path separators so Path becomes checkout.log or payments.log; preserve the line numbers and complete text, including CRLF.

## Absolute paths with an existing server

All three queries declare a `root` parameter for log discovery. Resolve it locally and pass the resulting absolute path:

```powershell
$fixtureRoot = (Resolve-Path -LiteralPath 'docs/examples/readme/investigation').Path
$parameters = @{ root = $fixtureRoot } | ConvertTo-Json -Compress
$query = (Resolve-Path -LiteralPath 'docs/examples/readme/investigation/find-errors.sql').Path
musoq run $query --from-file --params-json $parameters --format json
```

The same override works for parse-errors.sql. For errors-and-deployments.sql, also edit the `let deployments` declaration to contain the absolute path to deployments.csv before running it. SQL raw strings (`r'C:\work\Musoq\docs\examples\readme\investigation\deployments.csv'`, for example) preserve backslashes. Double any apostrophe inside an SQL string.

The CSV source discovers its columns during compilation, so its path is a `let` constant. In the checked CLI, a runtime parameter override cannot replace that declaration for metadata discovery. The default checked-in query runs unchanged when the server's working directory is the repository root.

## Verification

On 2026-11-02, the examples were checked on the locally installed development CLI with:

| Component | Version |
|---|---|
| Core engine libraries | 17.0.15-alpha.1+17392a33d11cd3ded743216668b9c42052463a92 |
| Search provider | 1.0.5-alpha.1 |
| SeparatedValues provider | 11.0.5-alpha.1 |
| System provider, for the binary example | 8.0.8-alpha.1 |

All three stages matched their ordered expected columns and values, both with default paths and with absolute log-root overrides. The combined query also passed with absolute input paths and a client working directory outside the repository. The unchanged binary example returned Kind = 1, Size = 3, and PayloadBytes = 3: eight successful executions in total.

This records example execution on that installed runtime, separately from build or test qualification of the engine checkout. The [syntax example collection](../README.md) and [SQL language tour](../language-tour.md) retain the broader examples.
