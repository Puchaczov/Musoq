# LIKE query conformance crosswalk

This crosswalk distinguishes the SQL `LIKE` predicate from `RLIKE` and from the `SELECT * LIKE` column-selection modifier.

| Behavior | Coverage before QMS-R09 | QMS-R09 executable case |
| --- | --- | --- |
| Exact, prefix, suffix, contains | Partial across operator and integration tests | `shape-exact`, `shape-prefix`, `shape-suffix`, `shape-contains` |
| Match-all, repeated `%`, empty | Fragmented | `shape-match-all`, `shape-repeated-percent`, `shape-empty` |
| `_`, mixed wildcards, interior `%` | Operator-focused | `shape-single-character`, `shape-mixed-wildcards`, `shape-interior-percent` |
| Regex metacharacters, paths, multiline values | Partial or advisory-only | `shape-regex-metacharacters`, `shape-raw-backslashes`, `shape-multiline` |
| Literal and same-row patterns | Covered in `WHERE` | Catalog projection snapshots |
| Parameter, `let`, concatenation | Missing systematic result oracle | `source-parameter`, `source-variable`, `source-concatenation` |
| Method, `CASE`, `COALESCE` pattern | Missing systematic result oracle | `source-method-result`, `source-case-expression`, `source-coalesce-expression` |
| Ordinary Unicode | Broad script coverage | `unicode-ordinary` |
| Culture-sensitive Unicode folding | Helper-only differential tests | Kelvin sign, dotted/dotless I, and sigma catalog cases under four cultures |
| Composed/decomposed and surrogate-containing values | Missing | `unicode-composed-decomposed`, `unicode-surrogate-containing` |
| Null input/pattern and negation | Existing `WHERE` cases | Six projection snapshots covering both polarities |
| JOIN, APPLY, aggregate, window, CTE, set, parallel contexts | Missing or indirect | QMS-R10 context cases, including qualified dynamic RHS and correlated source-call assertions |

The executable catalog owns stable IDs, SQL, deterministic fixtures, hard-coded result snapshots, semantic dimensions, and the expected execution strategy. Expected rows never call the production matcher.

QMS-R10 closes every context and pattern-source dimension. It also records regressions for two implementation defects found while activating the matrix: qualified pattern operands now parse for all four pattern operators, and aggregate finalization lowers `PatternMatch` expressions used by `HAVING`.
