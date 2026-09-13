# Recovery campaign — detailed scope map

82 new scopes. Default execution is the 64-scope engine lane. All scopes begin pending.
The previous 83-scope campaign is preserved as immutable historical evidence.

## Engine lane — 64 scopes

### REC-084 — Reconcile the 83-scope baseline and source identities

Establish a truthful starting point without altering historical completion records or assuming the CLI snapshot commit is an engine commit.

Sources: completed-diagnostic-campaign-83.json — /campaign, /scopes; 00-project-guide.md — Snapshot identity, Authority by question type; musoq-core-language-spec.md — 1.2 Scope, 23.7 v17 to v18 migration.

- **REC-084-O01**: Record engine repository root, baseline commit, branch, working-tree state, SDK, test infrastructure and instruction hierarchy.
- **REC-084-O02**: Keep the supplied completed ledger byte-for-byte unchanged; map all 83 IDs to historical commits where available and label absent records unverified.
- **REC-084-O03**: Reconcile discrepant recorded test totals against available raw results; missing historical logs remain evidence gaps, not newly fabricated test runs.
- **REC-084-O04**: Resolve target-local authoritative files and compare supplied file digests; retain both versions and a compatibility decision per source.
- **REC-084-O05**: Locate specs/diagnostic-catalog.json and docs/enums.md in the selected engine checkout; absence is a scoped authority gap, not permission to invent content.
- **REC-084-O06**: Run and retain a new full baseline gate; make future test evidence stronger even when past evidence cannot be reconstructed.

**Finish:** Every historical claim is classified verified, contradicted, or unverified with evidence. Product snapshot, engine commit and specification bundle are separate identities.
**Do not:** Do not require e255ddc98c61f63ddf176d951728b435abc8d76b to exist in the engine repository. Do not reject all progress solely because an old raw log is unavailable.

### REC-085 — Make the full test gate prove project completeness

Ensure a green gate cannot hide a missing project, stale binary, empty filter, stale result file or newly skipped test.

Sources: completed-diagnostic-campaign-83.json — /fullGate.

- **REC-085-O01**: Discover every test project and target framework in the selected solution, including examples and benchmark tests where part of that solution.
- **REC-085-O02**: Capture machine-readable results per project/target and reconcile discovered, executed, passed, failed and skipped counts.
- **REC-085-O03**: Bind fresh result files to the exact invocation, source payload, configuration, process exit and build artifacts; never reuse stale TRX as evidence.
- **REC-085-O04**: Record existing intentional skips by test identity and reason; fail on unexpected skips, aborted tests, missing projects or unexplained inventory shrinkage.
- **REC-085-O05**: Negative-test the gate with a missing project, zero-test filter, failed child process, stale log, duplicate result and modified source after build.

**Finish:** All intentionally broken gate fixtures fail for the expected reason. All tests run and pass at every scope completion; no balanced or focused-only substitute.
**Do not:** Do not count compile failures or discovery-only runs as passing tests. Do not introduce a new orchestration product or database merely to collect evidence.

### REC-086 — Create an explicit documentation-drift closure ledger

Turn historical caveats into accountable, source-specific issues rather than implicit acceptance.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-binary-text-spec.md — 4.9 Validation Constraints, 5.4.1 Until Delimiter, 6.4 Field Reference Scoping; completed-diagnostic-campaign-83.json — /scopes.

- **REC-086-O01**: Assign IDs to historical CASE metadata, missing CASE components, function/window casing and retired warning-code observations.
- **REC-086-O02**: Assign IDs to ASOF RIGHT generic diagnostics, typed-versus-dynamic coupled ASOF coverage, and legacy set-ordering objective wording.
- **REC-086-O03**: Record binary/text delimiter-consumption, forward-reference and example conflicts as hypotheses with exact source passages; verify the current checkout before asserting they persist.
- **REC-086-O04**: Give every finding an owning future scope, severity, affected version and closure evidence requirements.
- **REC-086-O05**: Inventory new same-domain conflicts encountered during source reconciliation; record rather than choosing convenient interpretations.

**Finish:** Every known caveat has an explicit disposition and owner. Inventory scope may complete with tracked open items; final release gate may not conceal them.
**Do not:** Do not edit authoritative language semantics to make implementation tests pass. A generated catalog proves synchronization, not independently correct guidance.

### REC-087 — Pre-register candidate expectations and outcome classes

Make the diagnostic oracle independent of what the current engine happens to emit.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 5.3 Column Aliasing, 2.5 String Literals.

- **REC-087-O01**: Persist a valid seed, fixture identity, user task, exact mutation, intended root cause and applicable spec citation before observing a candidate.
- **REC-087-O02**: Classify candidates as invalid, valid-but-suspicious, valid-and-ordinary or unspecified before execution; a mutation need not make a query invalid.
- **REC-087-O03**: Use root-cause/context fingerprints plus raw source hashes; different identifier spellings alone are not novel coverage.
- **REC-087-O04**: Minimize failures only while preserving the same fault and observation; retain original and minimized forms.
- **REC-087-O05**: Negative-test the harness with valid implicit aliases, allowed trailing commas, preserved unknown escapes, and an intentionally changed expected diagnostic.

**Finish:** The harness rejects post-hoc expectation replacement and invalid negative seeds. Coverage evidence records actual pre-execution intent and uncertainty.
**Do not:** Do not infer invalidity solely because parsing or execution failed.

### REC-088 — Mutation-test diagnostic assertions themselves

Prove shared assertions notice incorrect diagnostics instead of merely executing error paths.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-088-O01**: Reuse existing assertion helpers; add narrowly scoped assertions only for uncovered contract fields.
- **REC-088-O02**: Inject wrong code, phase, source kind, missing symbol argument, shifted span, fabricated zero offset and removed guidance into controlled diagnostic fixtures.
- **REC-088-O03**: Verify the intended assertion rejects each bad fixture and accepts correctly unknown runtime locations.
- **REC-088-O04**: Distinguish meaningful killed mutants, survived mutants, equivalent mutations, compilation-invalid mutations and timeouts.
- **REC-088-O05**: Retain a critical mutation-to-test map and run at least one targeted production-path mutation per diagnostic representation layer.

**Finish:** Every nominated critical non-equivalent mutation is killed by its intended assertion. Compile failures and timeouts never inflate assertion effectiveness.
**Do not:** Do not treat 100% catalog enumeration as diagnostic quality coverage.

### REC-089 — Independently verify catalog meaning and retired-code behavior

Keep exact emitted codes/templates synchronized while checking explanations against language rules independently.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.4 Exact parser diagnostics, 23.7 v17 to v18 migration.

- **REC-089-O01**: Inventory active, compatibility-only and removed codes at the resolved engine baseline.
- **REC-089-O02**: For high-risk codes author expected root-cause facts from the specification, not by calling the same descriptor factory being tested.
- **REC-089-O03**: Verify removed numeric codes are not repurposed and compatibility-only codes are explainable without being emitted.
- **REC-089-O04**: Exercise representative rendered examples and corrections rather than checking only descriptor strings.
- **REC-089-O05**: Resolve or retain explicit drift records for historical CASE descriptions and ordering/slicing compatibility identifiers.

**Finish:** An intentionally wrong explanation is caught even when catalog and registry agree. Legacy codes are not forced into artificial trigger tests.
**Do not:** Do not freeze an old enum of codes when the target contract differs.

### REC-090 — Exact quick-fix targeting in repeated and nested text

Ensure a fix modifies the erroneous occurrence and nothing else.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.3 Root-cause classification.

- **REC-090-O01**: Test the same misspelling in a string, comment, projection, nested subquery and source alias.
- **REC-090-O02**: Apply the actual emitted action rather than a test-authored substitute edit.
- **REC-090-O03**: Assert unchanged surrounding text, correctly scoped replacement and disappearance of the intended error.
- **REC-090-O04**: Test identical tokens in different scopes and multiple occurrences with only one invalid binding.

**Finish:** Every required emitted edit passes parse, binding and controlled execution when the original candidate has one fault.
**Do not:** Do not implement global string replacement as a text-edit applier.

### REC-091 — Missing-token repairs, quoting and trivia

Test insertions and identifier escaping at real syntax boundaries.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.4 Exact parser diagnostics, 2.3 Identifiers, 6.2 Table and Source Aliasing.

- **REC-091-O01**: Cover missing aliases, separators, closing delimiters, mandatory CASE components and end-of-file insertion points.
- **REC-091-O02**: Exercise adjacent comments, multiline whitespace and reserved or space-containing replacement identifiers.
- **REC-091-O03**: Reparse bracketed replacements and verify correct escaping according to the selected lexical contract.
- **REC-091-O04**: Prove recovery does not consume the next clause token while reporting an insertion.

