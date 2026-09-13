# Musoq CLI user manual

This manual describes the Musoq CLI, the Musoq CLI service, supported tool REST endpoints,
and MCP integration. It is written for terminal users and automated clients. Exact command
syntax is generated from the checked-in command contract; narrative chapters explain safe use,
output boundaries, and verification.

Edition: `0.40.0-alpha.12`

Language: `en`

Chapters: `89`

Start with `musoq describe`. Search by task with `musoq describe search "<task>"`, then
read one exact manual, specification, recipe, or command resource. Use
`musoq describe --format json` when a stable machine-readable response is required.
The existing `musoq manual` command remains available for reading the complete manual.

## Part 1: Start here

Installation, terminology, first execution, and ways to discover the interface without guessing.

- [1. About this manual](001-about-this-manual.html) — Start with `musoq describe`, then use this manual as the versioned operational contract for the CLI, service, and automation interfaces.
- [2. Product model and terminology](002-product-model-and-terminology.html) — Distinguish the terminal client, the Musoq CLI service, data sources, scripts, tools, buckets, registries, paths, and MCP contexts.
- [3. Supported interfaces and stability](003-supported-interfaces-and-stability.html) — Separate supported CLI discovery and execution, tool REST endpoints, and MCP resources from internal HTTP routes and implementation details.
- [4. Install Musoq CLI](004-install-musoq-cli.html) — Install the platform release through the maintained installer and retain SHA-256 verification as part of the trust boundary.
- [5. Update, select channels, and remove](005-update-channels-and-remove.html) — Change stable or prerelease tracks deliberately, install an exact version when required, and distinguish normal removal from purge.
- [6. Run the first query](006-first-query.html) — Move from the host-free discovery bootstrap to a normal query execution and verify result data independently from diagnostics.
- [7. How commands, the service, and data sources work together](007-command-service-and-datasource-flow.html) — Understand when a command is host-free, when it builds local services, and when it starts the Musoq CLI service for execution.
- [8. Discover Musoq with describe](008-find-help-and-machine-guidance.html) — Use one host-free `describe` entry point to search tasks and resolve exact commands, manuals, specifications, recipes, resources, and error help.

## Part 2: Run and inspect queries

Input resolution, parameters, output contracts, inference, compiler inspection, and query history.

- [9. Choose a query input form](009-query-input-forms.html) — Select inline SQL, a saved script, or a query file explicitly when automatic input resolution could be ambiguous.
- [10. Run queries](010-run-queries.html) — Execute one Musoq SQL statement or reusable input and choose execution options according to output, recursion, and failure needs.
- [11. Read SQL from files](011-read-sql-from-files.html) — Execute reviewed SQL from a file while keeping path resolution, encoding, and runtime parameters separate from query text.
- [12. Run saved scripts](012-run-saved-scripts.html) — Resolve a managed script by name and execute it with the same output, parameter, and diagnostic contracts as inline SQL.
- [13. Read standard input](013-read-standard-input.html) — Pipe supported tabular or structured input to a query while preserving stream ownership and end-of-input behavior.
- [14. Use parameters and settings profiles](014-parameters-and-settings-profiles.html) — Pass runtime values and source-specific settings as JSON while keeping query text stable and secrets outside command history.
- [15. Choose output formats](015-output-formats-and-raw-output.html) — Select table, JSON, CSV, YAML, raw, interpreted, or reconstructed output according to whether a human or program consumes the result.
- [16. Handle paging and redirected output](016-paging-and-redirected-output.html) — Keep long human output readable interactively while ensuring redirected output is complete, plain, and suitable for files or pipes.
- [17. Separate stdout, stderr, and progress](017-stdout-stderr-and-progress.html) — Preserve stdout for result documents and stderr for diagnostics, warnings, debug text, progress, and execution details, including quiet Watch refreshes.
- [18. Use exit codes in automation](018-exit-codes-and-automation.html) — Interpret stable process exits for success, query failure, usage error, service communication, missing resources, authorization, partial results, and cancellation.
- [19. Inspect compiler stages](019-inspect-compiler-stages.html) — Inspect logical, physical, execution, generated-code, or combined compiler output without executing datasource reads.
- [20. Infer log schemas](020-infer-log-schemas.html) — Generate an editable text-schema query from a representative log file while keeping model selection, traces, and review explicit.
- [21. Discover installed capabilities and source shape](021-discover-schemas-with-desc.html) — Use runtime `describe` children or SQL `DESC` to observe installed schemas, functions, settings, methods, and concrete source columns when needed.
- [22. Review query logs and input separators](022-query-logs-and-separators.html) — Inspect recent query history or extracted paths and use an explicit separator marker where a streaming workflow requires boundaries.

## Part 3: Watch

Foreground triggers, repeated actions, rolling state, retention, replay, and recovery boundaries.

- [23. Understand the Watch model and lifecycle](023-watch-model-and-lifecycle.html) — Run a foreground trigger loop whose filesystem, interval, or query events invoke the normal `run` grammar while presenting an immediate current table and keeping lifecycle output quiet.
- [24. Watch filesystem changes](024-filesystem-watches.html) — Trigger queries for filesystem activity while accounting for recursive scope, debounce, coalescing, bounded ingress, overflow, and resynchronization signals.
- [25. Run interval watches](025-interval-watches.html) — Execute a query on a duration-based trigger while controlling overlap expectations, costs, and cancellation.
- [26. Run query-triggered watches](026-query-triggered-watches.html) — Poll a probe query and trigger an action on a rising, falling, or changed single-value condition.
- [27. Configure Watch actions and failures](027-watch-actions-and-failures.html) — Apply the standalone `run` grammar to every Watch turn, bound transactional output, and choose whether failures continue or terminate the loop.
- [28. Use rolling state, history, and diff](028-rolling-state-history-and-diff.html) — Name a durable rolling state so Watch actions can compare current, previous, historical, and difference relations with commit-first delivery recovery.
- [29. Administer and replay Watch state](029-administer-and-replay-watch-state.html) — List active foreground sessions, inspect durable state, export, reset, delete, enumerate generations, replay retained state, and attach to committed output without a live datasource read.
- [30. Design Watch automation and recovery](030-watch-automation-limits-and-recovery.html) — Pipe successful Watch output to external automation with durable generation feeds, explicit cursors, bounded output, and at-least-once recovery semantics.

## Part 4: Data sources, registries, and paths

Discover, install, update, configure, and remove query providers while keeping versions and locations explicit.

- [31. Understand the datasource model](031-datasource-model.html) — Treat each datasource as a versioned plugin that contributes schemas, methods, functions, runtime behavior, and sometimes dynamic CLI commands.
- [32. Discover and inspect data sources](032-discover-and-inspect-datasources.html) — Search registries, list installed packages, show one package, and use runtime descriptions to inspect loaded schemas before selecting a datasource.
- [33. Install, update, and import data sources](033-install-update-and-import-datasources.html) — Add a datasource from a registry, update it within policy, or import a local package while retaining source and version evidence.
- [34. Create a datasource project and set its source](034-create-set-source-and-open-folder.html) — Create an editable datasource project, associate a source location, and open its folder only in an interactive environment.
- [35. Uninstall data sources](035-uninstall-datasources.html) — Remove one or all installed datasource packages with explicit non-interactive authorization and optional file retention.
- [36. Understand and inspect registries](036-registry-model-and-inspection.html) — Use registries as named package indexes whose URL, default status, and returned metadata determine datasource discovery.
- [37. Add, update, select, and remove registries](037-manage-registries.html) — Change registry configuration deliberately while checking endpoint identity, default selection, and dependencies on the registry name.
- [38. Check datasource compatibility and trust](038-datasource-compatibility-and-trust.html) — Evaluate package origin, runtime dependencies, generated-code compatibility, native requirements, and external access before use.
- [39. Manage named query paths](039-named-query-paths.html) — Register stable aliases for existing directories so queries can resolve selected roots without embedding machine-specific absolute paths.
- [40. Use datasource command extensions](040-datasource-command-extensions.html) — Discover optional command namespaces contributed by installed datasource packages without treating them as part of the fixed base CLI.
- [41. Troubleshoot datasource loading](041-troubleshoot-datasource-loading.html) — Separate package discovery, file presence, dependency closure, shadow copy, schema loading, compilation, and runtime execution when diagnosis is required.

## Part 5: Scripts, tools, buckets, images, and caches

Local reusable assets and the commands that create, inspect, execute, and remove them.

- [42. Manage scripts](042-manage-scripts.html) — Create, clone, list, inspect, update, rename, locate, and delete managed SQL scripts under the current user profile.
- [43. Manage tools](043-manage-tools.html) — Create and maintain named parameterized query tools whose definitions can be inspected, cloned, renamed, and removed.
- [44. Preview and execute tools](044-preview-and-execute-tools.html) — Resolve a tool's dynamic parameters, inspect its prepared query, and execute it with a declared output format.
- [45. Manage buckets](045-manage-buckets.html) — Create, list, select, and delete named query contexts while making destructive deletion explicit.
- [46. Encode images](046-encode-images.html) — Convert a selected local image into a data URI on stdout for use by a compatible query or external workflow.
- [47. Purge the compiled-query cache](047-purge-compiled-query-cache.html) — Remove cached compiled queries through the supported command when stale generated artifacts must be excluded from diagnosis.
- [48. Manage the local artifact lifecycle](048-local-artifact-lifecycle.html) — Track ownership and dependencies among scripts, tools, buckets, caches, Watch state, datasource packages, and configuration before cleanup.

## Part 6: Configuration and the Musoq CLI service

Configuration precedence, service lifecycle, network exposure, Python support, diagnostics, and recovery.

- [49. Understand configuration and precedence](049-configuration-model-and-precedence.html) — Distinguish packaged defaults, user settings, environment values, host arguments, and command options when resolving effective behavior.
- [50. Inspect configuration](050-inspect-configuration.html) — Read supported settings, service status, version, port, startup metrics, licenses, data sources, and environment metadata through `musoq get`.
- [51. Set and clear configuration](051-set-and-clear-configuration.html) — Modify supported settings through typed `set` and `clear` children, then verify the effective result and restart requirements.
- [52. Manage datasource environment variables](052-manage-environment-variables.html) — Set, list, locate, and clear persisted environment values used by data sources without exposing their plaintext in routine output.
- [53. Locate configuration and profile files](053-configuration-files-and-profile-locations.html) — Determine the active per-user home, settings, environment store, logs, plugins, scripts, tools, cache, and rolling-state locations safely.
- [54. Understand service lifecycle and auto-start](054-service-lifecycle-and-autostart.html) — Let execution commands start the Musoq CLI service when needed, or manage it explicitly for foreground supervision and diagnostics.
- [55. Start the service in background or foreground](055-start-background-or-foreground-service.html) — Choose detached background startup for ordinary local use or foreground waiting when a terminal or supervisor owns the process.
- [56. Control network binding and exposure](056-network-binding-and-exposure.html) — Keep the service on loopback by default and require an explicit risk acknowledgement before binding to a non-local address.
- [57. Read status, version, port, and startup metrics](057-service-status-version-port-and-metrics.html) — Confirm which Musoq CLI service process a client will use and retain startup measurements for diagnosis rather than assumption.
- [58. Configure Python support](058-python-support.html) — Select automatic, enabled, or disabled Python support at service startup according to installed plugins and runtime availability.
- [59. Run diagnostics, review licenses, and recover](059-doctor-licenses-and-recovery.html) — Use `doctor`, license output, status reads, and a bounded query to build a reproducible service health report.

## Part 7: MCP, tool REST, and AI clients

The supported machine integration boundary, discovery methods, context isolation, and safe execution.

- [60. Choose a machine integration boundary](060-machine-integration-boundaries.html) — Select CLI process invocation, MCP, or the supported tool REST surface according to discovery, isolation, and lifecycle requirements.
- [61. Enable and configure MCP](061-enable-and-configure-mcp.html) — Enable the MCP server setting, start the service, and connect through the configured Streamable HTTP endpoint.
- [62. Manage MCP contexts and tool assignments](062-manage-mcp-contexts.html) — Create named contexts and assign only the tools that a particular client should discover through its context endpoint.
- [63. Connect an MCP client](063-connect-an-mcp-client.html) — Configure a compatible client with the local MCP URL, follow initialization guidance, and discover capabilities instead of embedding schemas in prompts.
- [64. Read capability resources over MCP](064-read-manual-and-spec-resources-over-mcp.html) — Use MCP bootstrap, catalog, manual, and specification resources to provide version-matched operational and normative guidance.
- [65. Use the supported tool REST endpoints](065-use-tool-rest-endpoints.html) — Check health, discover tool parameter contracts, and execute a named tool with optional format and raw-response controls.
- [66. Apply safe AI and automation execution](066-safe-ai-and-automation-execution.html) — Constrain discovery, arguments, side effects, secrets, output size, retries, and evidence when a non-human client invokes Musoq.

## Part 8: Reference, security, and troubleshooting

Generated syntax, formats, exits, locations, compatibility, security assumptions, and issue evidence.

- [67. Use the generated reference](067-use-the-reference.html) — Combine exact `describe` resources with command, REST, MCP, installer, and manifest artifacts from one release bundle.
- [68. Complete command reference](068-complete-command-reference.html) — Read the generated list of every public built-in command path, argument, option, default, allowed value, example, and dynamic boundary.
- [69. Global syntax, help, completion, and version](069-global-syntax-help-and-version.html) — Use root help to find `describe`, then combine host-free exact discovery, the version switch, focused help, and shell completion.
- [70. Output format reference](070-output-format-reference.html) — Apply common rules for table, JSON, YAML, CSV, TSV, raw, interpreted, and reconstructed formats across commands that support them.
- [71. Exit code reference](071-exit-code-reference.html) — Map process codes 0, 1, 2, 3, 4, 5, 6, and 130 to success, domain, usage, communication, missing, authorization, partial, and cancellation states.
- [72. Configuration key reference](072-configuration-key-reference.html) — Discover typed `get`, `set`, and `clear` child commands for cloud connection, identity, logging, MCP, datasource updates, and environment settings.
- [73. File and directory reference](073-file-and-directory-reference.html) — Locate per-user settings, variables, logs, data sources, scripts, tools, caches, databases, and rolling state without assuming one platform layout.
- [74. Tool REST API reference](074-tool-rest-api-reference.html) — Use the filtered OpenAPI document for the three supported GET routes, their parameters, response modes, and error classes.
- [75. MCP protocol reference](075-mcp-protocol-reference.html) — Use the versioned MCP contract for endpoint patterns, initialization, ping, tool list and call, and resource list and read methods.
- [76. Dynamic command and value boundaries](076-dynamic-command-boundaries.html) — Recognize command modules, Watch providers, tool parameters, installed package commands, and completion values that are resolved at runtime.
- [77. Versions, channels, and compatibility](077-versions-channels-and-compatibility.html) — Track the CLI release, manual edition, datasource packages, core runtime packages, installer channel, and extension modules as one tested environment.
- [78. Security model](078-security-model.html) — Protect local network binding, plugin supply chain, datasource credentials, query scope, tool effects, files, logs, and AI client authority.
- [79. Diagnostics and common failures](079-diagnostics-and-common-failures.html) — Classify failures as usage, configuration, service communication, datasource loading, query compilation, runtime execution, output, or cleanup problems.
- [80. Performance and resource limits](080-performance-and-resource-limits.html) — Measure startup, query, Watch, state, recursion, ingress, output, and datasource work with executable identity and explicit bounded limits.
- [81. Report issues and documentation drift](081-report-issues-and-documentation-drift.html) — Provide a minimal reproducible record and identify whether implementation, generated contract, narrative, website, embedded content, or release packaging is stale.

## Part 9: Tested cookbook

Complete task-oriented workflows that state prerequisites, output shape, and verification.

- [82. Cookbook: first query with JSON output](082-cookbook-first-query-json.html) — Run a dependency-free query and produce one parseable JSON document suitable for a smoke test or automation probe.
- [83. Cookbook: inspect and query files](083-cookbook-query-files.html) — Discover the installed filesystem datasource surface and run a bounded file query against a selected directory.
- [84. Cookbook: query piped JSON](084-cookbook-piped-json.html) — Feed a local JSON fixture through standard input and select fields without mixing the data stream with query text.
- [85. Cookbook: create and run a parameterized script](085-cookbook-parameterized-script.html) — Create a managed SQL script, review it, and execute it with serializer-produced runtime parameters and JSON output.
- [86. Cookbook: create, preview, and execute a tool](086-cookbook-create-preview-execute-tool.html) — Define a bounded parameterized tool, inspect its dynamic grammar, preview the prepared query, and execute it as JSON.
- [87. Cookbook: watch a directory with rolling state](087-cookbook-watch-directory-state.html) — Watch a temporary directory, publish JSON events into named rolling state, inspect generations, replay one retained generation, and attach with a cursor.
- [88. Cookbook: connect an MCP client](088-cookbook-connect-mcp-client.html) — Enable MCP, create a least-privilege context, connect locally, and follow the shared `musoq_describe` to query workflow.
- [89. Cookbook: call a tool through REST](089-cookbook-call-tool-rest.html) — Verify local health, discover one tool contract, execute it through the supported GET endpoint, and validate its response mode.

