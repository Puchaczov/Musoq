# A SQLite page, read with Musoq

[Watch the 2:01 movie](../../../../images/experiments/sqlite-page.mp4) · [Captions](../../../../images/experiments/sqlite-page-captions.vtt) · [Walkthrough SQL](read-sqlite-page.sql) · [Parameterized table reader](read-sqlite-table.sql)

This is the experiment from [Experiments with Musoq](../../../../README.md#experiments-with-musoq). An agent reads a SQLite page through Musoq's filesystem source and binary interpretation functions. It builds up the reader by inspecting bytes and checking their layout. No SQLite API or SQLite datasource decodes the input for Musoq.

The [page walkthrough](read-sqlite-page.sql) contains the movie's small reader, simple explanations beside each relevant part, and four commented probes for repeating the investigation. The [table reader](read-sqlite-table.sql) extends the experiment with database-path and table-name arguments. Each is a complete, standalone SQL file; the table reader does not call the walkthrough or require a helper program.

## Read a table from another database

Use a running Musoq instance with the Os provider and binary interpretation support. From the repository root, the defaults read `firmware` from the bundled database:

```shell
musoq run docs/examples/experiments/sqlite-page/read-sqlite-table.sql --from-file --format json
```

The script declares two parameters:

| Parameter | Meaning | Default |
|---|---|---|
| `databasePath` | Path to the original SQLite main database file | `./docs/examples/experiments/sqlite-page/inputs/telemetry.db` |
| `tableName` | Exact table name to look up in its catalog | `firmware` |

Pass both through `--params-json`. This PowerShell example selects the 480-row `events` table from the bundled file:

```powershell
$sqliteDatabase = (Resolve-Path -LiteralPath './docs/examples/experiments/sqlite-page/inputs/telemetry.db').Path
$sqliteParameters = @{ databasePath = $sqliteDatabase; tableName = 'events' } | ConvertTo-Json -Compress
musoq run docs/examples/experiments/sqlite-page/read-sqlite-table.sql --from-file --params-json $sqliteParameters --format json
```

Replace the path and table name with your own. An absolute database path works independently of the current directory. These are parameter values, so file paths and table names containing quotes do not need manual SQL escaping.

The reader discovers page size and text encoding from the database header, reads `sqlite_schema` from page 1 and any catalog child pages, finds the requested table's root, and follows its branch pages. You do not supply a page number, offset, column count, or field lengths.

The default result has three rows. Here `StoredValuesJson` is shown as its readable string content; the complete CLI JSON is in [expected-table-results.json](expected-table-results.json).

| RowId | StoredValuesJson | PageNumber | CellOffset |
|---:|---|---:|---:|
| 1 | `["1.1","retired"]` | 5 | 1009 |
| 2 | `["1.2","stable"]` | 5 | 995 |
| 3 | `["1.3","canary"]` | 5 | 981 |

Every result also includes `TableName`. Values appear in stored field order. A fixed output shape lets one SQL file inspect tables with different numbers and types of fields. JSON text preserves quoting, Unicode, and embedded control characters; blobs appear as objects such as `{"$blob":"00FF"}`. The comments also show how to select one field per result row, including its zero-based position and storage class.

This reads **stored records**. It does not parse `CREATE TABLE` to attach column names, apply defaults added by `ALTER TABLE`, compute generated columns, or reproduce affinity conversions. An `INTEGER PRIMARY KEY` alias is normally stored as a NULL field; its value is available separately as `RowId`. Keep that distinction when comparing with SQLite's `SELECT *`.

### Supported layouts and checks

The table reader handles ordinary rowid tables, table and catalog branch traversal, legal page sizes from 512 to 65,536 bytes, UTF-8/UTF-16LE/UTF-16BE, one- through nine-byte varints, signed stored integers, finite reals, NULL, text, and blobs, including empty text and blobs. Missing tables and index-style roots used by `WITHOUT ROWID` tables fail strict checks; a supported empty table returns no rows.

Use a small, consistent, checkpointed copy of the main database file. This experiment loads the file's pages into memory and reads records whose payloads stay on their leaf page. Overflow payloads cause an interpretation failure. WAL/journal replay, virtual tables, deleted-record recovery, and full damaged-file validation are outside its scope. Recursive CTE limits still apply to deep trees and very wide records.

The parameterized reader was checked against all 531 rows in the bundled four-table database and separate fixtures covering mixed field types, empty tables, signed 64-bit row IDs, multi-byte type codes, a multi-byte record-header length, UTF-16, a branched catalog, and quoted paths/table names. The movie remains the original guided, one-page investigation; this larger reader is a subsequent extension.

## Replay the movie's page query

Use a running Musoq instance with the Os provider and binary interpretation support. From the repository root:

```shell
musoq run docs/examples/experiments/sqlite-page/read-sqlite-page.sql --from-file --format json
```

The default input folder is relative to that repository root. To supply its absolute path explicitly in PowerShell:

```powershell
$sqliteInputs = (Resolve-Path -LiteralPath 'docs/examples/experiments/sqlite-page/inputs').Path
$sqliteParameters = @{ root = $sqliteInputs } | ConvertTo-Json -Compress
musoq run docs/examples/experiments/sqlite-page/read-sqlite-page.sql --from-file --params-json $sqliteParameters --format json
```

Expected output, also retained in [expected-results.json](expected-results.json):

```json
[
  { "RowId": 1, "Version": "1.1", "Status": "retired" },
  { "RowId": 2, "Version": "1.2", "Status": "stable" },
  { "RowId": 3, "Version": "1.3", "Status": "canary" }
]
```

## How the investigation works

1. **Find the page bytes.** The supplied page is number 5, with 1,024 bytes per page. Its file offset is `(5 - 1) × 1024 = 4096`. Offsets inside that slice start again at zero.
2. **Read its row locations.** A leaf-page header says there are three cells. Its pointer array stores page-relative offsets 1,009, 995 and 981. Adding 4,096 converts them to file offsets.
3. **Inspect each cell's prefix.** The prefix contains a payload length, a row ID, a record-header length and two field-type codes. All relevant integer values in these records fit in one byte.
4. **Establish the field boundaries.** Row 1 uses text type codes 19 and 27, which give byte lengths `(19 - 13) / 2 = 3` and `(27 - 13) / 2 = 7`. Its 13-byte payload is a three-byte record header plus those two text fields. The checks agree for every row.
5. **Decode the text.** Musoq's `string[size] utf8` reads exactly those field bytes. The result is `1.1 / retired`, followed by the other two records. `CROSS APPLY` handles the pointer-by-pointer iteration.

The SQL comments explain these steps in more detail, including a labeled byte dump, the difference between page and file positions, each Musoq construct, and optional queries to inspect the intermediate values. Layout rules follow the [SQLite file-format specification](https://www.sqlite.org/fileformat.html).

## What was supplied, and what was established

The agent was given the format notes, the two-column table shape, page number, page size, page location and UTF-8 assumption. It was not given the decoded values or a previous reader. This was guided inspection of SQLite storage, not blind identification of an unknown format.

The agent produced five incremental queries in a recorded run. The movie edits the successful probes and public explanations into a presentation. It keeps complete scripts visible and marks additions or changes. The run also contained source-contract and binding corrections; the movie is not presented as uninterrupted live execution. The page walkthrough adapts the final query to this repository's input path, selects only the three record fields, and adds the teaching comments and optional probes.

[telemetry.db](inputs/telemetry.db) is a synthetic, ordinary SQLite database with four tables, fifteen 1,024-byte pages and 531 user rows. This example reads only the three records in `firmware` on page 5. Its SHA-256 is `1d974566af1b66910c512b12f95473653ee4930a9c0f4bb640658ba8a30783c3`.

SQLite was used separately to establish the reference result, verify the supplied page location and confirm UTF-8. The Musoq script reads only original file bytes; it does not consume an export or the expected-results JSON.

The movie's page reader covers the supplied ordinary leaf page, valid pointers, two short nonempty text fields and the observed one-byte integer encodings. The parameterized table reader adds the discovery and decoding described above. Both are experiments with Musoq's still-experimental binary/text specification.