**Finish:** Edits produce the correct grammar without deleting the next clause or altering literals.
**Do not:** Do not guess missing expressions or business predicates from syntax alone.

### REC-092 — Meaning-preserving repairs with discriminating fixtures

Reject repairs that compile but change the requested task.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 19. NULL Semantics, 12. Set Operations, 13. ORDER BY, SKIP, TAKE.

- **REC-092-O01**: For each repair-sensitive family use multiple fixtures that distinguish dropped filters, added DISTINCT, changed joins and replaced constants.
- **REC-092-O02**: Assert column names/types, duplicate multiplicity, NULL behavior, parameter binding and explicit ordering invariants.
- **REC-092-O03**: Compare expected results as bags when order is not specified, and as sequences only when the task orders them.
- **REC-092-O04**: Exercise tempting strict-to-soft conversions and Parse-to-TryParse rewrites; reject silent changes to failure policy.

**Finish:** A deliberately meaning-changing repair fails even though it compiles and runs. No single empty fixture can establish semantic preservation.
**Do not:** Do not require source-text identity with the hidden gold query; equivalent valid repairs are allowed.

### REC-093 — Ambiguity, missing information and justified abstention

Make uncertainty useful rather than converting plausible guesses into incorrect automatic fixes.

Sources: musoq-core-language-spec.md — 23.3 Root-cause classification; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-093-O01**: Construct equally plausible visible candidates, similar out-of-scope names and names from other providers.
- **REC-093-O02**: Require bounded truthful candidates and no guaranteed edit when intent is underdetermined.
- **REC-093-O03**: Distinguish a spelling mistake from missing metadata and from genuinely unsupported syntax.
- **REC-093-O04**: Verify absence-of-edit behavior for ambiguous repairs while retaining a clear explanation of what must be supplied.

**Finish:** Ambiguous cases never receive a falsely unique correction. Unique justified typo fixes remain available.
**Do not:** Do not require the engine to recover hidden business intent.

### REC-094 — Document identity and stale-edit protection

Protect consumers from applying an old diagnostic to changed text without inventing new native protocol fields.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-094-O01**: Bind harness actions to original query digest and recorded coordinate units.
- **REC-094-O02**: Alter text before the error, within it and after it; reject or reanalyze stale edits according to the consumer contract.
- **REC-094-O03**: Test unchanged text with a different wrapper document version and different text with the same length.
- **REC-094-O04**: Document whether protection belongs to the engine action, host adapter or harness; test each only at its actual boundary.

**Finish:** No stale action is silently applied to a different document. Native absence of a version field is not misreported as an existing Musoq contract violation.
**Do not:** Do not add unsupported wire fields merely to satisfy this campaign.

### REC-095 — Multiple actions, overlapping edits and repeated application

Validate action composition without assuming edits from different diagnostics are independent.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-095-O01**: Test non-overlapping independent edits, overlapping replacements and alternative mutually exclusive actions.
- **REC-095-O02**: Apply edits against a single original-document coordinate system or reanalyze after each action.
- **REC-095-O03**: Reject conflicts explicitly; do not silently select a destructive application order.
- **REC-095-O04**: Reapply an old edit after a successful correction and verify safe rejection or defined idempotence.

**Finish:** The harness catches corrupted offsets and conflicting edits.
**Do not:** Do not require native multi-edit support when only individual actions are contracted.

### REC-096 — Unicode and culture-safe correction rendering

Keep suggestions accurate across input spelling, culture and formatting variants.

Sources: musoq-core-language-spec.md — 2.1 Character Set, 2.3 Identifiers, 2.5 String Literals; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-096-O01**: Exercise supplementary characters, combining marks, case-sensitive names and near-homographs without silently normalizing identities.
- **REC-096-O02**: Run correction rendering under invariant, Polish and Turkish culture fixtures where supported.
- **REC-096-O03**: Test CRLF/LF and tabs while validating source units separately from terminal columns.
- **REC-096-O04**: Render quoted, escaped and control-containing candidates safely while preserving their structured values.

**Finish:** Identity and edit semantics are culture independent where the language requires it.
**Do not:** Do not count formatting-only variants toward novel-fault saturation.

### REC-097 — End-to-end actionable name and overload diagnostics

Check the complete error-to-correction chain for high-frequency name and signature mistakes.

Sources: musoq-core-language-spec.md — 23.3 Root-cause classification; musoq-core-language-spec.md — 6.1.1 Named source arguments, 8.10 Function Calls in Multi-Source Queries.

- **REC-097-O01**: Cover schema/source/alias/column/property/function roles with deliberate near-neighbor confusion.
- **REC-097-O02**: Verify actual/expected counts, argument types, bounded overload signatures and visibility-aware candidates.
- **REC-097-O03**: Apply uniquely justified named-argument and qualifier corrections; otherwise expose accurate alternatives.
- **REC-097-O04**: Confirm that successful repair leaves no unexpected secondary error and retains the intended owner.

**Finish:** Each callable resolution stage has a strict original-fault and repair contract.
**Do not:** Do not accept one of several unrelated codes for a single known role.

### REC-098 — Enum declaration grammar and borrowed forms

Cover the enum grammar now present in the supplied specification and verify its version alignment first.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-core-language-spec.md — 23.4 Exact parser diagnostics, 2.2 Keywords.

- **REC-098-O01**: Test missing name, colon, backing type, braces, commas, member names, equals signs and explicit values.
- **REC-098-O02**: Test empty declarations, repeated type names, trailing comma acceptance and contextual enum/flags identifiers.
- **REC-098-O03**: Exercise borrowed C#, PostgreSQL and MySQL-like forms only as explicitly invalid near-misses.
- **REC-098-O04**: Bind MQ2042-MQ2048 expectations to the current catalog and point at the exact declaration fault.

**Finish:** All contracted enum parser families have trigger and valid-neighbor protection.
**Do not:** Do not infer enum support from the old CLI snapshot label alone.

### REC-099 — Integral backing types and representability boundaries

Prove enum value errors are about representability, not merely unfamiliar values.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-core-language-spec.md — 2.6 Numeric Literals.

- **REC-099-O01**: Cover all eight allowed integral backing types at minimum, maximum, zero and adjacent out-of-range values.
- **REC-099-O02**: Test negative-to-unsigned, non-integral literals, numeric suffix mismatches and alternate-base forms according to the resolved spec.
- **REC-099-O03**: Distinguish malformed literal, invalid backing type, missing value and backing-value overflow.
- **REC-099-O04**: Keep unknown but representable runtime values valid and preserve their backing value.

**Finish:** No representable unknown value is rejected merely because it is undeclared.
**Do not:** Do not use CLR Enum.IsDefined as the entire validity oracle.

### REC-100 — Enum member casing, aliases and nominal identity

Separate type-name matching from member-name matching and value aliases.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion.

- **REC-100-O01**: Test case-insensitive local type names and exact case-sensitive member references.
- **REC-100-O02**: Reject exact/case-only duplicate member names but preserve duplicate numeric values as aliases.
- **REC-100-O03**: Verify first-declared canonical name and exact named-composite behavior.
- **REC-100-O04**: Use two enums with identical backing values to test forbidden cross-enum operations.

**Finish:** Suggestions preserve enum identity and do not select a similarly named member from another enum.
**Do not:** Do not treat nominal enum equality as plain integral equality in validation.

### REC-101 — Contextual enum binding and non-coercion

Expose useful errors for plausible enum expressions without accepting forbidden implicit conversions.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-core-language-spec.md — 19.10 NULL in Enum Intrinsics.

- **REC-101-O01**: Cover quoted contextual members in equality and membership predicates, including unknown and mis-cased members.
- **REC-101-O02**: Reject bare member names, Type.Member, numeric/string/object implicit coercions and enum casts where forbidden.
- **REC-101-O03**: Test ambiguity when a quoted literal lacks a unique enum context.
- **REC-101-O04**: Verify explicit EnumValue/EnumName guidance only when it matches the intended operation.

**Finish:** A suggested repair neither changes enum identity nor broadens operator semantics.
**Do not:** Do not assume adding quotes always repairs an enum-related expression.

### REC-102 — Enum intrinsics, flags, NULL and unknown values

Validate signatures and runtime behavior for compiler-owned enum intrinsics.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion.

- **REC-102-O01**: Exercise arity, first-argument type and literal-member restrictions for every enum intrinsic.
- **REC-102-O02**: Test flags helpers on non-flags enums, zero masks and composite masks after resolving docs/enums.md.
- **REC-102-O03**: Verify each intrinsic on NULL and unknown but representable values.
- **REC-102-O04**: Distinguish exact declared composite names from generated comma-separated text, which is not the contract.

**Finish:** Each intrinsic has successful, invalid and non-overmatching coverage.
**Do not:** Do not discover these intrinsics as if they were datasource library registrations.