---

# 1. About this manual

_Part 1: Start here_

Start with `musoq describe`, then use this manual as the versioned operational contract for the CLI, service, and automation interfaces.

## When to use this chapter

Read this chapter when an installed Musoq release is unfamiliar or a task requires version-matched operational guidance.

## Working method

1. Run `musoq describe` for the bounded, host-free starting document, then search by the task rather than guessing a command.
2. Choose the manual for operational guidance, the specification for normative syntax, and runtime descriptions only for capabilities installed on the target machine.
3. Execute known query text with `musoq run`; use `musoq inspect` only when compiler-stage evidence is actually required.

## Automation contract

Use `musoq describe --format json` and follow exact `argv` arrays or canonical `musoq://` URIs instead of extracting commands from display prose.

## Verification

Confirm that the selected resource reports the installed CLI version and that its digest matches the embedded capability catalog.

## Commands used

- `musoq describe`
- `musoq describe search`
- `musoq run`
- `musoq inspect`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 2. Product model and terminology

_Part 1: Start here_

Distinguish the terminal client, the Musoq CLI service, data sources, scripts, tools, buckets, registries, paths, and MCP contexts.

## When to use this chapter

Use these terms when diagnosing a failure or deciding which configuration and lifecycle applies to an operation.

## Working method

1. Call the installed executable and its command surface the Musoq CLI.
2. Call the local process that executes queries and exposes supported endpoints the Musoq CLI service.
3. Name stored assets by their public type: datasource, script, tool, bucket, registry, named path, or MCP context.

## Automation contract

Preserve these public terms in generated instructions, logs shown to users, resource names, and API descriptions while ignoring internal implementation type names.

## Verification

Run the published-artifact terminology scan and confirm that only the Musoq CLI service term appears in user-visible material.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 3. Supported interfaces and stability

_Part 1: Start here_

Separate supported CLI discovery and execution, tool REST endpoints, and MCP resources from internal HTTP routes and implementation details.

## When to use this chapter

Consult this boundary before building an integration that must survive a Musoq CLI update.

## Working method

1. Use the CLI `describe` to `run` workflow for broad discovery, administration, query execution, and stable process exit behavior.
2. Use only `GET /tools/health`, `GET /tools`, and `GET /tools/{name}` as supported REST surface.
3. Use MCP `musoq_describe` for the same capability catalog and context-scoped tools, then call `musoq_execute_query` when execution is authorized.

## Automation contract

Reject plans that depend on undocumented service routes; absence from the capability, command, tool OpenAPI, and MCP contracts means the interface is internal.

## Verification

Compare the integration against `commands.json`, `tools-openapi.json`, and `mcp.json` from the same manual bundle.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 4. Install Musoq CLI

_Part 1: Start here_

Install the platform release through the maintained installer and retain SHA-256 verification as part of the trust boundary.

## When to use this chapter

Use this procedure for a first installation on Windows, Linux, or supported Intel macOS systems.

## Working method

1. Read the installer script from the Musoq.CLI repository before running it in an elevated shell.
2. Run the default installer to select the newest stable release, or supply an exact channel or version.
3. Open a new terminal and verify both `musoq --version` and `musoq doctor`.

## Automation contract

Do not disable checksum verification or silently choose a prerelease channel; record the requested platform, channel or version, and installed version.

## Verification

The installer must report a verified platform asset and `musoq --version` must match the selected release.

## Example

```powershell
irm https://raw.githubusercontent.com/Puchaczov/Musoq.CLI/refs/heads/main/scripts/powershell/install.ps1 | iex
musoq --version
musoq doctor
```

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 5. Update, select channels, and remove

_Part 1: Start here_

Change stable or prerelease tracks deliberately, install an exact version when required, and distinguish normal removal from purge.

## When to use this chapter

Use this chapter for upgrades, downgrades between exact tracks, reproducible version installation, or decommissioning.

## Working method

1. Choose one selector: stable, alpha, beta, rc, or an exact semantic version.
2. Rerun the installer with that selector and verify the resulting version rather than comparing only numeric order.
3. Use normal removal to preserve per-user data; use purge only after confirming that configuration, plugins, scripts, tools, caches, and state may be deleted.

## Automation contract

Treat channel and exact-version selectors as mutually exclusive and require explicit authorization before invoking purge.

## Verification

After update, compare `musoq --version` with the requested selector; after normal removal, confirm that only the system installation was removed.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 6. Run the first query

_Part 1: Start here_

Move from the host-free discovery bootstrap to a normal query execution and verify result data independently from diagnostics.

## When to use this chapter

Use this as the first end-to-end workflow after installation or when encountering an unfamiliar release.

## Working method

1. Run `musoq describe` and confirm that its bounded bootstrap points to discovery resources and `musoq run` without starting the service.
2. Execute `SELECT 1 AS Value FROM system.dual()` through `musoq run`; no separate planning or checking command is required.
3. Repeat with `--format json`, check the exit code, parse stdout, and inspect stderr only for diagnostics or progress.

## Automation contract

Read discovery as Markdown or request its JSON envelope, then request JSON execution output explicitly and treat every nonzero exit as failure.

## Verification

Discovery succeeds while the service is stopped, and the subsequent JSON query result parses to one row whose `Value` is 1.

## Commands used

- `musoq describe`
- `musoq run`

## Example

```powershell
musoq run "SELECT 1 AS Value FROM system.dual()" --format json
```

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 7. How commands, the service, and data sources work together

_Part 1: Start here_

Understand when a command is host-free, when it builds local services, and when it starts the Musoq CLI service for execution.

## When to use this chapter

Use this model to explain startup time, filesystem access, network calls, and why some commands work while the service is stopped.

## Working method

1. Use `musoq describe` and every static child for embedded information without constructing or contacting the service.
2. Expect runtime `describe schema|source|functions|settings`, query, and tool operations to start the local service when required.
3. Treat concrete source discovery as provider-controlled data access and resolve dynamic extensions only after installed packages are known.

## Automation contract

Keep help, completion, static describe, specifications, and the embedded manual host-free; invoke runtime discovery only when installed observations are needed.

## Verification

Use startup tracing or service status to confirm that a host-free command did not launch or contact the service.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 8. Discover Musoq with describe

_Part 1: Start here_

Use one host-free `describe` entry point to search tasks and resolve exact commands, manuals, specifications, recipes, resources, and error help.

## When to use this chapter

Use `describe` whenever the command syntax, language semantics, task recipe, or installed capability is not already known.

## Working method

1. Run bare `musoq describe` for the bounded starting document, or `musoq describe --format json` for the stable machine envelope.
2. Search by intent with `musoq describe search "<task>"`, optionally restricting `--kind` and `--limit`; manual, specification, and recipe hits may point to the smallest matching heading section.
3. Resolve the selected command, manual, specification, recipe, error, or canonical `musoq://` resource exactly instead of dumping the whole catalog.
4. Use runtime `describe` children only for installed observations; use shell completion as an additional interactive convenience.

## Automation contract

Parse the format-versioned JSON envelope, preserve canonical resource URIs, and execute returned `argv` arrays directly without shell reconstruction. Even routing, parsing, and validation failures remain one JSON document when the request explicitly includes `--format json`; keep that document on stdout and use its exact help action.

## Verification

Static bootstrap, section-aware search, exact resource reads, and JSON usage failures succeed with the service unavailable and return the same content digest as the embedded catalog.

## Commands used

- `musoq describe`
- `musoq describe search`
- `musoq describe command`
- `musoq describe manual`
- `musoq describe spec`
- `musoq describe recipe`
- `musoq describe resource`
- `musoq describe error`
- `musoq completion script`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 9. Choose a query input form

_Part 2: Run and inspect queries_

Select inline SQL, a saved script, or a query file explicitly when automatic input resolution could be ambiguous.

## When to use this chapter

Use this decision before passing an input token that could be interpreted as SQL, a script name, or a filesystem path.

## Working method

1. Use inline SQL for short invocations where shell quoting remains readable.
2. Use `--hint script`, `--hint file`, or `--hint sql` to remove ambiguity in automation.
3. Use `--from-file` as the direct file-path form and keep the file under source control when reproducibility matters.

## Automation contract

Always set an input hint in generated automation unless the input is a literal SQL statement whose interpretation is unambiguous.

## Verification

Enable `--debug` only for diagnosis and confirm that the transformed query corresponds to the intended input source.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 10. Run queries

_Part 2: Run and inspect queries_

Execute one Musoq SQL statement or reusable input and choose execution options according to output, recursion, and failure needs.

## When to use this chapter

Use `musoq run` for ordinary query execution from a terminal, script, pipeline, or scheduled process.

## Working method

1. Execute directly when the query is known; discovery is optional and never a mandatory preflight.
2. Choose output and recursive CTE limits explicitly when downstream behavior depends on them.
3. Use `--progress` or `--execution-details` for observation without mixing diagnostics into result data.

## Automation contract

Keep query results on stdout and diagnostics on stderr; do not parse the human table format as an automation contract.

## Verification

Check the exit code, validate the selected output document, and compare row count or expected keys with the task invariant.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 11. Read SQL from files

_Part 2: Run and inspect queries_

Execute reviewed SQL from a file while keeping path resolution, encoding, and runtime parameters separate from query text.

## When to use this chapter

Use files for multiline queries, code review, repeatable operations, or shell environments with difficult quoting.

## Working method

1. Save the query as UTF-8 and review any external paths or side-effecting execution hooks.
2. Pass the path with `--from-file` or `--hint file` from the intended working directory.
3. Supply runtime parameters independently through `--params-json` rather than editing the query for each run.

## Automation contract

Resolve the absolute file path before execution and preserve the invocation directory when a query uses working-directory-relative resources.

## Verification

Run the same file twice with fixed inputs and confirm equivalent output and exit status.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 12. Run saved scripts

_Part 2: Run and inspect queries_

Resolve a managed script by name and execute it with the same output, parameter, and diagnostic contracts as inline SQL.

## When to use this chapter

Use a saved script when a query should be shared locally across invocations and managed through `musoq script`.

## Working method

1. List or show the managed script before using it in unattended execution.
2. Pass the script name to `musoq run` with `--hint script` when ambiguity is possible.
3. Record parameters and the script content digest with any retained output.

## Automation contract

Do not assume a script name resolves identically on another machine; discover it and its content in the target user profile first.

## Verification

Compare `musoq script show <name>` with the reviewed source and run a bounded fixture before production data.

## Commands used

- `musoq script show`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 13. Read standard input

_Part 2: Run and inspect queries_

Pipe supported tabular or structured input to a query while preserving stream ownership and end-of-input behavior.

## When to use this chapter

Use standard input to compose Musoq with a producer process instead of writing an intermediate file.

## Working method

1. Choose a stdin datasource method matching the producer format and its header or schema rules.
2. Keep the query itself in an argument or file so stdin remains dedicated to data.
3. Close the producer stream normally and inspect stderr when parsing or framing fails.

## Automation contract

Do not combine interactive prompts with redirected stdin; use non-interactive flags and explicit formats for every command in the pipeline.

## Verification

Feed a small fixture with known rows, assert the parsed row count, and then test empty and malformed input separately.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 14. Use parameters and settings profiles

_Part 2: Run and inspect queries_

Pass runtime values and source-specific settings as JSON while keeping query text stable and secrets outside command history.

## When to use this chapter

Use these options for parameterized scripts, source runtime tuning, or environments that vary without changing SQL.

## Working method

1. Define the script parameter contract and provide one JSON object through `--params-json`.
2. Provide source runtime profiles separately through `--settings-profiles-json`.
3. Validate names, types, and required values before opening remote resources.

## Automation contract

Construct JSON with a serializer, never by string concatenation, and avoid passing secret values on a command line when a protected environment store is available.

## Verification

Run with one known parameter set, one missing required value, and one invalid type; assert both exit codes and diagnostics.

## Commands used

- `musoq run`
- `musoq inspect`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 15. Choose output formats

_Part 2: Run and inspect queries_

Select table, JSON, CSV, YAML, raw, interpreted, or reconstructed output according to whether a human or program consumes the result.

## When to use this chapter

Choose a format before piping, storing, comparing, or displaying query results.

## Working method

1. Use table output for terminal reading and an explicit machine format for parsing.
2. Use CSV controls only with CSV and account for headers, quoting, and spreadsheet formula handling.
3. Use interpreted or reconstructed formats only when the query result requires their structural semantics.

## Automation contract

Request a format explicitly and parse exactly one stdout document; do not infer a schema from terminal column spacing or colors.

## Verification

Parse the output with the corresponding format parser and compare typed values, nulls, and row count with the expected result.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 16. Handle paging and redirected output

_Part 2: Run and inspect queries_

Keep long human output readable interactively while ensuring redirected output is complete, plain, and suitable for files or pipes.

## When to use this chapter

Use `--no-pager` for capture, automation, terminals without a pager, or any operation where immediate complete output is required.

## Working method

1. Allow paging only when both input and output are interactive.
2. Pass `--no-pager` for specifications, inspection, inference, and manual topics when capturing output.
3. Treat pager failure as a display fallback, not permission to truncate the document.

## Automation contract

Always disable paging or redirect stdout for unattended processes and reject ANSI or terminal control sequences in captured machine artifacts.

## Verification

Redirect the command to a file and confirm that the first and final expected sections are present with no control sequences.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 17. Separate stdout, stderr, and progress

_Part 2: Run and inspect queries_

Preserve stdout for result documents and stderr for diagnostics, warnings, debug text, progress, and execution details, including quiet Watch refreshes.

## When to use this chapter

Apply this contract to every pipeline, CI step, tool wrapper, process integration, and Watch attachment.

## Working method

1. Capture stdout and stderr independently.
2. Parse stdout only after the command exits successfully.
3. Stream or retain stderr for diagnosis without appending it to result data.
4. Enable outer Watch progress only when lifecycle telemetry is needed; a nested `run --progress` remains per-action progress.

## Automation contract

A progress line is never a query row; keep it on stderr, keep default Watch lifecycle output quiet, and tolerate append-only progress when output is redirected.

## Verification

Run Watch with neither, either, and both progress flags and prove that stdout remains a valid document while requested telemetry appears only on stderr.

## Commands used

- `musoq run`
- `musoq inspect`
- `musoq infer logs`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 18. Use exit codes in automation

_Part 2: Run and inspect queries_

Interpret stable process exits for success, query failure, usage error, service communication, missing resources, authorization, partial results, and cancellation.

## When to use this chapter

Use exit codes as the primary control signal for scripts, schedulers, CI, and process supervisors.

## Working method

1. Branch on the documented numeric exit code before parsing or acting on output.
2. Treat partial result as a distinct state that requires inspection rather than success.
3. Preserve cancellation exit 130 instead of converting it into a generic query failure.

## Automation contract

Do not decide success from message text; record exit code, command path, version, and stderr when an operation fails.

## Verification

Exercise at least one success, usage error, missing resource, and cancellation path in the invoking environment.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 19. Inspect compiler stages

_Part 2: Run and inspect queries_

Inspect logical, physical, execution, generated-code, or combined compiler output without executing datasource reads.

## When to use this chapter

Use `musoq inspect` only for expert diagnosis of parsing, planning, lowering, optimization, or generated code; it is not an execution preflight.

## Working method

1. First use `describe` when the missing information is command syntax, language semantics, or an installed runtime capability.
2. Choose one compiler stage or `all` and use the same input hint and settings that execution would use.
3. Request JSON when a tool will compare inspection structures.
4. Disable paging when retaining the complete inspection in an artifact.

## Automation contract

Never insert `inspect` automatically before `run`; inspection is compile-time evidence, not runtime proof or a required safety check.

## Verification

Confirm that the requested stage is present, the query was not executed, and the process returned success.

## Commands used

- `musoq inspect`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 20. Infer log schemas

_Part 2: Run and inspect queries_

Generate an editable text-schema query from a representative log file while keeping model selection, traces, and review explicit.

## When to use this chapter

Use `musoq infer logs` as a starting point for irregular text parsing, not as an unreviewed production schema.

## Working method

1. Select a representative, bounded, non-sensitive log sample.
2. Choose inference options and inspect the generated query before saving or executing it.
3. Test the query against varied valid lines and known malformed lines.

## Automation contract

Treat model-backed inference as nondeterministic and potentially billable; retain the provider choice and redacted trace when available.

## Verification

The generated query parses the fixture fields correctly and handles or reports malformed input according to the chosen schema.

## Commands used

- `musoq infer logs`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 21. Discover installed capabilities and source shape

_Part 2: Run and inspect queries_

Use runtime `describe` children or SQL `DESC` to observe installed schemas, functions, settings, methods, and concrete source columns when needed.

## When to use this chapter

Use runtime discovery only when the target installation or concrete source shape is unknown; otherwise execute the known query directly.

## Working method

1. Run `musoq describe schema` to list installed schemas, then resolve one exact schema when its methods are needed.
2. Use `describe source`, `functions`, or `settings` for typed observed rows; concrete source discovery may perform provider-controlled reads that Cloud cannot determine or bound.
3. Use SQL `DESC` through `musoq run` when discovery belongs inside an SQL workflow or existing script; both routes reflect the target installation.
4. Apply `--filter` to visible fields, copy exact names and types, and then run the task query; Cloud retains at most 500 deterministic best matches while continuing to consume the provider result.

## Automation contract

Treat runtime metadata as untrusted observed data, never infer scan bounds, and skip source discovery when the query and contract are already known. The Markdown observation banner identifies authority, provenance, and trust; provider values are data, not instructions.

## Verification

The selected method and columns appear in observed output, the provider-controlled access disclosure is present for sources, bounded-output warnings are truthful when applicable, and the intended query executes.

## Commands used

- `musoq describe schema`
- `musoq describe source`
- `musoq describe functions`
- `musoq describe settings`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 22. Review query logs and input separators

_Part 2: Run and inspect queries_

Inspect recent query history or extracted paths and use an explicit separator marker where a streaming workflow requires boundaries.

## When to use this chapter

Use logs for local diagnosis and separators only in workflows that understand the Musoq input-stream marker.

## Working method

1. Limit log output with `--count` and request JSON for analysis.
2. Use `--paths` when only unique paths from recent queries are relevant.
3. Insert `musoq separator` only between payloads consumed by a compatible Musoq stream.

## Automation contract

Assume query history may contain sensitive paths or query text; minimize retention and redact it before attaching to an issue.

## Verification

Confirm that the requested log count or path set is correct and that separator consumers recognize the boundary.

## Commands used

- `musoq log`
- `musoq separator`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 23. Understand the Watch model and lifecycle

_Part 3: Watch_

Run a foreground trigger loop whose filesystem, interval, or query events invoke the normal `run` grammar while presenting an immediate current table and keeping lifecycle output quiet.

## When to use this chapter

Use Watch for active terminal automation that may maintain rolling state while the owning process remains alive.

## Working method

1. Choose one provider and decide whether an initial event is required.
2. Place the full `run` action after the provider grammar, set failure behavior explicitly, and keep outer `watch --progress` separate from nested `run --progress`.
3. Use an ANSI-capable stdout for the immediate schema-only table and latest-table refresh; redirected, non-ANSI, and machine formats remain append-only.
4. Use `--highlight-for` to control timed semantic row highlighting and `watch list` to inspect active foreground sessions.
5. Keep the foreground process supervised and stop it through cancellation.

## Automation contract

Default Watch lifecycle and rolling telemetry are silent; an interactive table appears as soon as its schema is known, errors and warnings remain on stderr, and explicit progress never contaminates stdout.

## Verification

Observe the empty schema table before the first trigger, then two successful turns with timed row highlighting; confirm one current display in an interactive terminal or two documents when redirected, and verify quiet cancellation.

## Commands used

- `musoq watch`

## Example

```sql
param(directory: string)
let recursive: bool = true

SELECT * FROM os.files($directory, $recursive)
```

## Engineering notes

- Watch retains the normal `param` and `let` declaration preamble in synthesized rolling captures. A declaration used as a datasource argument is resolved for schema inspection and receives the runtime value during execution; required source parameters must be available when the session is registered.
- Pass required values through the `run` command's `--params-json` option.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 24. Watch filesystem changes

_Part 3: Watch_

Trigger queries for filesystem activity while accounting for recursive scope, debounce, coalescing, bounded ingress, overflow, and resynchronization signals.

## When to use this chapter

Use the filesystem provider for best-effort change detection in a selected directory tree.

## Working method

1. Select the smallest directory scope and enable recursion only when required.
2. Query `watch.events()` and inspect kind, subjects, sequence, observation time, and `RequiresResync`.
3. Set `--max-pending-events` for the workload and rescan authoritative state when correctness matters or resynchronization is requested.
4. Treat `native-overflow`, `watcher-recovery`, `ingress-capacity`, and `batch-deadline` as causes, not as a complete event log.

## Automation contract

Never treat a filesystem notification stream as a complete transaction log; bounded ingress may drop wakeups, and each affected batch emits one unconditional rescan warning without exposing watched paths.

## Verification

Create, modify, rename, and delete fixture files, saturate the pending-event limit, and confirm bounded batches, stable warning causes, and authoritative rescan behavior.

## Commands used

- `musoq watch`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 25. Run interval watches

_Part 3: Watch_

Execute a query on a duration-based trigger while controlling overlap expectations, costs, and cancellation.

## When to use this chapter

Use the interval provider for foreground polling where each turn can finish before the next useful observation.

## Working method

1. Choose an interval appropriate to source latency, rate limits, and expected change frequency.
2. Use `--initial` when a turn should run immediately rather than after the first interval.
3. Keep the action idempotent when repeated observations may contain the same state.

## Automation contract

Do not promise wall-clock scheduling precision or exactly-once execution; record successful turns and design safe retries.

## Verification

Measure several turns, confirm cancellation, and prove that repeated identical input does not create unsafe duplicate effects.

## Commands used

- `musoq watch`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 26. Run query-triggered watches

_Part 3: Watch_

Poll a probe query and trigger an action on a rising, falling, or changed single-value condition.

## When to use this chapter

Use the query provider when a datasource value, rather than a clock or filesystem event, determines whether work should run.

## Working method

1. Write a bounded probe that returns exactly one row and one column.
2. Choose the transition mode and polling duration explicitly.
3. Keep the action independent from transient probe diagnostics.

## Automation contract

Reject a probe with multiple rows or columns and treat network, quota, or model-backed probe cost as part of every poll.

## Verification

Drive the fixture through false, true, and changed values and assert which transitions invoke the action.

## Commands used

- `musoq watch`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 27. Configure Watch actions and failures

_Part 3: Watch_

Apply the standalone `run` grammar to every Watch turn, bound transactional output, and choose whether failures continue or terminate the loop.

## When to use this chapter

Use these rules when a Watch action formats output, consumes parameters, executes row hooks, or may fail transiently.

## Working method

1. Build and test the action first as an ordinary `musoq run` invocation.
2. Place the same grammar after the Watch provider, choose `--fail-fast` only when termination is intended, and set `--max-output-bytes` for bounded output.
3. Separate successful turn output from failure diagnostics in downstream consumers.
4. Remember that failed, cancelled, and output-limit turns publish no stdout or refresh sequence.

## Automation contract

A failed turn produces no successful result document; do not fabricate an empty result, exceed the configured byte limit, or advance external state from diagnostics.

## Verification

Exercise successful, failing, cancelled, formatter-failed, and output-limit turns with and without fail-fast, then assert loop, state, refresh, and output behavior.

## Commands used

- `musoq watch`
- `musoq run`

## Example

```text
musoq watch filesystem . run files.sql --from-file --params-json '{"directory":"D:\\incoming"}'
```

## Engineering notes

- `$name` references in projections and predicates use ordinary runtime binding. When `$name` is passed to a datasource method, Watch materializes the resolved `let` value, parameter default, or supplied `--params-json` value for detached schema inspection, then passes the runtime parameter dictionary to capture and action execution.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 28. Use rolling state, history, and diff

_Part 3: Watch_

Name a durable rolling state so Watch actions can compare current, previous, historical, and difference relations with commit-first delivery recovery.

## When to use this chapter

Use rolling relations when one trigger turn must be compared with earlier committed generations.

## Working method

1. Choose a stable state name and declare it with `--state`.
2. Use rolling methods only within an action whose state identity is explicit.
3. Set retention by age, generation count, and byte limit according to recovery needs.
4. Recover a committed generation with replay or `watch attach` when stdout delivery fails.

## Automation contract

A rolling turn formats into a bounded buffer, publishes the candidate, and only then writes stdout; after publication failure delivery is recoverable and the candidate is never discarded as if it were uncommitted.

## Verification

Run two controlled generations, inject publication and stdout failures, and validate current, previous, history, diff, replay, and recovery behavior against known fixture changes.

## Commands used

- `musoq watch`

## Engineering notes

- The durable definition includes script parameters, capture identities, slot schemas, and the rolling relation-source rewrite compatibility version. A generated relation contract change is rejected as a state definition mismatch; it never silently mixes old generations with a new action plan.
- After verifying the change, choose a new `--state` name or run `musoq watch state reset <state> --force`.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 29. Administer and replay Watch state

_Part 3: Watch_

List active foreground sessions, inspect durable state, export, reset, delete, enumerate generations, replay retained state, and attach to committed output without a live datasource read.

## When to use this chapter

Use state administration for diagnosis, archival, recovery, cleanup, or deterministic replay of a retained generation.

## Working method

1. Run `watch list` first when you need to see active foreground sessions, then list and show durable state metadata before performing a mutation.
2. Export or enumerate generations when evidence must be retained.
3. Use `watch attach` with `--once`, `--from-beginning`, `--after`, or a cursor file according to the recovery goal.
4. Remember that `--once` freezes the current feed-capable head, version-1 generations have no attachable result, and `watch replay --progress` is the explicit replay telemetry switch.
5. Use force or equivalent non-interactive authorization only after verifying the exact state name and operation.

## Automation contract

Treat reset and delete as destructive and require an exact reset-scoped state identity; `watch list` exposes only safe session metadata, while replay and attach read committed chunked result sidecars, never fabricate output for baseline generations, and report retention gaps explicitly.

## Verification

After each administration command, list or show the state and compare its generation and retention metadata; resume with a cursor and prove that a failed cursor update leaves an at-least-once duplicate window.

## Commands used

- `musoq watch list`
- `musoq watch state list`
- `musoq watch state show`
- `musoq watch state generations`
- `musoq watch state export`
- `musoq watch state reset`
- `musoq watch state delete`
- `musoq watch replay`
- `musoq watch attach`

## Engineering notes

- If a Watch action or rolling rewrite changes, inspect the state first and then either choose a new state name or explicitly reset the old history with `musoq watch state reset <state> --force`.
- A different runtime `param` value is part of the durable definition, so changing it for an existing state requires the same decision. Reset is destructive to retained generations.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 30. Design Watch automation and recovery

_Part 3: Watch_

Pipe successful Watch output to external automation with durable generation feeds, explicit cursors, bounded output, and at-least-once recovery semantics.

## When to use this chapter

Use this boundary before connecting Watch output to side effects, replaying a committed generation, or claiming delivery is exactly once.

## Working method

1. Name a rolling state and request JSON, YAML, CSV, raw, or table output according to the consumer contract.
2. Use `watch attach --once` for a bounded drain or a cursor file for continuous resume; keep the cursor parent directory pre-created.
3. Allow continuous attach to retry transient transport failures, but treat identity mismatch, integrity failure, missing result, and pruning as terminal recovery decisions.
4. Make downstream actions idempotent and persist their own evidence before treating a cursor update as acknowledgement.
5. Restart after transport, retention, reset, or output failures and reconcile the state identity and generation boundary.

## Automation contract

Attach delivery is at least once: stdout is flushed before the owner-restricted cursor advances, so a cursor-write failure can repeat a committed generation. Version-1 generations before the feed boundary and pruned cursors require explicit recovery; result sidecars count toward durable-byte quotas.

## Verification

Publish generations before and after subscription, exercise pagination and long-poll timeout, restart and reset the state, prune a cursor, and prove recovery never fabricates a baseline result or silently advances after a failed read.

## Commands used

- `musoq watch`
- `musoq watch attach`

## Engineering notes

- The local HTTP feed and result routes are internal service surfaces: clients should use `watch attach` unless they are testing the service contract directly.
- A generation result is held only while it is read; consumers do not pin retention, so retention gaps are normal and actionable.
- The tagged Windows ConPTY acceptance lane is local-only and quarantined; hosted CI keeps its test/contention lanes disabled and never invokes it.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 31. Understand the datasource model

_Part 4: Data sources, registries, and paths_

Treat each datasource as a versioned plugin that contributes schemas, methods, functions, runtime behavior, and sometimes dynamic CLI commands.

## When to use this chapter

Use this model before installation, compatibility analysis, query discovery, or plugin troubleshooting.

## Working method

1. Identify the package name, version, source registry, and plugin type.
2. Check compatibility with the installed Musoq CLI and core runtime versions.
3. Discover the loaded schema after installation rather than assuming package presence means successful activation.

## Automation contract

Keep package installation evidence distinct from runtime query evidence; a downloaded plugin can still fail to load or execute.

## Verification

The datasource appears in installed and loaded views and a representative bounded query succeeds.

## Commands used

- `musoq data-sources list`
- `musoq get data-sources`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 32. Discover and inspect data sources

_Part 4: Data sources, registries, and paths_

Search registries, list installed packages, show one package, and use runtime descriptions to inspect loaded schemas before selecting a datasource.

## When to use this chapter

Use discovery when choosing a provider or determining what is already installed on the target profile.

## Working method

1. Run registry search with a precise term and record the registry and package version.
2. List installed data sources in JSON for automation and show the selected item for details.
3. Compare installed packages with `musoq describe schema` and an optional concrete source description on the target profile.

## Automation contract

Use exact identifiers returned by discovery and avoid selecting a package based only on a similar display name.

## Verification

The selected identifier, version, registry, and loaded schema agree across discovery commands.

## Commands used

- `musoq data-sources search`
- `musoq data-sources list`
- `musoq data-sources show`
- `musoq get data-sources`
- `musoq describe schema`
- `musoq describe source`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 33. Install, update, and import data sources

_Part 4: Data sources, registries, and paths_

Add a datasource from a registry, update it within policy, or import a local package while retaining source and version evidence.

## When to use this chapter

Use installation commands after compatibility, registry trust, and package identity have been reviewed.

## Working method

1. Prefer an exact package version for reproducible environments.
2. Use import only for a reviewed local artifact and retain its digest.
3. Restart or refresh as required, then validate loading and representative runtime behavior.

## Automation contract

Do not advance a package train solely because installation succeeded; run a datasource-specific smoke query before continuing.

## Verification

Record package version and digest, loaded schema, and one successful representative query.

## Commands used

- `musoq data-sources install`
- `musoq data-sources update`
- `musoq data-sources import`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 34. Create a datasource project and set its source

_Part 4: Data sources, registries, and paths_

Create an editable datasource project, associate a source location, and open its folder only in an interactive environment.

## When to use this chapter

Use these commands for local plugin development or for correcting the source associated with a managed datasource.

## Working method

1. Create the project with a supported template and request JSON in headless workflows.
2. Set or review the source path using an absolute target that belongs to the intended plugin.
3. Open the folder only when an interactive desktop editor is available.

## Automation contract

Do not depend on editor launch in automation; JSON output must distinguish successful creation from the optional editor result.

## Verification

Inspect the generated project and source mapping, then build or load it with a bounded smoke query.

## Commands used

- `musoq data-sources create`
- `musoq data-sources set-source`
- `musoq data-sources folder`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 35. Uninstall data sources

_Part 4: Data sources, registries, and paths_

Remove one or all installed datasource packages with explicit non-interactive authorization and optional file retention.

## When to use this chapter

Use uninstall after checking query, script, tool, and Watch dependencies on the target package.

## Working method

1. List the exact installed snapshot and select one package or the full set.
2. Use interactive confirmation, or pass `--force` only after the scope is verified.
3. Choose file retention independently from authorization and inspect mixed failures after bulk removal.

## Automation contract

Treat uninstall-all as a bulk destructive operation; exit code 6 means a partial result that must be reconciled item by item.

## Verification

List installed and loaded data sources after removal and confirm that retained or deleted files match the chosen policy.

## Commands used

- `musoq data-sources uninstall`
- `musoq data-sources uninstall-all`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 36. Understand and inspect registries

_Part 4: Data sources, registries, and paths_

Use registries as named package indexes whose URL, default status, and returned metadata determine datasource discovery.

## When to use this chapter

Use registry inspection before trusting search results or changing the source used for package installation.

## Working method