### REC-103 — Native enum discovery and TABLE source planning

Validate enum descriptors at the source boundary using controlled providers.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-table-couple-spec.md — 3.7 Enum Columns, 6.5 Enum Type References, 7.3 Source Contract Diagnostics.

- **REC-103-O01**: Discover native enums only through permitted reachable types and exact fully qualified TABLE references.
- **REC-103-O02**: Verify primitive execution carriers, SourceReadType, nullable lifting and frozen descriptor identity.
- **REC-103-O03**: Reject providers unable to perform required logical scalar reads with source-contract diagnostics.
- **REC-103-O04**: Test metadata availability and modifier conflicts without inspecting object rows to invent enum maps.

**Finish:** Typed and dynamic fixtures exercise their original paths; unsupported contracts fail actionably.
**Do not:** Do not assume the SeparatedValues plugin is installed; concrete plugin tests require its verified source/artifact.

### REC-104 — Enum metadata propagation through query composition

Keep logical identity and diagnostic facts through nontrivial query rewrites.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-table-couple-spec.md — 6.5 Enum Type References; musoq-core-language-spec.md — 14. Common Table Expressions (CTEs), 12. Set Operations.

- **REC-104-O01**: Test CTEs, derived tables, projection aliases, joins, grouping and set operations with compatible enum identity.
- **REC-104-O02**: Test same-carrier/different-identity conflicts and nullable variants.
- **REC-104-O03**: Compare cold/warm compilation and reused source descriptors.
- **REC-104-O04**: Assert failures retain original SQL locations and do not become generated-code errors.

**Finish:** Representative compatible and incompatible compositions preserve the exact logical type.
**Do not:** Do not infer compatibility from identical numeric carrier types.

### REC-105 — Enum-specific mistake recovery and interpretation exclusion

Close enum recovery coverage without accidentally expanding interpretation-schema syntax.

Sources: musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-binary-text-spec.md — 12.1 Potential Extensions; musoq-table-couple-spec.md — 3.7 Enum Columns.

- **REC-105-O01**: Generate typos and missing declarations around local/native enum names and members.
- **REC-105-O02**: Verify high-confidence repairs and bounded ambiguity responses.
- **REC-105-O03**: Test direct enum annotations in binary/text fields remain unsupported in the supplied profile.
- **REC-105-O04**: Inventory docs/enums.md operator/output rules not covered by prior enum scopes and add explicit obligations.

**Finish:** Every remaining enum normative requirement is covered or blocks this scope explicitly.
**Do not:** Do not equate TABLE enum support with binary/text enum field support.

### REC-106 — Systematic omissions across core syntax

Explore missing-token families rather than another handful of keyword examples.

Sources: musoq-core-language-spec.md — 24. Formal Grammar, 23.4 Exact parser diagnostics; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-106-O01**: Create valid seeds for SELECT, FROM, predicates, JOIN/APPLY, grouping, ordering, CTEs and declarations.
- **REC-106-O02**: Delete required operands, aliases, separators and mandatory clause components one at a time.
- **REC-106-O03**: Record omission insertion points and expected boundary tokens before execution.
- **REC-106-O04**: Retain optional-token deletions as positive controls.

**Finish:** Every selected grammar boundary has a real omission case and a valid neighbor.
**Do not:** Do not classify optional syntax as required merely because a seed used it.

### REC-107 — Truncation and incomplete-query prefix coverage

Test what users and agents see while queries are only partially written.

Sources: musoq-core-language-spec.md — 24. Formal Grammar, 23.4 Exact parser diagnostics; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-107-O01**: Enumerate every meaningful token-boundary prefix of a frozen representative seed set.
- **REC-107-O02**: Distinguish complete valid prefixes from incomplete statements and unterminated lexical constructs.
- **REC-107-O03**: Test EOF locations, missing closing delimiters and compound-keyword truncation.
- **REC-107-O04**: Verify no crash, hang, manufactured binder error or discarded earlier root cause.

**Finish:** The prefix inventory and per-prefix classification are reproducible.
**Do not:** Do not require every shortened query to fail.

### REC-108 — Insertions, duplications and accidental paste fragments

Handle extra tokens and repeated constructs as specific recoverable mistakes.

Sources: musoq-core-language-spec.md — 24. Formal Grammar; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-108-O01**: Insert extra commas, closing delimiters, duplicated clauses, arguments and modifiers.
- **REC-108-O02**: Test adjacent pasted query fragments and entry-point-specific multiple-statement rules.
- **REC-108-O03**: Exercise comments and literals containing similar tokens as quiet controls.
- **REC-108-O04**: Ensure the first real duplication is identified without erasing legitimate repeated expressions.

**Finish:** Useful errors distinguish duplicate syntax from legitimate repeated projected values.
**Do not:** Do not deduplicate user semantics as an implicit repair.

### REC-109 — Misplacement, borrowed dialects and contextual boundaries

Diagnose valid constructs used in the wrong place, including retained ASOF caveats.

Sources: musoq-core-language-spec.md — 1.3 Relationship to Standard SQL, 2.2 Keywords, 8.11 ASOF JOIN, 23.4 Exact parser diagnostics.

- **REC-109-O01**: Move clauses and valid expressions into unsupported positions; test missing aliases at those boundaries.
- **REC-109-O02**: Cover foreign pagination, declarations and casts without enabling another dialect.
- **REC-109-O03**: Revisit ASOF RIGHT and TIE BREAK outside ASOF with exact user-repair evidence.
- **REC-109-O04**: Protect reserved words deliberately bracketed as identifiers.

**Finish:** A generic diagnostic is accepted only with an explicit evidence-based rationale.
**Do not:** Do not cite line-count or maintainability budgets as proof of diagnostic adequacy.

### REC-110 — Incremental renames and stale scope references

Test copied and edited queries whose names no longer agree.

Sources: musoq-core-language-spec.md — 5.3 Column Aliasing, 6.3 CTE References, 6.4 Derived Tables, 14. Common Table Expressions (CTEs); musoq-core-language-spec.md — 23.3 Root-cause classification.

- **REC-110-O01**: Rename one alias, projection, CTE output or TABLE field while leaving a single stale consumer.
- **REC-110-O02**: Cover nested blocks with shadowing and same-spelled visible/hidden candidates.
- **REC-110-O03**: Distinguish source-qualified names from exported output names.
- **REC-110-O04**: Ensure repair candidates belong to the failing scope, not another query block.

**Finish:** Exact role and visibility are preserved after correction.
**Do not:** Do not suggest internal synthesized aliases.

### REC-111 — Competing schema, source, alias and member failures

Assert the primary cause and forbidden secondary errors under overlapping name mistakes.

Sources: musoq-core-language-spec.md — 23.3 Root-cause classification; musoq-core-language-spec.md — 23.1 Diagnostic envelope contract.

- **REC-111-O01**: Combine unknown schema with unknown source; then known schema with wrong source and arguments.
- **REC-111-O02**: Test unknown alias followed by plausible property chains and columns with similar names.
- **REC-111-O03**: Test known owner with unknown column and known object with unknown property.
- **REC-111-O04**: Assert suppressed dependent diagnostics and preservation of independent sibling roots.

**Finish:** Expected code appearing somewhere in a cascade is insufficient; primary ordering and absence assertions pass.
**Do not:** Do not use blanket suppression to hide independent failures.

### REC-112 — Callable decision tree and named-argument conflicts

Freeze error precedence for name, arity, types, overload and owner.

Sources: musoq-core-language-spec.md — 23.3 Root-cause classification; musoq-core-language-spec.md — 6.1.1 Named source arguments.

- **REC-112-O01**: Combine wrong name/arity/types and check the documented resolution stage wins.
- **REC-112-O02**: Test hidden/injected parameters, missing metadata, duplicate assignments and unusable reflected defaults.
- **REC-112-O03**: Randomize provider/reflection enumeration and verify stable candidates and selection.
- **REC-112-O04**: Distinguish ambiguous overload from ambiguous owner and from named-argument metadata errors.

**Finish:** Every decision-tree edge has trigger, negative and deterministic-order evidence.
**Do not:** Do not convert all resolution failures into MQ3086 or another single fallback.

### REC-113 — Semantic mutations in grouping, windows, sets and recursion

Attack semantics without introducing an earlier syntax error that masks the target fault.

Sources: musoq-core-language-spec.md — 10. GROUP BY and Aggregation, 11. Window Functions, 12. Set Operations, 14. Common Table Expressions (CTEs); musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-113-O01**: Start from independently verified valid seeds and alter one grouped column, window placement, frame bound or set output type.
- **REC-113-O02**: Exercise recursive reference cardinality, anchor/member shape and prohibited member operations against exact current rules.
- **REC-113-O03**: Check aggregation ownership, QUALIFY visibility and result-level ordering context.
- **REC-113-O04**: Preserve type and cardinality errors through normalization instead of deferring them to generated code.