1. List registries and identify the current default.
2. Show the selected registry and review its endpoint before any mutation.
3. Search with a narrow term and retain source provenance with the result.

## Automation contract

Do not merge results from different registries without retaining the registry identifier and package version for every candidate.

## Verification

The default registry and the registry used by search or installation are explicit in retained evidence.

## Commands used

- `musoq registry list`
- `musoq registry show`
- `musoq data-sources search`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 37. Add, update, select, and remove registries

_Part 4: Data sources, registries, and paths_

Change registry configuration deliberately while checking endpoint identity, default selection, and dependencies on the registry name.

## When to use this chapter

Use registry mutations for an approved package source migration or local development index.

## Working method

1. Add or update an HTTPS endpoint whose ownership and content policy are known.
2. Set a default only after confirming search and package identity on that endpoint.
3. Remove a registry only after another source can satisfy required package operations.

## Automation contract

Treat registry URLs as supply-chain configuration and require explicit review before changing them in an unattended environment.

## Verification

List and show registries after mutation, then perform a read-only search against the intended default.

## Commands used

- `musoq registry add`
- `musoq registry update`
- `musoq registry set-default`
- `musoq registry remove`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 38. Check datasource compatibility and trust

_Part 4: Data sources, registries, and paths_

Evaluate package origin, runtime dependencies, generated-code compatibility, native requirements, and external access before use.

## When to use this chapter

Use this review before promoting a datasource version or importing an unpublished package.

## Working method

1. Compare declared Musoq package dependencies with the CLI runtime versions.
2. Review native libraries, Python requirements, network destinations, credentials, and filesystem scope.
3. Run representative compile and execution probes with the actual target row shapes.

## Automation contract

A package restore or load test is insufficient compatibility evidence; include generated execution and a representative query path.

## Verification

Retain the exact package set, runtime version, query, output invariant, and any failure code or exception.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 39. Manage named query paths

_Part 4: Data sources, registries, and paths_

Register stable aliases for existing directories so queries can resolve selected roots without embedding machine-specific absolute paths.

## When to use this chapter

Use named paths for portable query inputs whose target directory is controlled on each machine.

## Working method

1. Add a unique alias for an existing absolute directory.
2. List mappings in JSON and verify the target on the machine that runs the service.
3. Remove an alias only after scripts, tools, and Watch actions no longer reference it.

## Automation contract

Named roots are machine-local; never assume the same alias points to the same data on another host without discovery.

## Verification

Resolve the alias in a bounded query and confirm that the returned files belong to the intended directory.

## Commands used

- `musoq path add`
- `musoq path list`
- `musoq path remove`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 40. Use datasource command extensions

_Part 4: Data sources, registries, and paths_

Discover optional command namespaces contributed by installed datasource packages without treating them as part of the fixed base CLI.

## When to use this chapter

Use an extension command when the installed package documents a task that is better expressed outside SQL.

## Working method

1. Run root help or completion on the target machine after the package is installed.
2. Inspect focused help and record the contributing package and version.
3. Capture stdout, stderr, and exit status using the same process contract as built-in commands.

## Automation contract

Dynamic extension commands are installation-dependent; discover them at runtime and provide a fallback when the namespace is absent.

## Verification

The extension appears in the command contract produced with that module loaded and its documented smoke command succeeds.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 41. Troubleshoot datasource loading

_Part 4: Data sources, registries, and paths_

Separate package discovery, file presence, dependency closure, shadow copy, schema loading, compilation, and runtime execution when diagnosis is required.

## When to use this chapter

Use this sequence when a datasource is installed but absent, fails to load, or fails only during query execution.

## Working method

1. Record installed metadata, package files, and service logs without exposing environment-variable values.
2. Compare the package dependency closure and runtime assembly versions.
3. Run DESC, compile inspection, and a representative query to locate the first failing stage.

## Automation contract

Report the first reproducible failing boundary and exact error; do not label every assembly failure as a generic probing issue.

## Verification

A fix must pass package load, schema discovery, and the original representative query on the affected profile.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 42. Manage scripts

_Part 5: Scripts, tools, buckets, images, and caches_

Create, clone, list, inspect, update, rename, locate, and delete managed SQL scripts under the current user profile.

## When to use this chapter

Use script management for reusable local SQL that should be named and inspected independently from execution.

## Working method

1. Create or clone a script, then inspect its content before use.
2. Use update or rename while keeping references in tools and automation synchronized.
3. Delete only after listing consumers and retaining source control where required.

## Automation contract

Use JSON for listings and mutations, and do not rely on opening a folder or editor in headless environments.

## Verification

Show the final script by name and execute it against a bounded fixture with explicit input hint.

## Commands used

- `musoq script create`
- `musoq script clone`
- `musoq script list`
- `musoq script show`
- `musoq script update`
- `musoq script rename`
- `musoq script folder`
- `musoq script delete`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 43. Manage tools

_Part 5: Scripts, tools, buckets, images, and caches_

Create and maintain named parameterized query tools whose definitions can be inspected, cloned, renamed, and removed.

## When to use this chapter

Use a tool when a reviewed query should expose a stable parameter contract to CLI, REST, or MCP callers.

## Working method

1. Create or clone a definition with a unique name, description, query, parameters, and output format.
2. Show and preview the tool before exposing it to another process.
3. Update, rename, or delete only after checking MCP contexts and callers.

## Automation contract

Discover the tool definition and parameter schema at runtime; do not synthesize parameter names from natural-language descriptions.

## Verification

The shown definition matches the reviewed query, and preview or bounded execution returns the expected columns.

## Commands used

- `musoq tool create`
- `musoq tool clone`
- `musoq tool list`
- `musoq tool show`
- `musoq tool update`
- `musoq tool rename`
- `musoq tool delete`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 44. Preview and execute tools

_Part 5: Scripts, tools, buckets, images, and caches_

Resolve a tool's dynamic parameters, inspect its prepared query, and execute it with a declared output format.

## When to use this chapter

Use preview before first execution, after a definition change, or when a caller reports parameter substitution problems.

## Working method

1. Show the tool and use focused help or completion to discover dynamic parameters.
2. Preview with representative non-sensitive values and inspect the prepared query.
3. Execute with an explicit output format and capture the normal process contract.

## Automation contract

Pass parameter tokens according to discovered grammar, distinguish missing from invalid types, and never log secret parameter values.

## Verification

Compare previewed parameter bindings with the execution result and assert expected rows or fields.

## Commands used

- `musoq tool preview`
- `musoq tool execute`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 45. Manage buckets

_Part 5: Scripts, tools, buckets, images, and caches_

Create, list, select, and delete named query contexts while making destructive deletion explicit.

## When to use this chapter

Use buckets when query execution needs a named isolated context supported by the selected operations.

## Working method

1. Create a uniquely named bucket and list it before passing it to a query.
2. Use `--bucket` only with queries that are designed for that context.
3. Delete with explicit force in automation only after confirming the exact bucket identity.

## Automation contract

Treat bucket deletion as destructive and do not infer bucket content or lifecycle from its name.

## Verification

List before and after the mutation, then run a bounded bucket-aware query when applicable.

## Commands used

- `musoq bucket create`
- `musoq bucket list`
- `musoq bucket delete`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 46. Encode images

_Part 5: Scripts, tools, buckets, images, and caches_

Convert a selected local image into a data URI on stdout for use by a compatible query or external workflow.

## When to use this chapter

Use image encoding when a downstream interface requires inline base64 data instead of a file path.

## Working method

1. Verify the file path, media type, size, and permission to process the image.
2. Run `musoq image encode` and redirect stdout when the value should not be printed to the terminal.
3. Pass the data URI only to a compatible and size-bounded consumer.

## Automation contract

Treat encoded content as the original image for confidentiality and retention; base64 is not encryption.

## Verification

Decode the result in a test step and compare its SHA-256 digest with the source file.

## Commands used

- `musoq image encode`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 47. Purge the compiled-query cache

_Part 5: Scripts, tools, buckets, images, and caches_

Remove cached compiled queries through the supported command when stale generated artifacts must be excluded from diagnosis.

## When to use this chapter

Use cache purge after runtime or datasource changes, or when evidence points to a stale compiled artifact.

## Working method

1. Record the symptom and relevant versions before clearing the cache.
2. Run the supported compiled-query purge with machine-readable output where available.
3. Repeat the original query and compare fresh compilation behavior.

## Automation contract

Do not present cache purge as a fix by itself; use it to test the stale-artifact hypothesis and retain before-and-after evidence.

## Verification

The command reports the purge result and the next query recompiles before reproducing or resolving the symptom.

## Commands used

- `musoq cache purge compiled-queries`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 48. Manage the local artifact lifecycle

_Part 5: Scripts, tools, buckets, images, and caches_

Track ownership and dependencies among scripts, tools, buckets, caches, Watch state, datasource packages, and configuration before cleanup.

## When to use this chapter

Use this inventory before profile migration, normal removal, purge, or manual filesystem maintenance.

## Working method

1. List each asset type through its supported command and record names and versions.
2. Map tool, script, datasource, named-path, bucket, and Watch state dependencies.
3. Use supported deletion commands in dependency order and retain required exports.

## Automation contract

Avoid deleting files directly while the service is running; prefer public lifecycle commands and require explicit scope for irreversible cleanup.

## Verification

Repeat the inventory after cleanup and confirm that remaining assets resolve without dangling references.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 49. Understand configuration and precedence

_Part 6: Configuration and the Musoq CLI service_

Distinguish packaged defaults, user settings, environment values, host arguments, and command options when resolving effective behavior.

## When to use this chapter

Use this model whenever observed service behavior differs between machines, users, shells, or launch methods.

## Working method

1. Identify the user profile and executable that own the invocation.
2. Record configuration sources by precedence without printing secret values.
3. Use a read command or diagnostic to confirm the effective non-secret setting.

## Automation contract

Never enumerate environment-variable values during diagnosis; record names, presence, source, and masked metadata only.

## Verification

Change one non-sensitive setting in an isolated profile and confirm which source wins, then restore it through the supported command.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 50. Inspect configuration

_Part 6: Configuration and the Musoq CLI service_

Read supported settings, service status, version, port, startup metrics, licenses, data sources, and environment metadata through `musoq get`.

## When to use this chapter

Use read-only commands to collect a baseline before mutation or troubleshooting.

## Working method

1. Select the narrowest `get` child for the information required.
2. Request JSON for structured collections or values when supported.
3. Mask secrets and retain the executable version with the result.

## Automation contract

Do not infer configuration from files alone; use supported read paths to observe the active profile and service state.

## Verification

Cross-check service version and port with the process you intend to contact, and validate returned JSON before use.

## Commands used

- `musoq get`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 51. Set and clear configuration

_Part 6: Configuration and the Musoq CLI service_

Modify supported settings through typed `set` and `clear` children, then verify the effective result and restart requirements.

## When to use this chapter

Use these commands instead of editing configuration files for supported keys.

## Working method

1. Read the current setting and record whether it is present.
2. Set or clear one key with the exact child command and validated value shape.
3. Read the value again and restart the service only when the setting requires it.

## Automation contract

Prefer one setting per operation, avoid echoing secrets, and retain only masked change evidence.

## Verification

The read command reports the intended effective state and a bounded operation demonstrates the changed behavior.

## Commands used

- `musoq set`
- `musoq clear`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 52. Manage datasource environment variables

_Part 6: Configuration and the Musoq CLI service_

Set, list, locate, and clear persisted environment values used by data sources without exposing their plaintext in routine output.

## When to use this chapter

Use the managed environment store for datasource configuration that must be available to the Musoq CLI service profile.

## Working method

1. Set a value through the supported command without placing it in logs or source control.
2. List variable names or masked status and inspect the store path only when file permissions must be checked.
3. Clear unused values and validate dependent data sources afterward.

## Automation contract

Never request or print all variable values; ask only for the name and whether a value is configured.

## Verification

Check restricted file permissions and run a bounded datasource query that uses the value without revealing it.

## Commands used

- `musoq get environment-variables`
- `musoq get environment-variables-file-path`
- `musoq set environment-variable`
- `musoq clear environment-variable`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 53. Locate configuration and profile files

_Part 6: Configuration and the Musoq CLI service_

Determine the active per-user home, settings, environment store, logs, plugins, scripts, tools, cache, and rolling-state locations safely.

## When to use this chapter

Use location discovery for backup, permissions, profile migration, or evidence collection.

## Working method

1. Identify the operating-system account and home override used by the process.
2. Use folder or path commands where available instead of assuming a default directory.
3. Stop the service before copying mutable databases or state directories.

## Automation contract

Treat every discovered path as profile-specific and validate it as a descendant of the intended Musoq home before file operations.

## Verification

Compare the discovered paths with the running process account and confirm backup readability in an isolated location.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 54. Understand service lifecycle and auto-start

_Part 6: Configuration and the Musoq CLI service_

Let execution commands start the Musoq CLI service when needed, or manage it explicitly for foreground supervision and diagnostics.

## When to use this chapter

Use lifecycle control when startup timing, logs, network binding, shutdown, or shared use must be explicit.

## Working method

1. Check service status before deciding whether to start another process.
2. Use automatic startup for ordinary queries and explicit serve mode for supervised sessions.
3. Stop through `musoq quit` and wait when a clean shutdown is required.

## Automation contract

Do not start duplicate service instances; status checks and single-instance enforcement are part of safe lifecycle management.

## Verification

Observe stopped, starting, ready, and stopped states with the expected port and process ownership.

## Commands used

- `musoq serve`
- `musoq quit`
- `musoq get is-running`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 55. Start the service in background or foreground

_Part 6: Configuration and the Musoq CLI service_

Choose detached background startup for ordinary local use or foreground waiting when a terminal or supervisor owns the process.

## When to use this chapter

Use `musoq serve` when startup must happen before other commands or when service logs need direct observation.

## Working method

1. Use background startup for the default local binding and confirm readiness before dependent work.
2. Use `--wait-until-exit` for a foreground process managed by the current terminal or supervisor.
3. Use auto-shutdown only with a reviewed idle timeout and workload model.

## Automation contract

Capture foreground service logs separately from client result streams and avoid treating process creation as readiness.

## Verification

Poll supported status or health until ready, run a smoke query, then stop and confirm clean exit.

## Commands used

- `musoq serve`
- `musoq get is-running`
- `musoq quit`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 56. Control network binding and exposure

_Part 6: Configuration and the Musoq CLI service_

Keep the service on loopback by default and require an explicit risk acknowledgement before binding to a non-local address.

## When to use this chapter

Use non-default binding only in a controlled network design that supplies isolation, authentication at the boundary, and monitoring.

## Working method

1. Prefer `127.0.0.1` and select a valid local port.
2. Review firewall, container, proxy, and tenant boundaries before any non-loopback bind.
3. Use the explicit risk flag only after that review and test from both allowed and disallowed origins.

## Automation contract

Never expose the service publicly by default; unsupported management routes are not a remote administration contract.

## Verification

Inspect the listening address and prove that only intended clients can connect to the supported interface.

## Commands used

- `musoq serve`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 57. Read status, version, port, and startup metrics

_Part 6: Configuration and the Musoq CLI service_

Confirm which Musoq CLI service process a client will use and retain startup measurements for diagnosis rather than assumption.

## When to use this chapter

Use these reads before API calls, performance comparison, or troubleshooting a stale background process.

## Working method

1. Read running status, server version, and port from the active profile.
2. Compare service version with the invoking CLI executable.
3. Collect startup metrics with environment and executable identity when investigating latency.

## Automation contract

Do not make performance claims without confirming executable hash, process version, platform, and cold or warm state.

## Verification

The reported port accepts the health request and the service and client versions belong to the intended release.

## Commands used

- `musoq get is-running`
- `musoq get server-version`
- `musoq get server-port`
- `musoq get startup-metrics`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 58. Configure Python support

_Part 6: Configuration and the Musoq CLI service_

Select automatic, enabled, or disabled Python support at service startup according to installed plugins and runtime availability.

## When to use this chapter

Use this option when Python datasource loading must be required, prohibited, or recovered independently from ordinary CLI use.

## Working method

1. Use automatic mode for optional detection, enabled when Python is required, and disabled when it must not load.
2. Verify the Python runtime and plugin dependencies under the same user account as the service.
3. Run a Python datasource smoke query separately from non-Python service health.

## Automation contract

Do not report the whole CLI unavailable when optional Python initialization fails and disabled mode permits non-Python operation.

## Verification

Start in each relevant mode and assert both service readiness and the expected Python datasource behavior.

## Commands used

- `musoq serve`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 59. Run diagnostics, review licenses, and recover

_Part 6: Configuration and the Musoq CLI service_

Use `doctor`, license output, status reads, and a bounded query to build a reproducible service health report.

## When to use this chapter

Use this sequence after installation, failed startup, unexpected configuration, or before reporting a defect.

## Working method

1. Run `musoq doctor --format json` and retain non-secret findings.
2. Read version, port, status, and licenses from the same executable and profile.
3. Restart cleanly when appropriate and rerun the first-query smoke test.

## Automation contract

Redact paths only when necessary for privacy, never include environment-variable values, and preserve error codes and causal messages.

## Verification

Diagnostics parse, license output is accessible offline, and the bounded query either succeeds or produces a reproducible failure record.

## Commands used

- `musoq doctor`
- `musoq get licenses`
- `musoq get is-running`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 60. Choose a machine integration boundary

_Part 7: MCP, tool REST, and AI clients_

Select CLI process invocation, MCP, or the supported tool REST surface according to discovery, isolation, and lifecycle requirements.

## When to use this chapter

Use this decision before connecting an AI client, automation runner, or application to Musoq.

## Working method

1. Use CLI `musoq describe` followed by `musoq run` for broad discovery, administration, execution, and stable exit semantics.
2. Use MCP `musoq_describe` followed by `musoq_execute_query` for context-scoped discovery, readable resources, and structured calls.
3. Use tool REST only for health, tool discovery, and named tool execution.

## Automation contract

Do not expand authority from one interface to another; a client configured for tools does not receive general management access.

## Verification

The selected contract exposes every required operation and no undocumented route or filesystem mutation is assumed.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 61. Enable and configure MCP

_Part 7: MCP, tool REST, and AI clients_

Enable the MCP server setting, start the service, and connect through the configured Streamable HTTP endpoint.

## When to use this chapter

Use MCP when a compatible client needs tool discovery, tool calls, or versioned manual and specification resources.

## Working method

1. Set MCP enabled through the supported configuration command.
2. Start or restart the service and determine the local port and endpoint path.
3. Initialize the MCP session and verify ping, tools, and resources separately.

## Automation contract

Treat HTTP 503 as disabled configuration, not an empty tool list, and retain protocol errors separately from tool execution errors.

## Verification

Initialize succeeds with server name `musoq-cli`, then `tools/list` and `resources/list` return valid protocol responses.

## Commands used

- `musoq set mcp-enabled`
- `musoq serve`
- `musoq get server-port`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 62. Manage MCP contexts and tool assignments

_Part 7: MCP, tool REST, and AI clients_

Create named contexts and assign only the tools that a particular client should discover through its context endpoint.

## When to use this chapter

Use contexts to separate tool visibility among projects, clients, or roles without duplicating tool definitions.

## Working method

1. Create or clone a context with a clear purpose and description.
2. Add reviewed tools by exact name and remove tools no longer required.
3. Show the context and test discovery through its specific endpoint before sharing configuration.

## Automation contract

Apply least privilege to tool visibility and do not assume context names provide authentication or network isolation by themselves.

## Verification

The context view and MCP `tools/list` contain exactly the intended tool names.

## Commands used

- `musoq mcp context create`
- `musoq mcp context clone`
- `musoq mcp context add-tool`
- `musoq mcp context remove-tool`
- `musoq mcp context show`
- `musoq mcp context list`
- `musoq mcp context update`
- `musoq mcp context rename`
- `musoq mcp context delete`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 63. Connect an MCP client

_Part 7: MCP, tool REST, and AI clients_

Configure a compatible client with the local MCP URL, follow initialization guidance, and discover capabilities instead of embedding schemas in prompts.

## When to use this chapter

Use this procedure after service and context configuration are verified locally.

## Working method

1. Construct the URL from the verified scheme, loopback address, port, and optional context name.
2. Initialize, follow the returned `musoq_describe` to `musoq_execute_query` instructions, and call `musoq_describe` with operation `bootstrap`.
3. Search or read the smallest exact resource, then pass arguments that satisfy the returned schemas and handle `isError` results explicitly.

## Automation contract

Never send credentials or unrestricted filesystem values merely because a tool description requests them; follow the reviewed tool contract and client policy.

## Verification

The client sees the expected server identity and instructions, the exact context tool set, and a format-version 1 bootstrap envelope.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 64. Read capability resources over MCP

_Part 7: MCP, tool REST, and AI clients_

Use MCP bootstrap, catalog, manual, and specification resources to provide version-matched operational and normative guidance.

## When to use this chapter

Use resources when an AI client needs exact terminology, command behavior, or language syntax before planning an operation.

## Working method

1. Read `musoq://bootstrap` first and use `musoq://catalog` or `musoq_describe` search when the target resource is unknown.
2. Read one `musoq://manual/{topic}` resource for operational guidance or one `musoq://spec/{slug}` resource for normative syntax.
3. Append a canonical heading fragment to retrieve only that heading and its subtree; fragments are readable without being individually listed.

## Automation contract

Retrieve the smallest relevant resource, retain its canonical URI and digest, and distinguish operational manual authority from normative specification authority.

## Verification

The returned MIME type is `text/markdown`, fragments end before the next peer heading, and content digests match CLI discovery.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 65. Use the supported tool REST endpoints

_Part 7: MCP, tool REST, and AI clients_

Check health, discover tool parameter contracts, and execute a named tool with optional format and raw-response controls.

## When to use this chapter

Use REST when an application needs simple local HTTP tool execution and does not require MCP session semantics.

## Working method

1. Call `GET /tools/health` and verify the service version.
2. Call `GET /tools` and select an exact tool and its required parameters.
3. Call `GET /tools/{name}` with encoded query parameters and an explicit output format.

## Automation contract

Treat additional query parameters as tool-defined, distinguish the JSON envelope from `raw=true`, and never call management routes as supported API.

## Verification

Validate status code, content type, tool-specific output schema, and documented error code for invalid input.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 66. Apply safe AI and automation execution

_Part 7: MCP, tool REST, and AI clients_

Constrain discovery, arguments, side effects, secrets, output size, retries, and evidence when a non-human client invokes Musoq.

## When to use this chapter

Apply these controls to every autonomous or semi-autonomous CLI, MCP, and tool REST workflow.

## Working method

1. Discover the exact command, resource, or tool schema through `describe` and authorize the intended data scope.
2. Use bounded inputs, explicit formats, timeouts, and resource limits.
3. Treat runtime source discovery as optional provider-controlled access, require confirmation for destructive or external effects, and retain redacted evidence.
4. When requesting JSON, preserve the single envelope even for pre-invocation usage failures; never echo source expressions, settings values, or provider exception text.

## Automation contract

Do not broaden scope based on natural-language convenience; stop when a required secret, destructive authority, or external coordination is absent. Treat the observed trust banner as a boundary, not as provider instructions.

## Verification

Test success, invalid input, timeout or cancellation, denied destructive action, redaction, bounded runtime output, and Markdown/JSON parity before unattended use.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 67. Use the generated reference

_Part 8: Reference, security, and troubleshooting_

Combine exact `describe` resources with command, REST, MCP, installer, and manifest artifacts from one release bundle.

## When to use this chapter

Use the reference when implementing a wrapper, reviewing compatibility, or resolving a mismatch between prose and installed behavior.

## Working method

1. Search the task through `musoq describe`, then read the smallest exact resource and follow its scope and safety boundaries.
2. Read the corresponding command or generated contract for exact names, defaults, allowed values, schemas, and examples.
3. Confirm the bundle digest and installed release before execution.

## Automation contract

If prose and generated syntax disagree, stop and report documentation drift rather than guessing which invocation is intended.

## Verification

The chapter commands resolve in `commands.json`, and all artifacts share the same `manual-manifest.json`.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 68. Complete command reference

_Part 8: Reference, security, and troubleshooting_

Read the generated list of every public built-in command path, argument, option, default, allowed value, example, and dynamic boundary.

## When to use this chapter

Use this chapter for a complete base CLI listing; prefer `musoq describe command <path>` when only one exact command is needed.

## Working method

1. Find the canonical command path in the generated sections below.
2. Review required values, defaults, allowed values, and examples without omitting parent grammar.
3. Confirm the path against `musoq describe command`, `musoq manual commands`, or focused help on the target release.

## Automation contract

Use `musoq describe command <path> --format json` for one command or parse `commands.json` for the complete schema instead of scraping Markdown.

## Verification

The reference contains every non-hidden, non-internal path from the persisted command contract and no path absent from that contract.

## Generated command reference

This section is generated from the persisted command schema. Use `musoq <path> --help`
for terminal-adapted help and `musoq describe command <path> --format json` for the
canonical machine-readable command record.

### `musoq bucket`

Manage storage buckets

### `musoq bucket create`

Create a new storage bucket

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the bucket to create |

Examples:

- `musoq bucket create my-bucket`

### `musoq bucket delete`

Delete a storage bucket

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the bucket to delete |

Examples:

- `musoq bucket delete my-bucket`

### `musoq bucket list`

List all storage buckets

Examples:

- `musoq bucket list`

### `musoq cache`

Manage local caches

### `musoq cache purge`

Purge cached data

### `musoq cache purge compiled-queries`

Purge persisted compiled query artifacts

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--progress` | Flag | default: `false` | Show live cache purge progress on stderr |

Examples:

- `musoq cache purge compiled-queries --force`
- `musoq cache purge compiled-queries --force --format json`

### `musoq clear`

Clear configuration values

### `musoq clear agent-coordinator-url`

Clear agent coordinator URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear agent-coordinator-url`

### `musoq clear agent-name`

Clear agent name

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear agent-name`

### `musoq clear api-key`

Clear API key

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear api-key`

### `musoq clear api-toolbox-url`

Clear API toolbox URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear api-toolbox-url`

### `musoq clear connect-after-start`

Clear connect after start setting

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear connect-after-start`

### `musoq clear connect-with-cloud`

Clear connect with cloud setting

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear connect-with-cloud`

### `musoq clear environment-variable`

Clear an environment variable

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Environment variable name to clear |

Examples:

- `musoq clear environment-variable MY_VAR`

### `musoq clear labels`

Clear agent labels

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear labels`

### `musoq clear log-path`

Clear log file path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear log-path`

### `musoq clear organization-id`

Clear organization ID

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear organization-id`

### `musoq clear sso-url`

Clear SSO URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear sso-url`

### `musoq clear update-data-sources`

Clear update data sources setting

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq clear update-data-sources`

### `musoq completion`

Generate shell completion integrations without modifying shell profiles

### `musoq completion script`

Print a shell completion script to stdout

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--alias` | OptionList | optional | Additional executable alias to register; may be repeated |
| `--executable` | Option | default: `musoq` | Executable path embedded in the generated script |
| `shell` | Argument | required | Shell adapter: powershell, bash, zsh, or fish Allowed: `bash`, `fish`, `powershell`, `zsh`. |

Examples:

- `musoq completion script bash --executable /opt/musoq/musoq`
- `musoq completion script powershell`
- `musoq completion script zsh --alias mq`

### `musoq data-sources`

Manage installed data sources (Python and .NET plugins)

### `musoq data-sources create`

Create a new Python data source from a template

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--template` | Option | optional | Template to use: basic, api, database (defaults to basic) |
| `name` | Argument | required | Name for the new Python data source |

Examples:

- `musoq data-sources create my_plugin`
- `musoq data-sources create my_plugin --template api`
- `musoq datasource create my_plugin`
- `musoq datasource create my_plugin --template api`

### `musoq data-sources folder`

Show or open the data sources folder

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--open` | Flag | default: `false` | Open the folder in the default file manager |
| `name` | OptionalArgument | optional | Optional name of a specific data source to get the folder for |

Examples:

- `musoq data-sources folder`
- `musoq data-sources folder --open`
- `musoq data-sources folder my-plugin`
- `musoq datasource folder`
- `musoq datasource folder --open`
- `musoq datasource folder my-plugin`

### `musoq data-sources import`

Import a data source from a local path or zip file

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--name` | Option | optional | Custom name for the imported data source (default: derived from path) |
| `--non-interactive` | Flag | default: `false` | Output progress as plain text instead of interactive progress bar |
| `path` | Argument | required | Local directory path or .zip file to import |

Examples:

- `musoq data-sources import /path/to/plugin`
- `musoq data-sources import /path/to/plugin --name my-plugin`
- `musoq data-sources import C:\Downloads\plugin.zip`
- `musoq datasource import /path/to/plugin`
- `musoq datasource import /path/to/plugin --name my-plugin`
- `musoq datasource import C:\Downloads\plugin.zip`

### `musoq data-sources install`

Install a data source from the plugin registry

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--channel` | Option | optional | Registry channel to install from (for example: stable, alpha, beta, rc) |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--non-interactive` | Flag | default: `false` | Output progress as plain text instead of interactive progress bar |
| `--offline` | Flag | default: `false` | Use cached registry only, don't fetch from remote |
| `--version` | Option | optional | Specific version to install (default: latest) |
| `name` | Argument | required | Plugin name to install from the registry (e.g., Acme.DataSource.Weather) |

Examples:

- `musoq data-sources install Acme.DataSource.Weather`
- `musoq data-sources install Acme.DataSource.Weather --channel alpha`
- `musoq data-sources install Acme.DataSource.Weather --version 1.0.0`
- `musoq datasource install Acme.DataSource.Weather`
- `musoq datasource install Acme.DataSource.Weather --channel alpha`
- `musoq datasource install Acme.DataSource.Weather --version 1.0.0`

### `musoq data-sources list`

List all installed data sources

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |
| `--search` | Option | optional | Filter data sources by search term |

Examples:

- `musoq data-sources list`
- `musoq data-sources list --search roslyn`
- `musoq datasource list`
- `musoq datasource list --search roslyn`

### `musoq data-sources search`

Search for data sources in the GitHub registry

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |
| `--offline` | Flag | default: `false` | Use cached registry only, don't fetch from remote |
| `--progress` | Flag | default: `false` | Show live registry search progress on stderr |
| `query` | OptionalArgument | optional | Search query (matches name, description, or tags). If empty, lists all available plugins. |

Examples:

- `musoq data-sources search`
- `musoq data-sources search --offline`
- `musoq data-sources search postgres`
- `musoq datasource search`
- `musoq datasource search --offline`
- `musoq datasource search postgres`

### `musoq data-sources set-source`

Set the installation source for a data source

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--channel` | Option | optional | The registry channel for future updates (only applicable when source is 'registry') |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--plugin` | Option | optional | The full registry plugin/package id (only applicable when source is 'registry') |
| `--registry` | Option | optional | The name of the registry (only applicable when source is 'registry') |
| `name` | Argument | required | Name of the data source to update |
| `source` | NamedValue | optional | The source type: 'registry', 'file', or 'manual' |

Examples:

- `musoq data-sources set-source my-plugin --source Manual`
- `musoq data-sources set-source my-plugin --source Registry --registry official`
- `musoq data-sources set-source my-plugin --source Registry --registry official --plugin Acme.DataSource.Weather --channel stable`
- `musoq datasource set-source my-plugin --source Manual`
- `musoq datasource set-source my-plugin --source Registry --registry official`
- `musoq datasource set-source my-plugin --source Registry --registry official --plugin Acme.DataSource.Weather --channel stable`

### `musoq data-sources show`

Show detailed information about a specific data source

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `name` | Argument | required | The name of the data source to show |

Examples:

- `musoq data-sources show my-plugin`
- `musoq datasource show my-plugin`

### `musoq data-sources uninstall`

Uninstall a data source

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Uninstall without an interactive confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--keep-files` | Flag | default: `false` | Deactivate the data source and its command modules without deleting data source files |
| `--progress` | Flag | default: `false` | Show live uninstall progress on stderr |
| `name` | Argument | required | The name of the data source to uninstall |

Examples:

- `musoq data-sources uninstall my-plugin`
- `musoq datasource uninstall my-plugin`

### `musoq data-sources uninstall-all`

Uninstall all installed data sources

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Uninstall without an interactive confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--keep-files` | Flag | default: `false` | Deactivate all data sources without deleting their files |
| `--progress` | Flag | default: `false` | Show live uninstall progress on stderr |

Examples:

- `musoq data-sources uninstall-all`
- `musoq data-sources uninstall-all --force`
- `musoq data-sources uninstall-all --keep-files --force`
- `musoq datasource uninstall-all`
- `musoq datasource uninstall-all --force`
- `musoq datasource uninstall-all --keep-files --force`

### `musoq data-sources update`