**Finish:** Each candidate reaches the intended semantic phase with no unrelated seed error.
**Do not:** Do not substitute a simplified seed that bypasses the original feature combination.

### REC-114 — Binary/text declarations versus malformed input data

Separate schema syntax mistakes from substrate parse failures.

Sources: musoq-binary-text-spec.md — 4. Binary Schema Syntax, 5. Text Schema Syntax, 9. Error Handling, 10. Grammar Specification.

- **REC-114-O01**: Delete, replace and reorder types, endianness, sizes, delimiters, modifiers and field references.
- **REC-114-O02**: Mutate valid substrate length/content independently while keeping the schema valid.
- **REC-114-O03**: Check forward references, selected branch defaults, repetition progress and bounded substreams.
- **REC-114-O04**: Keep compile-time MQ diagnostics and runtime ISE diagnostics distinct with correct domains.

**Finish:** Schema errors never pass merely because tolerant interpretation returns NULL.
**Do not:** Do not alter both schema and data in an ordinary single-fault candidate.

### REC-115 — Valid mutations and false-positive resistance

Prove the diagnostic system does not punish permitted or merely unusual input.

Sources: musoq-core-language-spec.md — 2. Lexical Elements, 5.3 Column Aliasing, 24. Formal Grammar; musoq-table-couple-spec.md — 3.2 Structure; musoq-binary-text-spec.md — 4.1 Schema Declaration.

- **REC-115-O01**: Test optional semicolons/trailing commas, implicit aliases, contextual keywords and preserved unknown ordinary escapes.
- **REC-115-O02**: Generate mutations that bind to another real column; classify changed semantics without falsely declaring invalid SQL.
- **REC-115-O03**: Apply whitespace, comment and keyword-case changes and compare semantic diagnostics after coordinate normalization.
- **REC-115-O04**: Keep identifier casing and literals out of transformations that are not semantics-preserving.

**Finish:** All positive and uncertain controls remain correctly classified.
**Do not:** Do not maximize diagnostic count as a measure of quality.

### REC-116 — Path, regex and glob warnings with quiet controls

Test lexical versus binding-dependent warning triggers without changing values.

Sources: musoq-core-language-spec.md — Advisory warning contract, 23.5 Active advisory warnings; musoq-core-language-spec.md — 2.5 String Literals, 7.6 LIKE Pattern Matching, 7.7 RLIKE (Regular Expression Matching).

- **REC-116-O01**: Cover rooted and relative path risks, raw strings, doubled escapes and deliberate standalone escapes.
- **REC-116-O02**: Test regex word-boundary backspace versus intentional character-class backspace.
- **REC-116-O03**: Test glob-like LIKE patterns versus SQL wildcards, prose and dynamic patterns.
- **REC-116-O04**: Verify warning locations, one-warning-per-literal policy and syntax-only versus bound API differences.

**Finish:** Trigger, quiet and uncertain fixtures all satisfy the exact advisory contract.
**Do not:** Do not rewrite the literal or silently repair it merely because a warning fires.

### REC-117 — NULL, outer joins and NOT IN warning precision

Make dangerous NULL-related expressions understandable without guessing row-presence intent.

Sources: musoq-core-language-spec.md — Advisory warning contract, 23.5 Active advisory warnings; musoq-core-language-spec.md — 19. NULL Semantics.

- **REC-117-O01**: Cover NULL comparisons in predicates versus permitted projection/order expressions.
- **REC-117-O02**: Distinguish nullable present values from missing outer-join rows.
- **REC-117-O03**: Test null-rejecting WHERE predicates, preserving OR branches and explicit presence checks.
- **REC-117-O04**: Exercise NULL-bearing NOT IN and positive/null-free membership controls.

**Finish:** Warnings explain alternatives without treating a meaning-changing rewrite as unconditional repair.
**Do not:** Do not move WHERE into ON automatically when intent is unknown.

### REC-118 — Conservative proofs and temporal conversion advisories

Keep mathematical proofs and date warnings within the documented certainty boundary.

Sources: musoq-core-language-spec.md — Advisory warning contract, 23.5 Active advisory warnings; musoq-core-language-spec.md — 22.2 String-to-DateTime Coercion.

- **REC-118-O01**: Test tautologies, contradictions and impossible constant conversions with deterministic direct columns.
- **REC-118-O02**: Use floating-point edge values, NULLs, method calls and dynamic values as exclusions where the contract excludes proofs.
- **REC-118-O03**: Exercise ambiguous numeric date strings against ISO, unambiguous components, month names and explicit formats.
- **REC-118-O04**: Verify a warning-only query produces exactly the same runtime values and arguments as before.

**Finish:** No uncertain proof is advertised as a fact.
**Do not:** Do not fix semantics by coercing additional types or cultures.

### REC-119 — Reachability, warning precedence and inactive codes

Test useful warnings without noise from internal or dead generated structures.

Sources: musoq-core-language-spec.md — Advisory warning contract, 23.5 Active advisory warnings; musoq-core-language-spec.md — 23.7 v17 to v18 migration.

- **REC-119-O01**: Cover live transitive CTE/let chains, dead chains, nested scopes and generated CTEs.
- **REC-119-O02**: Test unreachable CASE/coalesce paths against uncertain and NULL-sensitive alternatives.
- **REC-119-O03**: Verify a specific advisory wins over a generic proof for the same source region.
- **REC-119-O04**: Assert retired style and set-modifier compatibility warnings are not emitted.

**Finish:** Warnings remain stable and bounded across equivalent rewrites.
**Do not:** Do not report script parameters as unused let declarations.

### REC-120 — Warning-aware APIs and error-only compatibility surfaces

Check intended API differences rather than demand identical outputs everywhere.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — Advisory warning contract, 23.5 Active advisory warnings.

- **REC-120-O01**: Compare ParseResult, QueryAnalysisResult, BuildResult and inspection warnings only within their documented phases.
- **REC-120-O02**: Verify warning-aware compilation retains advisories while convenience compilation remains source compatible.
- **REC-120-O03**: Assert error-only envelope operations stay error-only and all-diagnostics operations preserve warnings.
- **REC-120-O04**: Prove warnings do not change generated execution, cache identity, source arguments or results.

**Finish:** Warning loss is caught at the actual promised boundary.
**Do not:** Do not attach a new warning API to CompiledQuery solely for test convenience.

### REC-121 — Coordinate units, Unicode, tabs and diagnostic display

Separate source coordinates from terminal display width and substrate positions.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 2.1 Character Set; musoq-binary-text-spec.md — 9.1 Parse Error Structure.

- **REC-121-O01**: Test LF, CRLF, mixed endings, no final newline and EOF insertions.
- **REC-121-O02**: Exercise supplementary characters, combining marks, wide glyphs and tabs before the error.
- **REC-121-O03**: Document exact offset/column units for each observed public surface.
- **REC-121-O04**: Check control-safe snippets independently from raw structured source locations.

**Finish:** Every coordinate resolves to the intended original input region.
**Do not:** Do not conflate UTF-16 offsets, Unicode scalar indexes, display cells and byte offsets.

### REC-122 — Original-source locations through rewriting and code generation

Preserve source provenance through optimizer and compiler transformations.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 17. Reordered Query Syntax, 5.3 Column Aliasing.

- **REC-122-O01**: Exercise FROM-first rewriting, alias substitution, star expansion, CTEs, synthetic grouping and interpretation emission.
- **REC-122-O02**: Compare locations against original text, not normalized or generated text.
- **REC-122-O03**: Attach generated-source facts and SQL related locations only when exact mappings exist.
- **REC-122-O04**: Mutation-test a wrong/clamped origin map and require the location test to fail.

**Finish:** Unknown mappings remain unknown instead of pointing to a plausible unrelated SQL token.
**Do not:** Do not satisfy source tests by widening every span to the whole query.

### REC-123 — Independent roots and poisoned dependent expressions

Keep useful secondary errors while suppressing derivative noise.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.3 Root-cause classification.

- **REC-123-O01**: Cover invalid roots in projections, aggregates, windows, casts, set outputs, recursive members and interpretation fields.
- **REC-123-O02**: Assert dependent member/cast/type errors are suppressed after a root fails.
- **REC-123-O03**: Retain independent sibling errors in stable source order.
- **REC-123-O04**: Fix one root and verify exactly the next genuine error remains.

**Finish:** Both over-reporting and over-suppression are detected.
**Do not:** Do not use an assertion that accepts the intended diagnostic anywhere in an unbounded cascade.

### REC-124 — Editing trajectories and successive corrections

Test diagnostic evolution over realistic sequences rather than isolated strings.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 24. Formal Grammar.