Update installed data sources from their registries

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--channel` | Option | optional | Switch a specific data source to a registry channel (for example: stable, alpha, beta, rc) |
| `--dry-run` | Flag | default: `false` | Show what would be updated without actually updating |
| `--force` | Flag | default: `false` | Reinstall even if already at the latest version |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--non-interactive` | Flag | default: `false` | Output progress as plain text instead of interactive progress bar |
| `--stop-on-error` | Flag | default: `false` | Stop updating on first error (default: continue on error) |
| `name` | OptionalArgument | optional | Optional: Name of a specific data source to update (if not specified, all are updated) |
| `version` | OptionalArgument | optional | Optional: Specific version to update to (only valid when name is specified). If not provided, updates to the latest version. |

Examples:

- `musoq data-sources update`
- `musoq data-sources update --dry-run`
- `musoq data-sources update --force`
- `musoq data-sources update --non-interactive`
- `musoq data-sources update my-plugin --channel beta`
- `musoq data-sources update my-plugin --channel stable`
- `musoq data-sources update my-plugin 1.0.0-alpha.1`
- `musoq datasource update`
- `musoq datasource update --dry-run`
- `musoq datasource update --force`
- `musoq datasource update --non-interactive`
- `musoq datasource update my-plugin --channel beta`
- `musoq datasource update my-plugin --channel stable`
- `musoq datasource update my-plugin 1.0.0-alpha.1`

### `musoq describe`

Discover Musoq commands, documentation, and installed capabilities

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |

Examples:

- `musoq describe`
- `musoq describe --format json`

### `musoq describe command`

List commands or read one exact command contract

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `path` | VariadicArgument | optional | Exact command path |

Examples:

- `musoq describe command`
- `musoq describe command tool list`

### `musoq describe error`

Explain a Musoq error and provide deterministic recovery actions

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `code` | Argument | required | Exit code or known diagnostic family |

Examples:

- `musoq describe error 2`
- `musoq describe error not-found --format json`

### `musoq describe functions`

Describe functions installed for one schema context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--filter` | Option | optional | Case-insensitive substring filter over visible runtime fields |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `schema-context` | Argument | required | Installed schema context, with or without the # prefix |

Examples:

- `musoq describe functions #metadata --filter log --format json`
- `musoq describe functions metadata`

### `musoq describe manual`

List the manual or read one exact chapter or section

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `--section` | Option | optional | Exact canonical heading slug |
| `topic` | OptionalArgument | optional | Manual topic, slug, alias, or chapter number |

Examples:

- `musoq describe manual`
- `musoq describe manual first-query`
- `musoq describe manual first-query --section run-the-query`

### `musoq describe recipe`

List or read task-oriented cookbook entries

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `topic` | OptionalArgument | optional | Recipe name, alias, or chapter number |

Examples:

- `musoq describe recipe`
- `musoq describe recipe query-csv-files`

### `musoq describe resource`

Resolve one canonical musoq:// resource or heading fragment

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `musoq-uri` | Argument | required | Canonical musoq:// resource URI |

Examples:

- `musoq describe resource musoq://spec/language#desc-statement`

### `musoq describe schema`

List installed schemas or describe one installed schema

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--filter` | Option | optional | Case-insensitive substring filter over visible runtime fields |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `name` | OptionalArgument | optional | Installed schema name, with or without the # prefix |

Examples:

- `musoq describe schema`
- `musoq describe schema metadata --format json`

### `musoq describe search`

Search the offline Musoq capability catalog

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `--kind` | OptionList | optional | Restrict results; repeat for multiple kinds Allowed: `command`, `error`, `manual`, `recipe`, `spec`. |
| `--limit` | Option | default: `10` | Maximum number of results (1-50) |
| `text` | Argument | required | Task, phrase, identifier, alias, or musoq:// URI |

Examples:

- `musoq describe search functions --kind spec --format json`
- `musoq describe search query a CSV file`

### `musoq describe settings`

Describe safe setting metadata for one source or alias

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--filter` | Option | optional | Case-insensitive substring filter over visible runtime fields |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `source-or-alias` | Argument | required | One concrete source expression or query alias |

Examples:

- `musoq describe settings #metadata.logs()`
- `musoq describe settings sourceAlias --format json`

### `musoq describe source`

Describe the observed shape of one concrete source

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--filter` | Option | optional | Case-insensitive substring filter over visible runtime fields |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `--top-level-only` | Flag | default: `false` | Return only source columns whose names are not nested paths |
| `source-expression` | Argument | required | One concrete schema source expression |

Examples:

- `musoq describe source #csv.from('data.csv') --top-level-only --format json`
- `musoq describe source #json.from('data.json')`

### `musoq describe spec`

List or read an authoritative specification document or section

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `markdown` | Output format: markdown or json Allowed: `json`, `markdown`. |
| `--section` | Option | optional | Exact canonical heading slug |
| `slug` | OptionalArgument | optional | Specification slug |

Examples:

- `musoq describe spec`
- `musoq describe spec language --section desc-statement`

### `musoq doctor`

Run environment and server diagnostics

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |
| `--json` | Flag | default: `false` | Print machine-readable diagnostics as JSON |

Examples:

- `musoq doctor`
- `musoq doctor --json`

### `musoq get`

Retrieve system information and configuration

### `musoq get data-sources`

List all available data sources and their schemas

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq get data-sources`

### `musoq get environment-variables`

List configured environment variables

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |
| `--show-sensitive` | Option | default: `false` | Show sensitive environment variables |

Examples:

- `musoq get environment-variables`
- `musoq get environment-variables --show-sensitive`

### `musoq get environment-variables-file-path`

Show the file path where environment variables are stored

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq get environment-variables-file-path`

### `musoq get is-running`

Check if the Musoq server is currently running

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--exit-zero-when-down` | Flag | default: `false` | Always exit with code 0, even when server is not running |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |
| `--json` | Flag | default: `false` | Print machine-readable JSON output |
| `--wait` | Option | optional | Wait up to N seconds for the server to become ready |

Examples:

- `musoq get is-running`
- `musoq get is-running --exit-zero-when-down`
- `musoq get is-running --json`
- `musoq get is-running --wait 10`

### `musoq get licenses`

Show license information for installed components

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |

Examples:

- `musoq get licenses`

### `musoq get server-port`

Show the port the server is listening on

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq get server-port`

### `musoq get server-version`

Show the running server version

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq get server-version`

### `musoq get startup-metrics`

Show server startup performance metrics (cold start time)

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq get startup-metrics`

### `musoq image`

Image manipulation commands

### `musoq image encode`

Encode image file to base64

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `file-path` | Argument | required | Path to the image file to encode |

Examples:

- `musoq image encode photo.jpg`

### `musoq infer`

Infer Musoq queries from files

### `musoq infer logs`

Infer an editable text-schema query for a log file

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--diagnostics` | Flag | default: `false` | Write inference diagnostics to stderr |
| `--execution-details` | Flag | default: `false` | Show live query execution phases when using --run |
| `--format` | Option | optional | Output format used with --run Allowed: `csv`, `interpreted_json`, `interpreted_yaml`, `json`, `raw`, `reconstructed_json`, `reconstructed_yaml`, `table`, `yaml`. |
| `--json` | Flag | default: `false` | Write the full inference response as JSON |
| `--llm` | Flag | default: `false` | Allow the configured textual model to inspect redacted samples when deterministic inference is uncertain |
| `--llm-trace` | Flag | default: `false` | Write a redacted end-to-end trace of the request sent to the configured textual model |
| `--no-llm` | Flag | default: `false` | Disable model fallback inference (model fallback is disabled by default) |
| `--no-pager` | Flag | default: `false` | Write the complete generated query directly instead of opening a pager |
| `--no-regex` | Flag | default: `false` | Do not allow regex pattern fields in generated text schemas |
| `--no-show-types` | Flag | default: `false` | Hide friendly column types below table headers when using --run |
| `--overwrite-script` | Flag | default: `false` | Overwrite an existing script when used with --save-script |
| `--progress` | Flag | default: `false` | Show live query progress on stderr when using --run |
| `--run` | Flag | default: `false` | Run the generated query after inference |
| `--sample-lines` | Option | optional | Number of log lines to sample, capped at 5000 |
| `--save-script` | Option | optional | Save the generated query as a SQL script |
| `--show-query` | Flag | default: `false` | Show the generated query when using --run |
| `--table-layout` | Option | optional | Table layout when using --run: auto, grid, records, or compact Allowed: `auto`, `compact`, `grid`, `records`. |
| `path` | Argument | required | Path to the log file to infer |

Examples:

- `musoq infer logs app.log`
- `musoq infer logs app.log --run`
- `musoq infer logs app.log --save-script app-log.sql`

### `musoq inspect`

Inspect compiler plans, generated code, and intermediate representation without executing sources

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--from-file` | Flag | default: `false` | Treat <input> as a file path (equivalent to --hint file) |
| `--hint` | Option | default: `None` | Hint for input type: none (auto-detect), file, sql, or script |
| `--no-pager` | Flag | default: `false` | Write the complete inspection directly instead of opening a pager |
| `--progress` | Flag | default: `false` | Show live inspection phases on stderr |
| `--recursive-max-iterations` | Option | optional | Maximum recursive CTE iterations |
| `--recursive-max-rows` | Option | optional | Maximum rows accepted by a recursive CTE |
| `--recursive-max-snapshot-rows` | Option | optional | Maximum invariant rows retained by a recursive CTE |
| `--settings-profiles-json` | Option | optional | JSON object with source runtime settings profiles |
| `--stage` | Option | default: `execution` | Inspection stage: logical, logical-initial, logical-optimized, physical, physical-initial, physical-optimized, execution, execution-initial, execution-optimized, planning, optimizer-trace, generated-code, all. Aliases: ir, intermediate Allowed: `all`, `execution`, `execution-initial`, `execution-optimized`, `generated-code`, `intermediate`, `ir`, `logical`, `logical-initial`, `logical-optimized`, `optimizer-trace`, `physical`, `physical-initial`, `physical-optimized`, `planning`. |
| `input` | Argument | required | SQL query string, script name, or file path |

Examples:

- `musoq inspect SELECT 1 FROM system.dual()`
- `musoq inspect SELECT 1 FROM system.dual() --stage all --format json`
- `musoq inspect SELECT 1 FROM system.dual() --stage execution`
- `musoq inspect SELECT 1 FROM system.dual() --stage generated-code`
- `musoq inspect SELECT 1 FROM system.dual() --stage physical`
- `musoq inspect query.sql --from-file --stage logical`

### `musoq log`

Show recent query execution logs

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--count` | Option | optional | Number of recent logs to display (default: 10) |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |
| `--paths` | Flag | default: `false` | Show unique paths extracted from query history instead of the full query log |

Examples:

- `musoq log`
- `musoq log --count 20`
- `musoq log --paths`

### `musoq manual`

Read the versioned Musoq CLI user manual

Examples:

- `musoq manual`
- `musoq manual commands --path run`
- `musoq manual list`
- `musoq manual show first-query`

### `musoq manual commands`

Read the generated CLI command contract

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--no-pager` | Flag | default: `false` | Write the complete document directly to stdout |
| `--path` | Option | optional | Exact command path to select, for example 'tool list' |

Examples:

- `musoq manual commands`
- `musoq manual commands --format json`
- `musoq manual commands --path run`

### `musoq manual list`

List all manual chapters in reading order

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table or json Allowed: `json`, `table`. |

Examples:

- `musoq manual list`
- `musoq manual list --format json`

### `musoq manual show`

Show a manual chapter; omit the topic to show the index

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--no-pager` | Flag | default: `false` | Write the complete document directly to stdout |
| `--raw` | Flag | default: `false` | Write the stored Markdown without presentation processing |
| `topic` | OptionalArgument | optional | Chapter topic, slug, or number; defaults to index |

Examples:

- `musoq manual show 068 --raw`
- `musoq manual show exit-code-reference --format json`
- `musoq manual show first-query`

### `musoq mcp`

Manage MCP contexts for per-context tool isolation.

### `musoq mcp context`

Manage tool contexts exposed via MCP endpoints

### `musoq mcp context add-tool`

Add a tool to a context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `context-name` | Argument | required | Name of the context |
| `tool-name` | Argument | required | Name of the tool to add |

Examples:

- `musoq mcp context add-tool programming find-all-methods`

### `musoq mcp context clone`

Clone an existing context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `new-name` | OptionalArgument | optional | Optional new context name (auto-generated if omitted) |
| `source-name` | Argument | required | Existing context name to clone |

Examples:

- `musoq mcp context clone programming`
- `musoq mcp context clone programming programming-copy`

### `musoq mcp context create`

Create a new context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--description` | Option | optional | Optional description for the context |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the context to create |

Examples:

- `musoq mcp context create programming`
- `musoq mcp context create programming --description Tools for programming tasks`

### `musoq mcp context delete`

Delete a context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the context to delete |

Examples:

- `musoq mcp context delete programming`

### `musoq mcp context list`

List all available contexts with tool counts

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq mcp context list`

### `musoq mcp context remove-tool`

Remove a tool from a context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `context-name` | Argument | required | Name of the context |
| `tool-name` | Argument | required | Name of the tool to remove |

Examples:

- `musoq mcp context remove-tool programming find-all-methods`

### `musoq mcp context rename`

Rename an existing context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Existing context name to rename |
| `new-name` | Argument | required | New context name |

Examples:

- `musoq mcp context rename programming dev-tools`

### `musoq mcp context show`

Show details for a single context

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the context to show |

Examples:

- `musoq mcp context show programming`

### `musoq mcp context update`

Update context metadata

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--description` | Option | default: `` | New description for the context |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the context to update |

Examples:

- `musoq mcp context update programming --description `
- `musoq mcp context update programming --description Tools for code investigation`

### `musoq path`

Manage named directory paths for queries

### `musoq path add`

Register an absolute root or relative component under a named query path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--relative` | Flag | default: `false` | Store the value as a relative path component |
| `name` | Argument | required | Path alias |
| `path` | Argument | required | Path value to register |

Examples:

- `musoq path add service-logs .\logs`
- `musoq path add system-folder system\folder --relative`

### `musoq path check`

Check whether absolute named paths are available

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |
| `name` | OptionalArgument | optional | Optional path alias |

Examples:

- `musoq path check`
- `musoq path check service-logs --format json`

### `musoq path evaluate`

Evaluate named path and current-directory markers in arbitrary text

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `text` | Argument | required | Text containing @path/<alias> or @cwd markers |

Examples:

- `musoq path evaluate @path/root/@path/system-folder`
- `musoq path evaluate input=@cwd/data.csv --format json`

### `musoq path list`

List registered named query paths

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, csv, or tsv Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq path list`
- `musoq path list --format json`

### `musoq path remove`

Remove a named query path mapping

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Path alias |

Examples:

- `musoq path remove service-logs`
- `musoq path remove service-logs --format json`

### `musoq path show`

Show a named query path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Path alias |

Examples:

- `musoq path show service-logs`
- `musoq path show service-logs --format json`

### `musoq path update`

Update an existing named query path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--relative` | Flag | default: `false` | Store the value as a relative path component |
| `name` | Argument | required | Path alias |
| `path` | Argument | required | Replacement path value |

Examples:

- `musoq path update service-logs D:\logs`
- `musoq path update system-folder system\folder --relative`

### `musoq quit`

Stop the running Musoq server

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--wait` | Option | optional | Wait up to N seconds for the server to stop |

Examples:

- `musoq quit`
- `musoq quit --wait 10`

### `musoq registry`

Manage data source registries

### `musoq registry add`

Add a new data source registry

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--default` | Flag | default: `false` | Set as default registry |
| `--description` | Option | optional | Optional description for the registry |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--token` | Option | optional | Optional authentication token for private registries. Supports template syntax (e.g., '{{ GITHUB_TOKEN }}') to reference environment variables. |
| `name` | Argument | required | Name of the registry |
| `url` | Argument | required | URL to the registry JSON file |

Examples:

- `musoq registry add backup https://backup.com/registry.json --description Backup registry`
- `musoq registry add custom https://example.com/registry.json`
- `musoq registry add internal https://internal.com/registry.json --default`

### `musoq registry list`

List all configured data source registries

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--enabled-only` | Flag | default: `false` | Show only enabled registries |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |

Examples:

- `musoq registry list`
- `musoq registry list --enabled-only`

### `musoq registry remove`

Remove a data source registry

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Force removal of system registries (like 'official') |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the registry to remove |

Examples:

- `musoq registry remove custom`

### `musoq registry set-default`

Set a registry as the default

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the registry to set as default |

Examples:

- `musoq registry set-default official`

### `musoq registry show`

Show detailed information about a specific registry

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the registry to show |

Examples:

- `musoq registry show official`

### `musoq registry update`

Update a data source registry configuration

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--description` | Option | optional | New description for the registry |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `--url` | Option | optional | New URL for the registry |
| `name` | Argument | required | Name of the registry to update |

Examples:

- `musoq registry update custom --description Updated description`
- `musoq registry update custom --url https://new-url.com/registry.json`

### `musoq run`

Execute a SQL query from a string, script name, or file path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--bucket` | Option | optional | Bucket name for query context |
| `--debug` | Flag | default: `false` | Show the transformed query before execution |
| `--excel-safe` | Flag | default: `false` | Prefix formula-like string cells for safer spreadsheet import (CSV only) |
| `--execute` | Option | optional | Expression to execute |
| `--execution-details` | Flag | default: `false` | Show execution details including phase changes and data source progress |
| `--filter` | Option | optional | Filter desc output by name (case-insensitive substring match) |
| `--format` | Option | optional | Output format (json, csv, table, etc.) Allowed: `csv`, `interpreted_json`, `interpreted_yaml`, `json`, `raw`, `reconstructed_json`, `reconstructed_yaml`, `table`, `yaml`. |
| `--from-file` | Flag | default: `false` | Treat <input> as a file path (equivalent to --hint file) |
| `--hint` | Option | default: `None` | Hint for input type: none (auto-detect), file, sql, or script Allowed: `file`, `none`, `script`, `sql`. |
| `--no-clickable-cells` | Flag | default: `false` | Disable clickable cells (URLs and file paths) in table output |
| `--no-header` | Flag | default: `false` | Skip header row in CSV output |
| `--no-show-types` | Flag | default: `false` | Hide friendly column types below table headers |
| `--params-json` | Option | optional | JSON object with runtime-v2 script parameter values |
| `--progress` | Flag | default: `false` | Show live query progress on stderr |
| `--recursive-max-iterations` | Option | optional | Maximum recursive CTE iterations |
| `--recursive-max-rows` | Option | optional | Maximum rows accepted by a recursive CTE |
| `--recursive-max-snapshot-rows` | Option | optional | Maximum invariant rows retained by a recursive CTE |
| `--settings-profiles-json` | Option | optional | JSON object with source runtime settings profiles |
| `--stacktrace` | Flag | default: `false` | Include stack trace in error output |
| `--table-layout` | Option | optional | Table layout: auto, grid, records, or compact Allowed: `auto`, `compact`, `grid`, `records`. |
| `--unquoted` | Flag | default: `false` | Disable quoting in CSV output |
| `input` | Argument | required | SQL query string, script name, or file path |

Examples:

- `musoq run /path/to/query.sql --hint file`
- `musoq run SELECT 1 FROM system.dual()`
- `musoq run SELECT 1 FROM system.dual() --debug`
- `musoq run SELECT 1 FROM system.dual() --execution-details`
- `musoq run SELECT 1 FROM system.dual() --format json`
- `musoq run desc functions #system --filter date`
- `musoq run my_query --hint script`
- `musoq run my_script`
- `musoq run query.sql`
- `musoq run query.sql --from-file`

Dynamic values are resolved at invocation time; inspect help or completion on the target machine.

### `musoq script`

Manage SQL scripts

### `musoq script clone`

Clone an existing SQL script

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | OptionalArgument | optional | Optional name for the cloned script. If not provided, a numeric suffix will be added |
| `source` | Argument | required | Name of the SQL script to clone |

Examples:

- `musoq script clone my_query`
- `musoq script clone my_query my_query_copy`
- `musoq script clone my_query.sql my_query_backup.sql`

### `musoq script create`

Create a new SQL script and open it in the default editor

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--content` | Option | optional | Initial SQL content for the script |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | SQL script name to create |

Examples:

- `musoq script create my_query`
- `musoq script create my_query.sql`

### `musoq script delete`

Delete an SQL script

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | SQL script name to delete |

Examples:

- `musoq script delete my_query`
- `musoq script delete my_query.sql`

### `musoq script folder`

Show or open the SQL scripts folder

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--open` | Flag | default: `false` | Open the folder in the system's default file explorer |

Examples:

- `musoq script folder`
- `musoq script folder --open`

### `musoq script list`

List all SQL scripts

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |
| `--search` | Option | optional | Filter scripts by search term |

Examples:

- `musoq script list`
- `musoq script list --search query`

### `musoq script rename`

Rename an existing SQL script

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Existing SQL script name to rename |
| `new-name` | Argument | required | New SQL script name |

Examples:

- `musoq script rename old_query new_query`
- `musoq script rename old_query.sql new_query.sql`

### `musoq script update`

Open an existing SQL script in the default editor

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | SQL script name to update |

Examples:

- `musoq script update my_query`
- `musoq script update my_query.sql`

### `musoq separator`

Insert a separator marker in the input stream

Examples:

- `musoq separator`

### `musoq serve`

Start the Musoq CLI service (auto-starts when running queries)

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--auto-shutdown` | Flag | default: `false` | Enable auto-shutdown after idle period (configured via AutoShutdown:IdleTimeout in appsettings) |
| `--bind` | Option | optional | Bind address for the server (default: 127.0.0.1). Use 0.0.0.0 to expose on all interfaces (requires confirmation) |
| `--i-accept-the-risk` | Flag | default: `false` | Skip confirmation prompt when binding to non-localhost addresses (for automated deployments) |
| `--is-independent-process` | Flag | default: `false` | Run as an independent background process |
| `--port` | Option | optional | Port number for the server (default: 5000) |
| `--python-support` | Option | optional | Python support mode: auto (default), enabled, or disabled Allowed: `auto`, `disabled`, `enabled`. |
| `--wait-until-exit` | Flag | default: `false` | Wait until the server is explicitly stopped |

Examples:

- `musoq serve`
- `musoq serve --auto-shutdown`
- `musoq serve --bind 0.0.0.0 --i-accept-the-risk`
- `musoq serve --port 8080`
- `musoq serve --python-support disabled`
- `musoq serve --wait-until-exit`

### `musoq set`

Set configuration values

### `musoq set agent-coordinator-url`

Set agent coordinator URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set agent-coordinator-url https://coordinator.example.com`

### `musoq set agent-name`

Set agent name

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set agent-name my-agent`

### `musoq set api-key`

Set API key

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set api-key key-abc-123`

### `musoq set api-toolbox-url`

Set API toolbox URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set api-toolbox-url https://api.example.com`

### `musoq set connect-after-start`

Set whether to connect after start

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set connect-after-start true`

### `musoq set connect-with-cloud`

Set whether to connect with cloud

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set connect-with-cloud true`

### `musoq set environment-variable`

Set an environment variable

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Environment variable name |
| `value` | Argument | required | Environment variable value |

Examples:

- `musoq set environment-variable MY_VAR my_value`

### `musoq set labels`

Set agent labels

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `labels` | VariadicArgument | optional | Labels to set (comma-separated) |

Examples:

- `musoq set labels prod us-east`

### `musoq set log-path`

Set log file path

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set log-path /var/log/musoq`

### `musoq set mcp-enabled`

Enable or disable MCP server (takes effect immediately)

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set mcp-enabled true`

### `musoq set organization-id`

Set organization ID

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set organization-id org-123`

### `musoq set sso-url`

Set SSO URL

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set sso-url https://sso.example.com`

### `musoq set update-data-sources`

Set whether to update data sources on start

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `value` | Argument | required | Value to set |

Examples:

- `musoq set update-data-sources true`

### `musoq spec`

View Musoq specification documents

### `musoq spec autonomous-plugin-development`

Show the Autonomous Plugin Development

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec autonomous-plugin-development`
- `musoq spec autonomous-plugin-development --update`

### `musoq spec binary-text`

Show the Musoq Interpretation Schemas (Binary/Text) specification

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec binary-text`
- `musoq spec binary-text --update`

### `musoq spec dotnet-plugins-zip`

Show the .NET Plugins ZIP Specification

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec dotnet-plugins-zip`
- `musoq spec dotnet-plugins-zip --update`

### `musoq spec language`

Show the Musoq Core SQL Language specification

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec language`
- `musoq spec language --update`

### `musoq spec list`

List all available specification documents

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--update` | Flag | default: `false` | Download the latest versions of all specs from GitHub before listing |

Examples:

- `musoq spec list`
- `musoq spec list --update`

### `musoq spec python-plugins-zip`

Show the Python Plugins ZIP Specification

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec python-plugins-zip`
- `musoq spec python-plugins-zip --update`

### `musoq spec table-couple`

Show the Musoq TABLE and COUPLE Statements specification

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--pager` | Flag | default: `false` | Open the specification in a pager when the terminal is interactive |
| `--progress` | Flag | default: `false` | Show live specification update progress on stderr |
| `--raw` | Flag | default: `false` | Print raw markdown without rendering |
| `--update` | Flag | default: `false` | Download the latest version of this spec from GitHub before displaying it |

Examples:

- `musoq spec table-couple`
- `musoq spec table-couple --update`

### `musoq tool`

Manage and execute tools with dynamic parameters

### `musoq tool clone`

Clone an existing tool

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | OptionalArgument | optional | Optional name for the cloned tool. If not provided, a numeric suffix will be added |
| `source` | Argument | required | Name of the tool to clone |

Examples:

- `musoq tool clone weather`
- `musoq tool clone weather weather-copy`

### `musoq tool create`

Create a new tool from a template

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name for the new tool |

Examples:

- `musoq tool create my-tool`

### `musoq tool delete`

Delete an existing tool

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Skip the destructive-operation confirmation |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the tool to delete |

Examples:

- `musoq tool delete my-tool`

### `musoq tool execute`

Execute a tool with dynamic, tool-specific parameters

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--debug` | Flag | default: `false` | Enable debug output for troubleshooting |
| `--execution-details` | Flag | default: `false` | Show live query execution phases and data source progress |
| `--format` | Option | optional | Output format: json, csv, or table (default: table) Allowed: `csv`, `json`, `table`. |
| `--progress` | Flag | default: `false` | Show live query progress on stderr |
| `name` | Argument | required | Name of the tool to execute |

Examples:

- `musoq tool execute calculator a 5 b 10 operation add`
- `musoq tool execute weather -- --city London`
- `musoq tool execute weather city London`
- `musoq tool execute weather city London --debug`
- `musoq tool execute weather city London --format json`

Dynamic values are resolved at invocation time; inspect help or completion on the target machine.

### `musoq tool folder`

Show or open the tools folder

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `--open` | Flag | default: `false` | Open the folder in the system's default file explorer |

Examples:

- `musoq tool folder`
- `musoq tool folder --open`

### `musoq tool list`

List available tools

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format Allowed: `csv`, `json`, `table`, `tsv`. |
| `--search` | Option | optional | Filter tools by search term |

Examples:

- `musoq tool list`
- `musoq tool list --search weather`

### `musoq tool preview`

Show how to execute a tool with placeholder parameters

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `name` | Argument | required | Name of the tool to preview |

Examples:

- `musoq tool preview weather`

### `musoq tool rename`

Rename an existing tool

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Existing tool name to rename |
| `new-name` | Argument | required | New tool name |

Examples:

- `musoq tool rename old-name new-name`

### `musoq tool show`

Show detailed information about a specific tool

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the tool to display |

Examples:

- `musoq tool show weather`

### `musoq tool update`

Open an existing tool for editing

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `text` | Output format: text or json Allowed: `json`, `text`. |
| `name` | Argument | required | Name of the tool to update |

Examples:

- `musoq tool update my-tool`

### `musoq watch`

Run a normal Musoq query when a filesystem, interval, or query trigger fires. Interactive ANSI terminals refresh the latest successful table; redirected and machine-formatted output remains append-only. Routine Watch lifecycle telemetry is quiet unless --progress is supplied. Actions may keep durable rolling relations and compare them through watch.previous, history, and diff sources. Each trigger uses the standalone 'run' command grammar. Polling and AI-backed actions may incur cost.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--fail-fast` | Flag | default: `false` | Terminate after the first probe or action failure. |
| `--highlight-for` | Option | default: `3s` | Highlight newly published table rows for this duration; use 0ms to disable. |
| `--initial` | Flag | default: `false` | Emit one provider-specific initial event. |
| `--keep-for` | Option | default: `30d` | Maximum retained generation age, such as 12h or 30d. |
| `--keep-generations` | Option | default: `100` | Maximum retained generations for this state. |
| `--max-output-bytes` | Option | default: `1GiB` | Maximum output bytes retained for one Watch turn. |
| `--max-state-bytes` | Option | default: `10GiB` | Maximum retained bytes for this state, such as 512MiB or 10GiB. |
| `--progress` | Flag | default: `false` | Show Watch lifecycle progress on stderr; otherwise routine lifecycle telemetry is quiet. |
| `--skip-unchanged` | Flag | default: `false` | Discard a candidate without running the action when all exposed slots are unchanged. |
| `--state` | Option | optional | Durable rolling-state name; omit for isolated ephemeral rolling state. |
| `provider` | Argument | required | Watch provider name: filesystem, interval, or query. Reserved routes: list, state, replay, and attach. |

Examples:

- `musoq watch filesystem . --recursive run SELECT * FROM watch.events()`
- `musoq watch filesystem D:\incoming --recursive --state incoming-files run file-diff.sql --from-file`
- `musoq watch interval 30s run SELECT 1 FROM system.dual()`
- `musoq watch query SELECT true FROM system.dual() --every 30s run SELECT * FROM watch.events()`
- `musoq watch replay incoming-files --generation g00000000000000000042`

Dynamic values are resolved at invocation time; inspect help or completion on the target machine.

### `musoq watch attach`

Attach to committed generations of a durable rolling Watch state.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--after` | Option | optional | Start exclusively after this generation ID. |
| `--cursor-file` | Option | optional | Versioned cursor file used to resume delivery. |
| `--excel-safe` | Flag | default: `false` | Prefix formula-like CSV cells for safer spreadsheet import. |
| `--format` | Option | default: `table` | Output format: table, json, yaml, csv, or raw. Allowed: `csv`, `interpreted_json`, `interpreted_yaml`, `json`, `raw`, `reconstructed_json`, `reconstructed_yaml`, `table`, `yaml`. |
| `--from-beginning` | Flag | default: `false` | Start at the oldest retained feed-capable generation. |
| `--max-output-bytes` | Option | default: `1GiB` | Maximum output bytes retained for one attached result. |
| `--no-header` | Flag | default: `false` | Skip the header row in CSV output. |
| `--once` | Flag | default: `false` | Drain currently available generations and exit. |
| `--progress` | Flag | default: `false` | Show attach progress on stderr. |
| `--show-types` | Flag | default: `false` | Show friendly column types below table headers. |
| `--table-layout` | Option | optional | Table layout: auto, grid, records, or compact. Allowed: `auto`, `compact`, `grid`, `records`. |
| `--unquoted` | Flag | default: `false` | Disable quoting in CSV output. |
| `state` | Argument | required | Durable rolling state name. |

### `musoq watch list`

List active foreground Watch sessions without exposing action SQL, parameters, or secrets.

### `musoq watch replay`

Replay a retained rolling Watch generation without live datasources.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--format` | Option | default: `table` | Output format: table, json, yaml, csv, or raw. |
| `--generation` | Option | optional | Generation ID to replay. |
| `--max-output-bytes` | Option | default: `1GiB` | Maximum output bytes retained for one replay turn. |
| `--progress` | Flag | default: `false` | Show replay lifecycle progress on stderr. |
| `--recommit` | Flag | default: `false` | Publish the replay result as a new generation after successful output. |
| `state` | Argument | required | Durable state name. |

### `musoq watch state`

Inspect and administer durable rolling Watch state.

### `musoq watch state delete`

Delete a state or one non-head generation.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Confirm the destructive operation. |
| `--generation` | Option | optional | Delete only this non-head generation. |
| `state` | Argument | required | Durable state name. |

### `musoq watch state export`

Export one durable rolling Watch state.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--decrypt` | Flag | default: `false` | Export decrypted action, event, and neutral row data. |
| `--force` | Flag | default: `false` | Allow replacing an existing output bundle. |
| `--output` | Option | optional | Destination bundle path. |
| `state` | Argument | required | Durable state name. |

### `musoq watch state generations`

List retained generations for one state.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `state` | Argument | required | Durable state name. |

### `musoq watch state list`

List durable rolling Watch states.

### `musoq watch state reset`

Delete all history and definition metadata for one state.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `--force` | Flag | default: `false` | Confirm the destructive operation. |
| `state` | Argument | required | Durable state name. |

### `musoq watch state show`

Show one durable rolling Watch state.

| Value | Kind | Required or default | Description |
| --- | --- | --- | --- |
| `state` | Argument | required | Durable state name. |

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 69. Global syntax, help, completion, and version

_Part 8: Reference, security, and troubleshooting_

Use root help to find `describe`, then combine host-free exact discovery, the version switch, focused help, and shell completion.