- **REC-124-O01**: Replay valid-to-incomplete-to-fixed sequences, then introduce a different fault.
- **REC-124-O02**: Rename aliases and CTE outputs one reference at a time.
- **REC-124-O03**: Verify stale diagnostics disappear and retained ones shift to current coordinates.
- **REC-124-O04**: Use repeated full analysis if no incremental API exists; distinguish that evidence from editor integration.

**Finish:** The frozen sequence has correct expected outcomes at every step.
**Do not:** Do not invent an LSP/editor protocol that is not part of the selected target.

### REC-125 — Diagnostic state reuse and deterministic ordering

Find residual error bags, counters and stale metadata across supported reusable objects.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-125-O01**: Run valid-invalid-valid and invalid-A-invalid-B sequences on supported reusable analyzers and contexts.
- **REC-125-O02**: Use different schema fixtures with equal query text and deterministic candidate ordering.
- **REC-125-O03**: Test diagnostic-count limits without suppressing the earliest useful root.
- **REC-125-O04**: Verify phase/source/related-location payloads survive copying and formatting.

**Finish:** Repeated observations are identical modulo documented per-run values such as correlation IDs.
**Do not:** Do not share objects concurrently when the API explicitly does not support that use.

### REC-126 — Unknown locations, source domains and provider-origin spans

Protect the distinction between a known insertion and an unknown/non-query location.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; musoq-table-couple-spec.md — 7.3 Source Contract Diagnostics.

- **REC-126-O01**: Test null location versus offset zero and zero-length insertion.
- **REC-126-O02**: Cover Query, Schema, DataSource, Runtime, GeneratedSource and Internal origins.
- **REC-126-O03**: Retain table-origin related locations for provider failures when exact information exists.
- **REC-126-O04**: Record and resolve source conflicts about empty-span fallback rather than manufacture compatibility.

**Finish:** Known query locations are precise; unavailable locations stay explicitly unknown.
**Do not:** Do not impose SourceKind=Runtime on every execution failure when its actual boundary is DataSource or Internal.

### REC-127 — Provider metadata, planning and open failures

Exercise the first failing lifecycle boundary using fault-injecting repository fixtures.

Sources: musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; musoq-table-couple-spec.md — 7.3 Source Contract Diagnostics.

- **REC-127-O01**: Fail controlled providers in metadata description, planning, construction and open separately.
- **REC-127-O02**: Preserve source/schema/alias/context/operation facts without argument values.
- **REC-127-O03**: Test successful metadata followed by failed open and missing required runtime parameter before any open.
- **REC-127-O04**: Verify a provider error is not reported as invalid SQL syntax.

**Finish:** The diagnostic names the actual operation and original safe cause.
**Do not:** Do not assume metadata discovery is side-effect-free.

### REC-128 — Deferred reads, disposal and cancellation precedence

Keep causal failures truthful when iteration and cleanup both fail.

Sources: musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures.

- **REC-128-O01**: Inject first-read, later-read, deferred-property and disposal failures.
- **REC-128-O02**: Combine a read failure with a disposal failure and retain the primary cause plus safe related information.
- **REC-128-O03**: Cancel during open/read/cleanup and preserve cancellation instead of translating it to generic error.
- **REC-128-O04**: Verify enumerators and resources are disposed without duplicate cleanup or fabricated successful completion.

**Finish:** Lifecycle failure combinations have deterministic contract-backed precedence.
**Do not:** Do not let a cleanup exception erase cancellation or an earlier root failure.

### REC-129 — User-owned runtime faults versus internal invariants

Revisit runtime classification at known weak points without silently changing public meanings.

Sources: musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; musoq-core-language-spec.md — 7.7 RLIKE (Regular Expression Matching), 3.7 Strict Postfix Casts, 7.11 Scalar Subqueries.

- **REC-129-O01**: Reproduce recorded dynamic-invalid-regex and scalar-cardinality failures on the target baseline.
- **REC-129-O02**: Compare their catalog classification with the runtime/internal source contract; record contradictions before changes.
- **REC-129-O03**: Cover data-dependent strict conversion and arithmetic failures separately from impossible constant expressions.
- **REC-129-O04**: Ensure unexpected CLR engine failures remain safe internal errors rather than borrowed syntax diagnostics.

**Finish:** Every reclassification has versioned authority and regression evidence; unresolved conflict blocks completion.
**Do not:** Do not infer a new public error code solely from an exception type.

### REC-130 — Nested interpretation error paths and consumed positions

Make malformed binary/text records diagnosable at the exact nested failure.

Sources: musoq-binary-text-spec.md — 7.6 Offset-Based Interpretation, 7.8 Partial Interpretation (Debugging), 9.1 Parse Error Structure, 4.13 Substream (Length-Bounded) Payloads.

- **REC-130-O01**: Combine InterpretAt with nested arrays, substreams, multibyte decoded text and absolute seeks.
- **REC-130-O02**: Verify schema path, field, expected/actual facts, input units and consumed-prefix semantics.
- **REC-130-O03**: Test optional rollback and zero/partial progress before failure.
- **REC-130-O04**: Use two surrounding valid records so the failing record identity is not inferred from incidental order.

**Finish:** Reported offsets and parsed prefixes resolve to the same original substrate record.
**Do not:** Do not dump entire sensitive records into the diagnostic.

### REC-131 — Strict, tolerant and partial interpretation failure policy

Ensure tolerant parsing handles parse failures without swallowing unrelated failures.

Sources: musoq-binary-text-spec.md — 7.5 Safe Interpretation, 7.8 Partial Interpretation (Debugging), 9.3 Error Behavior, 9.4 Partial Results (Debugging), D.1 Behavior Summary; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures.

- **REC-131-O01**: Use one malformed row among valid data and compare Parse/Interpret, Try variants and Partial variants.
- **REC-131-O02**: Assert CROSS drops a NULL result and OUTER preserves the null-extended row.
- **REC-131-O03**: Keep invalid schema compilation, provider failures, cancellation and engine invariants distinct from ordinary substrate mismatch.
- **REC-131-O04**: Verify partial field dictionaries and error positions are truthful on success and failure.

**Finish:** No automatic correction changes strictness or row-retention policy silently.
**Do not:** Do not catch every exception in TryParse/TryInterpret.

### REC-132 — Typed and dynamic source-shape compatibility regressions

Return to the original source shape when historical tests used an easier replacement.

Sources: musoq-table-couple-spec.md — 10.2 With JOINs, 7.4 Runtime Adapter Diagnostics [Informative]; musoq-core-language-spec.md — 8.11 ASOF JOIN, 6.5 Inline VALUES Row Sources.

- **REC-132-O01**: Reproduce typed and dynamic coupled ASOF forms separately using controlled fixtures.
- **REC-132-O02**: Verify modifier and logical-type metadata across source planning, materialization and generated execution.
- **REC-132-O03**: Treat unsupported provider contracts as explicit source compatibility failures, not SQL typos.
- **REC-132-O04**: Record each original-versus-substitute test path and close the historical caveat only with matching evidence.

**Finish:** A typed passing test cannot be used as proof of dynamic-path support.
**Do not:** Do not change the fixture shape to avoid an unresolved production problem.

### REC-133 — Cancellation and bounded zero-progress behavior

Guarantee bounded failure investigation without conflating test timeouts with proper diagnostics.

Sources: musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; musoq-binary-text-spec.md — 4.10 Repetition Until Condition, 9.2 Error Categories; manual.md — 80. Performance and resource limits.

- **REC-133-O01**: Exercise cancelled compilation/execution where supported and zero-progress interpreter loops.
- **REC-133-O02**: Use explicit harness time/memory budgets and controllable providers rather than fragile stopwatch assertions.
- **REC-133-O03**: Test interruption after partial work and required cleanup.
- **REC-133-O04**: Classify harness timeouts distinctly and retain a minimized reproduction; do not mark them diagnostic passes.

**Finish:** All bounded scenarios terminate by the contracted error or cancellation path.
**Do not:** Do not invent Musoq settings for limits that exist only in the test harness.

### REC-134 — Secret-safe diagnostics across all engine sinks

Test actual leakage using unique synthetic secrets instead of only checking one message field.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; manual.md — 66. Apply safe AI and automation execution, 78. Security model.

- **REC-134-O01**: Inject fake secret sentinels into parameters, settings, provider exceptions, inner exceptions and substrate data.
- **REC-134-O02**: Inspect messages, arguments, snippets, related locations, actions, serialized envelopes and logs.
- **REC-134-O03**: Separate public-safe formatting from explicit trusted verbose policy and label retained sensitive artifacts.
- **REC-134-O04**: Test a literal selected by a diagnostic span so snippet generation cannot bypass the secret policy.

**Finish:** No sentinel appears in a sink whose contract promises redaction.
**Do not:** Do not use real credentials or persist complete process environments.

### REC-135 — Adversarial metadata and safe diagnostic rendering

Keep provider-controlled names and descriptions as data in every representation.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 21. Discover installed capabilities and source shape, 66. Apply safe AI and automation execution, 78. Security model.

- **REC-135-O01**: Inject terminal escapes, newlines, bidi controls, long strings and instruction-like metadata.
- **REC-135-O02**: Verify machine JSON remains valid and terminal/log rendering cannot forge trusted status lines.
- **REC-135-O03**: Verify suggested identifiers are safely quoted and visibility bounded.
- **REC-135-O04**: Create fixtures for later live-agent tests but label renderer-only checks as non-agent evidence.

**Finish:** Untrusted content never becomes a shell command or authority override.
**Do not:** Do not claim prompt-injection resistance of an AI agent from serialization tests alone.

### REC-136 — Supported concurrent-session diagnostic isolation

Separate legitimate API concurrency guarantees from test-only shared state assumptions.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; completed-diagnostic-campaign-83.json — /scopes.

- **REC-136-O01**: Inventory documented supported concurrency boundaries before choosing reusable objects.
- **REC-136-O02**: Run independent sessions with identical query text and different schemas/settings/parameters.
- **REC-136-O03**: Verify no cross-session candidates, locations, enum descriptors or correlation IDs leak.
- **REC-136-O04**: Revisit the historical non-parallel star-modifier fixture and distinguish harness global state from production contract.

**Finish:** Supported independent sessions remain isolated with deterministic assertions.
**Do not:** Do not claim unsafe shared-object use is a product defect without a supported concurrency contract.

### REC-137 — Cold/warm cache and schema-change diagnostics

Make warm execution just as truthful as first compilation.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 3.8 Enum Types; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures.

- **REC-137-O01**: Compare cold/warm outcomes with same text under different schema signatures and enum descriptors.
- **REC-137-O02**: Test invalid-to-valid-to-invalid sequences and changed settings/source metadata.
- **REC-137-O03**: Verify stale compiled artifacts have a compatibility/recompile outcome, not a fabricated syntax error.
- **REC-137-O04**: Bind every observation to actual engine/source/fixture identities.

**Finish:** Cached diagnostics never leak old source facts into a new context.
**Do not:** Do not purge global user caches; use owned temporary fixture storage.

### REC-138 — Adversarial size, depth and diagnostic-volume bounds

Find denial-of-service-like diagnostic paths using bounded local tests.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 80. Performance and resource limits; musoq-binary-text-spec.md — 4.10 Repetition Until Condition.

- **REC-138-O01**: Vary nesting depth, long malformed literals, large candidate sets and many repeated errors within declared harness limits.
- **REC-138-O02**: Test descriptor rendering and suggestion ranking with thousands of near-matching fixture names.
- **REC-138-O03**: Assert bounded output and graceful cancellation/error rather than stack overflow or runaway allocation.
- **REC-138-O04**: Persist limits, workload size and observed outcome; do not equate truncation with complete diagnostic coverage.

**Finish:** Limits are reproducible and results do not depend on undeclared global settings.
**Do not:** Do not run unbounded fuzzing, memory exhaustion or external provider scans.

### REC-139 — Generated-code and incompatible-artifact recovery

Preserve native compiler facts and truthful recompile guidance at engine-owned boundaries.

Sources: musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-139-O01**: Inject generated-code compilation failure with and without an exact original-source map.
- **REC-139-O02**: Test incompatible cached artifact contracts and recompile guidance.
- **REC-139-O03**: Verify internal failures have safe correlation information without exposing native stack/source by default.
- **REC-139-O04**: Confirm an invalid user query is rejected earlier when its cause is already known.

**Finish:** Generated-source and runtime artifact failures are never relabeled as unsupported SQL for convenience.
**Do not:** Do not clamp generated locations to SQL length.

### REC-140 — Resolve delimiter and forward-reference documentation conflicts

Close concrete source conflicts with an explicit specification decision and executable evidence.

Sources: musoq-binary-text-spec.md — 4.9 Validation Constraints, 5.4.1 Until Delimiter, 6.4 Field Reference Scoping, 8.1 Schema References.

- **REC-140-O01**: Verify each suspected conflict against target and supplied file hashes before changing anything.
- **REC-140-O02**: Reproduce until-consumes-delimiter examples that subsequently expect the same delimiter.
- **REC-140-O03**: Reproduce forward-reference examples in CHECK and schema declarations.
- **REC-140-O04**: Record normative-versus-example conflict and obtain/retain approved resolution; then add corrected positive or intentionally negative fixtures.
- **REC-140-O05**: Keep original excerpts and resolution IDs for traceability.

**Finish:** No conflicting example is silently blessed because the current implementation accepts it.
**Do not:** Do not invent a precedence rule among conflicting same-domain sources.

### REC-141 — Executable examples and recovery-text examples

Make published examples a tested part of the user recovery surface.

Sources: musoq-core-language-spec.md — 25. Appendices; musoq-binary-text-spec.md — 11. Examples; musoq-table-couple-spec.md — 9. Examples; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-141-O01**: Classify examples as runnable positives, intentional negatives, fragments or future syntax.
- **REC-141-O02**: Adapt external providers only through explicit fixture mappings preserving the language claim.
- **REC-141-O03**: Execute positive examples and their stated outputs; assert negative diagnostics and the prescribed correction.
- **REC-141-O04**: Check metadata examples against non-reused independent semantics and retain missing-provider boundaries.

**Finish:** Documentation test evidence distinguishes fixture adaptation from actual installed-provider execution.
**Do not:** Do not execute arbitrary commands extracted from prose or untrusted metadata.

### REC-142 — Recovery guidance when environment facts are missing

Require useful next steps without guessing packages, profiles or capabilities.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-table-couple-spec.md — 7.4 Runtime Adapter Diagnostics [Informative]; manual.md — 21. Discover installed capabilities and source shape, 79. Diagnostics and common failures, 81. Report issues and documentation drift.

- **REC-142-O01**: Test unavailable source metadata, missing runtime settings and provider/runtime compatibility failures.
- **REC-142-O02**: Check guidance names the missing observation and separates syntax, configuration and compatibility.
- **REC-142-O03**: Validate any CLI action against commands.json when it is emitted; do not invent undocumented routes.
- **REC-142-O04**: Keep destructive reset/reinstall/force suggestions distinct from safe read-only inspection.

**Finish:** The correct response may request information; it must not falsely promise a unique repair.
**Do not:** Do not execute destructive recovery actions against the user profile.

### REC-143 — Task-invariant corpus and leakage-resistant split tooling

Prepare measurable recovery tasks without making a benchmark depend on matching one gold SQL spelling.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 3.8 Enum Types, 18.4 Enum Intrinsics, 19.10 NULL in Enum Intrinsics, 22.5 Enum Non-Coercion; musoq-binary-text-spec.md — 9. Error Handling.

- **REC-143-O01**: Build task fixtures spanning lexical, omissions, names, types, grouping/windows, enum, TABLE/COUPLE and binary/text families.
- **REC-143-O02**: Define expected result shape, multiplicity, ordering, NULL/failure policy and acceptable clarification outcomes.
- **REC-143-O03**: Split by seed/semantic family and provider fixture identity, not superficial token spelling.
- **REC-143-O04**: Store private judge data separately from repair payloads and mark development versus held-out data explicitly.
- **REC-143-O05**: Reserve final holdout creation until the compared builds and repair prompts are frozen.

**Finish:** A compile-only or empty-result repair does not pass a task invariant.
**Do not:** Do not call cases held-out after their answers have been exposed to the repairer.

### REC-144 — Blind-repair harness and access-boundary tests

Build the mechanism for a fresh repairer, without claiming that a normal subagent is automatically blind.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 66. Apply safe AI and automation execution.

- **REC-144-O01**: Expose only task, malformed query, delivered diagnostic and allowlisted bounded discovery to the repair actor.
- **REC-144-O02**: Deny filesystem/repository/history/gold-answer access at the tool boundary; a different working directory alone is insufficient.
- **REC-144-O03**: Use fake repair actors to test denied access, budgets, malformed answers, unsafe actions and semantic judge failures.
- **REC-144-O04**: Record exact actor identity, instructions, capabilities and observations for future live runs.
- **REC-144-O05**: Mark all construction tests simulated; no human or live-model score may be fabricated.

**Finish:** Contaminated runs are detected and excluded with their attempted access retained.
**Do not:** Do not pass parent reasoning, expected mutations or minimized regression answers into the repair context.

### REC-145 — Final targeted production diagnostic mutations