## When to use this chapter

Use these interfaces before invoking an unfamiliar release or generating a shell integration.

## Working method

1. Run `musoq --version` to identify the executable.
2. Run root help and follow its `musoq describe` pointer; use focused help only when a human display of one known command is useful.
3. Generate a completion script for the exact shell and executable path without changing the profile automatically.

## Automation contract

Treat human help as display text and use `musoq describe --format json` or an exact command resource for programmatic inspection.

## Verification

Help exposes the start and machine-contract pointers, static describe remains host-free, and the completion script parses in its target shell.

## Commands used

- `musoq describe`
- `musoq completion script`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 70. Output format reference

_Part 8: Reference, security, and troubleshooting_

Apply common rules for table, JSON, YAML, CSV, TSV, raw, interpreted, and reconstructed formats across commands that support them.

## When to use this chapter

Use this matrix when choosing a stable output for a person, parser, spreadsheet, or structured reconstruction workflow.

## Working method

1. Read the command-specific allowed values because not every command supports every format.
2. Use table only as a reading interface and request a machine format for parsing.
3. Keep formatter-specific switches with their compatible format.

## Automation contract

Reject unknown formats before service communication and expect one complete parseable machine document followed by a newline.

## Verification

Parse representative null, string, numeric, collection, and empty outputs for each format used by the integration.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 71. Exit code reference

_Part 8: Reference, security, and troubleshooting_

Map process codes 0, 1, 2, 3, 4, 5, 6, and 130 to success, domain, usage, communication, missing, authorization, partial, and cancellation states.

## When to use this chapter

Use this reference in wrappers and runbooks that need deterministic control flow.

## Working method

1. Handle 0 as success only after required output validation.
2. Handle 6 as a mixed result that requires item-level reconciliation.
3. Preserve 130 as cancellation and retain stderr for the remaining failures.

## Automation contract

Never remap all nonzero codes to one generic error before the caller has a chance to apply retry, correction, or authorization policy.

## Verification

The wrapper returns or records the original code for every documented class.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 72. Configuration key reference

_Part 8: Reference, security, and troubleshooting_

Discover typed `get`, `set`, and `clear` child commands for cloud connection, identity, logging, MCP, datasource updates, and environment settings.

## When to use this chapter

Use the generated command reference rather than a copied flat key list because supported children and value types are release-specific.

## Working method

1. Inspect `get`, `set`, and `clear` focused help for the target release.
2. Use the exact child path so value conversion and validation happen before persistence.
3. Read back the effective value without exposing secrets.

## Automation contract

Do not invent configuration keys or edit internal JSON for a setting that has a supported typed command.

## Verification

Each used setting has matching generated command metadata and a successful masked read-back.

## Commands used

- `musoq get`
- `musoq set`
- `musoq clear`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 73. File and directory reference

_Part 8: Reference, security, and troubleshooting_

Locate per-user settings, variables, logs, data sources, scripts, tools, caches, databases, and rolling state without assuming one platform layout.

## When to use this chapter

Use this reference for backup, permissions, evidence, storage sizing, and profile migration.

## Working method

1. Discover paths through CLI commands and the active home configuration.
2. Resolve each path to an absolute descendant before copying or deleting files.
3. Stop the service before copying mutable state and preserve file permissions.

## Automation contract

Never use a home directory, workspace root, wildcard, or unresolved environment variable as a recursive cleanup target.

## Verification

Every filesystem operation logs an exact resolved target inside the intended profile directory.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 74. Tool REST API reference

_Part 8: Reference, security, and troubleshooting_

Use the filtered OpenAPI document for the three supported GET routes, their parameters, response modes, and error classes.

## When to use this chapter

Use `tools-openapi.json` to generate or validate an application client for local tool execution.

## Working method

1. Verify service health and version.
2. Discover tool definitions and parameter requirements.
3. Execute by encoded tool name and query arguments, selecting envelope or raw output deliberately.

## Automation contract

Generate clients only from the filtered manual OpenAPI contract; the service Swagger document can contain internal routes outside support scope.

## Verification

Contract tests compare all three paths and their parameter semantics with the executing controller.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 75. MCP protocol reference

_Part 8: Reference, security, and troubleshooting_

Use the versioned MCP contract for endpoint patterns, initialization, ping, tool list and call, and resource list and read methods.

## When to use this chapter

Use `mcp.json` when implementing or debugging a compatible client.

## Working method

1. Initialize before ordinary method calls, retain the server instructions, and send the initialized notification when required by the client flow.
2. Use `musoq_describe` for bootstrap, search, exact resources, errors, and optional live descriptions; validate its format-version 1 output schema.
3. Use context endpoints for scoped tools, read bootstrap or catalog resources including heading fragments, and handle JSON-RPC errors separately from tool `isError` results.

## Automation contract

Retain request IDs, resource URIs, and exact next-action arrays in redacted traces; never retry a side-effecting call blindly after ambiguous transport failure.

## Verification

Protocol tests cover discovery Markdown and structured parity, fragments, failures with help actions, legacy tools, unknown resources, disabled service, malformed JSON, and ping.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 76. Dynamic command and value boundaries

_Part 8: Reference, security, and troubleshooting_

Recognize command modules, Watch providers, tool parameters, installed package commands, and completion values that are resolved at runtime.

## When to use this chapter

Use runtime discovery when a command contract marks a dynamic namespace or the installed profile determines available names.

## Working method

1. Resolve prerequisites such as installed package, tool name, provider, context, or state first.
2. Use static `describe` or completion for fixed identifiers and runtime `describe schema` only when lightweight installed context is required.
3. Validate the final typed invocation before discovering a concrete source or contacting the service.

## Automation contract

Do not cache dynamic values across machines or package changes without a fingerprint and invalidation rule.

## Verification

Change the underlying tool or module catalog and confirm discovery updates while the fixed base schema remains compatible.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 77. Versions, channels, and compatibility

_Part 8: Reference, security, and troubleshooting_

Track the CLI release, manual edition, datasource packages, core runtime packages, installer channel, and extension modules as one tested environment.

## When to use this chapter

Use this inventory for reproduction, upgrades, rollback, release notes, or package compatibility analysis.

## Working method

1. Record exact semantic versions and source digests rather than labels such as newest.
2. Compare command and package contracts before changing a component independently.
3. Run representative behavior tests after every compatibility change.

## Automation contract

A channel is a selection policy, not a compatibility guarantee; exact versions and runtime evidence remain necessary.

## Verification

The environment record can reconstruct the executable, manual, packages, and representative test result.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 78. Security model

_Part 8: Reference, security, and troubleshooting_

Protect local network binding, plugin supply chain, datasource credentials, query scope, tool effects, files, logs, and AI client authority.

## When to use this chapter

Apply this review before installing code, exposing a port, processing sensitive data, or enabling unattended execution.

## Working method

1. Keep service binding local unless an explicit network boundary has been designed.
2. Verify installers and packages, minimize datasource permissions, and store secrets outside query text.
3. Constrain tools, contexts, paths, output, and destructive operations to the task scope.

## Automation contract

Stop when authorization is ambiguous, never enumerate secrets, and require confirmation for purge, bulk removal, or external side effects.

## Verification

Security tests cover loopback binding, checksum rejection, masked settings, path containment, least-privilege tool discovery, and destructive guards.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 79. Diagnostics and common failures

_Part 8: Reference, security, and troubleshooting_

Classify failures as usage, configuration, service communication, datasource loading, query compilation, runtime execution, output, or cleanup problems.

## When to use this chapter

Use this classification before applying a fix or escalating an issue.

## Working method

1. Capture command path, exact arguments with secrets removed, version, exit code, stdout, and stderr.
2. Run `musoq describe error <code>` and follow its canonical documentation and exact recovery actions before escalating to doctor, status, or expert inspection.
3. Change one variable and rerun the focused reproduction before broad validation.

## Automation contract

Preserve the diagnostic code and help URI, redact source arguments and secrets, and report causal boundaries rather than a guessed root cause.

## Verification

The reproduction fails consistently before the change and passes the same invariant after the change.

## Commands used

- `musoq describe error`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 80. Performance and resource limits

_Part 8: Reference, security, and troubleshooting_

Measure startup, query, Watch, state, recursion, ingress, output, and datasource work with executable identity and explicit bounded limits.

## When to use this chapter

Use this method before claiming a regression, changing retention, or increasing query and Watch limits.

## Working method

1. Identify the exact executable hash, commit, platform, runtime, package set, and cold or warm state.
2. Measure representative workloads with raw samples and result correctness checks.
3. Set recursion, Watch retention, polling, `--max-pending-events`, `--max-output-bytes`, and external-service limits according to measured needs.

## Automation contract

Do not trade away correctness, cancellation, or isolation for a faster number, and do not reuse measurements from a different executable; sustained producers must not defeat Watch batch deadlines.

## Verification

A performance report includes raw samples, p50 and p95 where applicable, environment identity, exact-result invariants, event-capacity observations, and output-limit behavior.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 81. Report issues and documentation drift

_Part 8: Reference, security, and troubleshooting_

Provide a minimal reproducible record and identify whether implementation, generated contract, narrative, website, embedded content, or release packaging is stale.

## When to use this chapter

Use this format for defects, support requests, and pre-release freshness failures.

## Working method

1. Record exact versions, SHAs, platform, command, expected result, actual result, exit code, and redacted streams.
2. Include the manual digest and the specific chapter or generated contract entry.
3. State the smallest reproducible mismatch and any checks already performed.

## Automation contract

Do not update prose to conceal an implementation defect or change implementation merely to match stale prose; determine the authoritative contract first.

## Verification

Another clean profile can reproduce the mismatch from the supplied evidence without access to private data.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 82. Cookbook: first query with JSON output

_Part 9: Tested cookbook_

Run a dependency-free query and produce one parseable JSON document suitable for a smoke test or automation probe.

## When to use this chapter

Use this recipe immediately after installation to verify the complete `describe` to `run` path.

## Working method

1. Read `musoq describe --format json` and confirm that it returns one bootstrap resource and a `run` next action.
2. Run `SELECT 1 AS Value FROM system.dual()` with JSON output; no mandatory preflight is required.
3. Capture stdout and stderr separately and check for exit code 0.
4. Parse stdout and assert that the first row contains numeric value 1.

## Automation contract

Validate discovery and execution as separate JSON documents; do not accept table text or diagnostics as the query result.

## Verification

The bootstrap identifies `musoq://bootstrap`, then execution returns one row whose `Value` equals 1.

## Commands used

- `musoq describe`
- `musoq run`

## Example

```powershell
$json = musoq run "SELECT 1 AS Value FROM system.dual()" --format json
if ($LASTEXITCODE -ne 0) { throw "Query failed" }
($json | ConvertFrom-Json)[0].Value
```

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 83. Cookbook: inspect and query files

_Part 9: Tested cookbook_

Discover the installed filesystem datasource surface and run a bounded file query against a selected directory.

## When to use this chapter

Use this recipe to validate a filesystem datasource without scanning an entire drive.

## Working method

1. Run `musoq describe search "query files"` and read this exact recipe plus the relevant command and language resources.
2. When the installed filesystem method or returned columns are unknown, use runtime source discovery and acknowledge its provider-controlled data access.
3. Create a small directory fixture and query only that absolute path.
4. Request JSON and assert names, sizes, and row count against the fixture.

## Automation contract

Do not guess a method signature from an older example, but skip optional runtime discovery when the installed contract is already known.

## Verification

Every returned path belongs to the fixture and the sorted result matches the known files.

## Commands used

- `musoq describe search`
- `musoq describe source`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 84. Cookbook: query piped JSON

_Part 9: Tested cookbook_

Feed a local JSON fixture through standard input and select fields without mixing the data stream with query text.

## When to use this chapter

Use this recipe to compose a JSON-producing command with Musoq in a deterministic pipeline.

## Working method

1. Create a small UTF-8 JSON fixture with known active and inactive records.
2. Pipe it to a query that uses the installed stdin JSON method and selects active rows.
3. Request JSON output and validate identifiers and row count.

## Automation contract

Use a local fixture before a network producer and keep both producer and Musoq exit codes visible.

## Verification

Only the known active records appear and malformed JSON produces a nonzero exit with stderr diagnostics.

## Commands used

- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 85. Cookbook: create and run a parameterized script

_Part 9: Tested cookbook_

Create a managed SQL script, review it, and execute it with serializer-produced runtime parameters and JSON output.

## When to use this chapter

Use this recipe for a reusable query whose values vary between runs.

## Working method

1. Create the script and edit it to declare or consume the intended runtime parameter.
2. Show the saved script and retain its reviewed content.
3. Serialize the parameter object, run with `--hint script`, and validate JSON output.

## Automation contract

Do not interpolate parameters into SQL text or build JSON by concatenating untrusted strings.

## Verification

Two different parameter sets change only the expected values while the script content digest remains constant.

## Commands used

- `musoq script create`
- `musoq script show`
- `musoq run`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 86. Cookbook: create, preview, and execute a tool

_Part 9: Tested cookbook_

Define a bounded parameterized tool, inspect its dynamic grammar, preview the prepared query, and execute it as JSON.

## When to use this chapter

Use this recipe before exposing a new tool through MCP or REST.

## Working method

1. Create a tool with a simple query and one non-sensitive typed parameter.
2. Show it, inspect focused help, and preview a representative binding.
3. Execute it through the CLI and compare the result with the previewed query.

## Automation contract

Keep the tool side-effect free for this smoke test and discover parameters rather than assuming their token order.

## Verification

The output contains the bound value, missing parameters fail clearly, and the tool definition remains unchanged.

## Commands used

- `musoq tool create`
- `musoq tool show`
- `musoq tool preview`
- `musoq tool execute`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 87. Cookbook: watch a directory with rolling state

_Part 9: Tested cookbook_

Watch a temporary directory, publish JSON events into named rolling state, inspect generations, replay one retained generation, and attach with a cursor.

## When to use this chapter

Use this recipe to validate quiet Watch output, durable state, replay, and attach recovery without relying on production files.

## Working method

1. Create an isolated directory and start a recursive filesystem Watch with `--initial` and a unique state name.
2. Create and modify fixture files, then stop the foreground process cleanly; use `--max-pending-events` and `--max-output-bytes` when exercising limits.
3. List state generations and replay a selected generation as JSON.
4. Run `watch attach --once --from-beginning` with a cursor file, restart it, and verify committed generations resume without silently skipping a retention gap.

## Automation contract

Use a unique state and fixture directory, tolerate best-effort event coalescing, validate final filesystem state as authoritative, and treat cursor delivery as at least once.

## Verification

At least one committed generation exists, replay and attach succeed without a live datasource read, cursor state advances atomically, and cleanup targets only the fixture state.

## Commands used

- `musoq watch`
- `musoq watch state generations`
- `musoq watch replay`
- `musoq watch attach`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 88. Cookbook: connect an MCP client

_Part 9: Tested cookbook_

Enable MCP, create a least-privilege context, connect locally, and follow the shared `musoq_describe` to query workflow.

## When to use this chapter

Use this recipe as an MCP integration smoke test before assigning operational tools.

## Working method

1. Enable MCP, start the service, and create a context with no tools or one harmless test tool.
2. Initialize the context endpoint, verify its discovery instructions, and list tools and resources.
3. Call `musoq_describe` with operation `bootstrap`, then read `musoq://catalog` and one smallest relevant manual or specification fragment.

## Automation contract

Verify the context tool set before calls, keep the endpoint on loopback, and use `musoq_execute_query` only when execution is required.

## Verification

Server identity is `musoq-cli`, bootstrap Markdown matches structured content, resource digests match the CLI, and no unassigned tool is visible.

## Commands used

- `musoq set mcp-enabled`
- `musoq mcp context create`
- `musoq mcp context add-tool`
- `musoq serve`

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.

---

# 89. Cookbook: call a tool through REST

_Part 9: Tested cookbook_

Verify local health, discover one tool contract, execute it through the supported GET endpoint, and validate its response mode.

## When to use this chapter

Use this recipe for a minimal application integration that does not need MCP.

## Working method

1. Call `/tools/health` on loopback and compare the returned version with the CLI release.
2. Call `/tools`, select the harmless test tool, and validate its required parameters.
3. Call `/tools/{name}` with encoded values and explicit JSON format, then repeat with raw output only if required.

## Automation contract

Do not call `/tools/management` or other internal routes; handle 400, 404, and 500 separately and avoid blind retries after uncertain effects.

## Verification

The envelope reports success and expected data, while an invalid parameter produces the documented error response.

## Scope boundary

Treat the command help and generated contracts as authoritative for syntax. Datasource-specific SQL and plugin APIs are documented by their own versioned specifications.