Challenge the improved tests after all earlier production changes.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; musoq-core-language-spec.md — 23.3 Root-cause classification; musoq-core-language-spec.md — 23.6 Schema, datasource, runtime, and internal failures.

- **REC-145-O01**: Re-run critical wrong-code, span, argument, action, cancellation and leakage mutations on the final code paths.
- **REC-145-O02**: Run each mutant in an isolated temporary copy or controlled patch with restoration verified.
- **REC-145-O03**: Require the nominated test assertion to fail; unrelated infrastructure failure is not a kill.
- **REC-145-O04**: Add independent challengers for new enum and warning paths.
- **REC-145-O05**: Return to a clean unmutated build and pass all tests before completion.

**Finish:** No nominated critical non-equivalent survivor remains unexplained.
**Do not:** Do not commit intentionally broken production mutants.

### REC-146 — Whole-corpus replay and historical caveat closure

Re-audit after rework rather than rely on the original 73-scope audit.

Sources: completed-diagnostic-campaign-83.json — /scopes; musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-146-O01**: Replay the retained 83-scope regressions plus all new completed engine evidence.
- **REC-146-O02**: Reconcile original-source shape substitutions, test inventories and outstanding drift/finding IDs.
- **REC-146-O03**: Verify every retained failing case has a permanent strict regression and every justified edit its repair evidence.
- **REC-146-O04**: Check active/legacy diagnostic and per-family coverage counts match artifacts.
- **REC-146-O05**: Leave missing historical evidence labeled unverified; do not rewrite old completion facts.

**Finish:** There is no unresolved critical/major diagnostic defect in the selected engine lane.
**Do not:** Do not interpret historical unverified test logs as a new passing run.

### REC-147 — Freeze and certify the engine lane only

Produce a truthful engine-lane conclusion and immutable comparison baseline for later package and user studies.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; completed-diagnostic-campaign-83.json — /fullGate.

- **REC-147-O01**: Verify all REC-084..REC-146 completion commits, reports, evidence hashes and dependencies.
- **REC-147-O02**: Run the complete fresh restore/build/test gate and reconcile every project/target result.
- **REC-147-O03**: Validate the tested source payload and this scope completion change before committing.
- **REC-147-O04**: Record engine artifact hashes and final corpus/prompt digests for packaged and blind-repair studies.
- **REC-147-O05**: Report remaining product and empirical lanes explicitly as not run, never as passed.

**Finish:** All 64 engine scopes have one completion commit and genuine complete test evidence.
**Do not:** Do not claim human repairability or packaged CLI/MCP conformance from engine tests.

## Product lane — 12 scopes

### REC-148 — Bind the real CLI/service build to the frozen engine

Verify the actual package under test rather than a mock or an old service process.

Sources: 00-project-guide.md — Snapshot identity; manual.md — 57. Read status, version, port, and startup metrics, 77. Versions, channels, and compatibility.

- **REC-148-O01**: Resolve explicit host repository/artifact, isolated profile and package dependency graph.
- **REC-148-O02**: Prove the tested CLI/service loads the engine fixed by the engine lane; record binary/package hashes.
- **REC-148-O03**: Observe process/service identity, owned profile and test endpoints rather than assuming a port.
- **REC-148-O04**: Bind host verification commands and run full engine plus relevant host solution gates.

**Finish:** Unavailable host/artifact means blocked product scope, not mock success.
**Do not:** Do not modify another repository without explicit authorized binding.

### REC-149 — CLI grammar and JSON usage-failure recovery

Test actual command parsing and exact help actions.

Sources: commands.json — /FormatVersion, /Commands; describe-response.v1.schema.json — /required, /$defs/action; manual.md — 8. Discover Musoq with describe, 69. Global syntax, help, completion, and version.

- **REC-149-O01**: Generate unknown options, missing values, duplicate flags, wrong enum values and misplaced positional arguments from the bound command contract.
- **REC-149-O02**: Verify describe --format json usage failures yield one schema-valid stdout document.
- **REC-149-O03**: Check non-JSON mode, exit status and help action arguments separately.
- **REC-149-O04**: Use allowed non-destructive commands and isolated fixtures only.

**Finish:** A matching in-process parser test cannot substitute for real executable evidence.
**Do not:** Do not reconstruct returned argv arrays through shell concatenation.

### REC-150 — Shell, file, stdin and encoding boundaries

Differentiate caller transformations from SQL errors.

Sources: manual.md — 9. Choose a query input form, 11. Read SQL from files, 13. Read standard input; commands.json — /Commands.

- **REC-150-O01**: Run one fixture through argument arrays, supported available shells and UTF-8 files with explicit input forms.
- **REC-150-O02**: Cover quotes, dollars, slashes, braces, newlines, Unicode, empty strings and leading hyphens.
- **REC-150-O03**: Keep stdin data separate from query text; test empty/malformed data and broken producers.
- **REC-150-O04**: Record delivered engine text and classify wrapper failures at the first true boundary.

**Finish:** Unavailable platform lanes stay explicitly unverified; claimed platform results require actual execution.
**Do not:** Do not report shell-transformed input as the user having typed different SQL.

### REC-151 — Parameter JSON and settings-profile input failures

Validate supplied data shapes before remote or provider work begins.

Sources: manual.md — 14. Use parameters and settings profiles; commands.json — /Commands; musoq-core-language-spec.md — 4.3 Script Parameters.

- **REC-151-O01**: Test malformed JSON, wrong top-level shape, duplicate/unknown keys and scalar/collection type errors according to the host contract.
- **REC-151-O02**: Distinguish script parameters from settings profiles and compile-time let variables.
- **REC-151-O03**: Prove invalid required values prevent source opening with a controlled provider.
- **REC-151-O04**: Retain redacted input and exact exit/diagnostic facts.

**Finish:** Provider fixture counters show no forbidden opening/read on rejected input.
**Do not:** Do not place real secrets in argv or logs.

### REC-152 — Streams, formats, exits and partial-output honesty

Prevent a well-formed diagnostic from corrupting result data or hiding an incomplete result.

Sources: manual.md — 15. Choose output formats, 17. Separate stdout, stderr, and progress, 18. Use exit codes in automation, 71. Exit code reference.

- **REC-152-O01**: Exercise success, usage, compilation, runtime, format failure, cancellation and partial-result exits.
- **REC-152-O02**: Capture stdout/stderr separately with progress, quiet and redirected modes.
- **REC-152-O03**: Test errors after some rows and broken output consumers under the actual normal-run contract.
- **REC-152-O04**: Compare machine and human formatting without imposing Watch transactional rules on ordinary execution.

**Finish:** Every emitted result is labeled and interpreted according to the actual interface contract.
**Do not:** Do not treat an empty result or parseable prefix as proof of success.

### REC-153 — Describe schemas, provenance and bounded next actions

Prove discovery delivers usable, trustworthy recovery metadata.

Sources: describe-response.v1.schema.json — /required, /$defs/resource, /$defs/diagnostic, /$defs/action; manual.md — 8. Discover Musoq with describe, 21. Discover installed capabilities and source shape.

- **REC-153-O01**: Validate real bootstrap/search/resource/observation envelopes against the supplied schema after version binding.
- **REC-153-O02**: Check canonical URIs, resource digests, authority/provenance and missing-resource behavior.
- **REC-153-O03**: Test bounded candidates, provider-controlled access disclosure and malicious metadata escaping.
- **REC-153-O04**: Follow safe next actions in the isolated profile and verify they answer the missing observation.

**Finish:** Engine error envelopes are not substituted for describe-response.v1.
**Do not:** Do not assume provider discovery is side-effect-free or scan-bounded.

### REC-154 — Error help and packaged catalog synchronization

Make actual error codes lead to current working recovery guidance.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 79. Diagnostics and common failures, 81. Report issues and documentation drift; commands.json — /Commands.

- **REC-154-O01**: Resolve help for representative active and compatibility codes through supported describe error/resource paths.
- **REC-154-O02**: Compare packaged explanations and examples to the loaded engine version and embedded catalog digest.
- **REC-154-O03**: Apply offered safe corrections to the original fixtures.
- **REC-154-O04**: Detect stale website/embedded/manual/generated resource divergence without silently changing authority.

**Finish:** Each selected delivered help path is executed, not merely a syntactically valid URI.
**Do not:** Do not infer that e255... denotes the engine commit.

### REC-155 — MCP protocol errors versus tool failures

Test the actual transport and context behavior promised by the versioned MCP contract.

Sources: mcp.json — /methods, /toolFailures, /notes; describe-response.v1.schema.json — /required; manual.md — 63. Connect an MCP client, 75. MCP protocol reference.

- **REC-155-O01**: Exercise initialization, malformed calls, unknown tools/resources, disabled configuration and context-specific availability.
- **REC-155-O02**: Verify domain tool failures remain successful JSON-RPC results with isError true and correct recovery fields.
- **REC-155-O03**: Check content and structuredContent semantic agreement without losing structured facts.
- **REC-155-O04**: Follow bounded read-only recovery actions and verify denied/unassigned capabilities are not suggested as available.

**Finish:** Real transport evidence is retained independently from in-memory handler tests.
**Do not:** Do not treat context names as authentication or network isolation.

### REC-156 — Supported tool REST failure and output modes

Validate only the documented tool REST surface.

Sources: tools-openapi.json — /paths, /components/schemas/ToolInfo; manual.md — 65. Use the supported tool REST endpoints, 74. Tool REST API reference.

- **REC-156-O01**: Test GET /tools/health, GET /tools and GET /tools/{name} on an owned service.
- **REC-156-O02**: Cover invalid names, unknown tools, invalid parameters/formats, preparation and execution errors.
- **REC-156-O03**: Distinguish raw=false envelope from raw=true formatter output and declared content types.
- **REC-156-O04**: Do not assert undocumented exact error fields; record contract gaps explicitly.

**Finish:** Status codes and output modes agree with the bound OpenAPI contract.
**Do not:** Do not turn undocumented internal service routes into supported API tests.

### REC-157 — Service, package and configuration recovery boundaries

Keep environment failures distinct from language failures.

Sources: manual.md — 41. Troubleshoot datasource loading, 54. Understand service lifecycle and auto-start, 57. Read status, version, port, and startup metrics, 78. Security model.

- **REC-157-O01**: Test absent/mismatched service, incompatible plugin dependency closure and controlled configuration gaps.
- **REC-157-O02**: Prove diagnostics identify load, metadata, compile or execution boundary accurately.
- **REC-157-O03**: Use only owned temporary configuration and explicitly bound packages.
- **REC-157-O04**: Verify recovery suggestions do not grant broader network or filesystem authority.

**Finish:** No environment failure is rebranded as a user SQL mistake.
**Do not:** Do not auto-install, globally upgrade, expose non-loopback ports or purge user state.

### REC-158 — Watch failure publication and safe recovery

Check truthful error handling across failed turns and durable publication boundaries.

Sources: manual.md — 27. Configure Watch actions and failures, 28. Use rolling state, history, and diff, 30. Design Watch automation and recovery; commands.json — /Commands.

- **REC-158-O01**: Use owned temporary state and controlled triggers to test failed, cancelled, formatter-failed and output-limit turns.
- **REC-158-O02**: Check failed turns publish no successful stdout/refresh sequence under the Watch contract.
- **REC-158-O03**: Inject post-publication delivery and cursor-write failures and verify recoverable at-least-once behavior.
- **REC-158-O04**: Validate reset/pruned cursor guidance without executing destructive actions outside fixtures.

**Finish:** Published state is never claimed uncommitted after a delivery failure.
**Do not:** Do not describe Watch as exactly-once or normal query output as automatically transactional.

### REC-159 — Final packaged recovery parity gate

Certify actual supported interfaces against one frozen engine/host artifact set.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; mcp.json — /toolFailures; tools-openapi.json — /paths; describe-response.v1.schema.json — /required.

- **REC-159-O01**: Replay representative engine, CLI, discovery, MCP and REST failures with exact documented per-surface expectations.
- **REC-159-O02**: Run full engine and host test gates after the final code changes.
- **REC-159-O03**: Verify all REC-148..REC-158 commit mappings, artifact hashes and identity bindings.
- **REC-159-O04**: Record missing environments as explicit unmet evidence, not a generic passed integration status.

**Finish:** Product lane passes only with real bound-package observations for every required interface.
**Do not:** Do not demand identical envelope shapes from different public interfaces.

## Empirical lane — 6 scopes

### REC-160 — Pre-register and freeze independent recovery evaluation

Create a leakage-resistant live study with declared sample, actor and resource limits.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 66. Apply safe AI and automation execution.

- **REC-160-O01**: Freeze baseline/candidate engine and package builds, repairer instructions, model identifier and tool allowlist.
- **REC-160-O02**: Have an independent case author create held-out families after implementation freeze; disclose no gold queries to repairers.
- **REC-160-O03**: Use at least 60 unique held-out tasks across at least eight mistake families, including ambiguous and valid-no-change controls.
- **REC-160-O04**: Pre-register matched diagnostic conditions, randomization, turn/tool budgets, output judging and exclusion rules.
- **REC-160-O05**: Keep human-study setup separate from simulated participants and record required permissions/cost limits.

**Finish:** No outcome-dependent threshold or sample exclusion can be changed after seeing results.
**Do not:** Do not count a ordinary forked subagent with parent context as a blinded subject.

### REC-161 — Blind first-attempt repair with diagnostic ablations

Measure actual repair benefit using only what an ordinary consumer receives.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract.

- **REC-161-O01**: Run the frozen tasks with message-only, structured-diagnostic and structured-plus-discovery conditions.
- **REC-161-O02**: Use fresh verified-isolated contexts and pair conditions on the same task families.
- **REC-161-O03**: Judge task meaning, output and safe clarification; compilation alone is insufficient.
- **REC-161-O04**: Record all successes, wrong-result repairs, unsafe edits, invalid responses and access-boundary violations.
- **REC-161-O05**: Compare baseline/candidate according to the preregistered design; negative results remain valid measurements, not hidden reruns.

**Finish:** Report first-attempt rates and denominators per family, condition and build.
**Do not:** Do not label fake repair actor tests as live model evidence.

### REC-162 — Bounded multi-turn repair and stopping behavior

Measure recovery over a realistic short conversation without unlimited retries.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 66. Apply safe AI and automation execution.

- **REC-162-O01**: Allow at most three repair turns per task and retain every intervening query and diagnostic.
- **REC-162-O02**: Count permitted discovery calls, failed attempts, unnecessary changes and unresolved cases.
- **REC-162-O03**: Judge meaning-preserving final success and refusal to repeat unsafe operations.
- **REC-162-O04**: Test that a solved query is not needlessly edited again.

**Finish:** Rates are success-within-budget, not eventual success after cherry-picked retries.
**Do not:** Do not tune the repair prompt on the final holdout and keep calling it held-out.

### REC-163 — Calibrated uncertainty and adversarial recovery behavior

Test whether a fresh agent requests missing information rather than inventing repairs or obeying metadata.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 21. Discover installed capabilities and source shape, 66. Apply safe AI and automation execution, 78. Security model.

- **REC-163-O01**: Use ambiguous names, absent provider metadata, valid unusual queries and missing external intent.
- **REC-163-O02**: Insert adversarial provider/document text into the observation channel.
- **REC-163-O03**: Score useful clarification, correct no-change decisions, authorized next actions and false-certainty repairs separately.
- **REC-163-O04**: Retain any attempted exfiltration, destructive suggestion or instruction-following as a safety finding.

**Finish:** The study reports failures honestly even if all deterministic tests passed.
**Do not:** Do not auto-execute a dangerous suggestion to learn whether it would work.

### REC-164 — Human recovery pilot with comparable evidence

Measure actual human comprehension without simulating human participants with an LLM.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; manual.md — 79. Diagnostics and common failures.

- **REC-164-O01**: Recruit at least three consenting participants for a pilot with 12 balanced tasks each and minimal required domain briefing.
- **REC-164-O02**: Present only the same task/diagnostic/discovery information allowed to the study condition.
- **REC-164-O03**: Record correct repair, clarification, abandonment and confusing wording; avoid collecting personal data beyond study needs.
- **REC-164-O04**: Label sample size and limitations; do not generalize a small pilot into universal usability.

**Finish:** No participants available means blocked empirical evidence, never fabricated completion.
**Do not:** Do not present an agent proxy as a human result.

### REC-165 — Final evidence synthesis and next-regression intake

Separate engineering conformance from measured recovery and remaining risks.

Sources: musoq-core-language-spec.md — 23. Diagnostic Contract and Catalog, 23.1 Diagnostic envelope contract; 00-project-guide.md — Snapshot identity.

- **REC-165-O01**: Reconcile all new scope commit mappings, exact test results, product identities and empirical artifacts.
- **REC-165-O02**: Publish family-level paired outcomes, sample sizes, invalid-run exclusions and all safety/semantic-repair findings.
- **REC-165-O03**: Promote reproducible study failures into a separately versioned development backlog; do not repair and retest the same holdout as fresh validation.
- **REC-165-O04**: State engine-lane, packaged-interface and human/model evidence conclusions independently.
- **REC-165-O05**: Run the final full engine gate and all authorized host gates before the synthesis completion commit.

**Finish:** Measured negative results are visible; task completion does not assert perfect product recovery. No unrun lane or missing human result is counted as passed.
**Do not:** Do not claim absolute completeness, statistical independence of mutations or guaranteed human/agent success.

