# Diagnostic regression inventory

This inventory records the permanent regression evidence for all 73 scopes in
`musoq-diagnostic-campaign.json`. It was reviewed on 2026-08-31 against the
Parser, Schema, Evaluator, and Converter test projects.

The classifications use these rules:

- **Strict** means the permanent evidence asserts the stable behavior and, for
  an invalid query or schema, the diagnostic code plus the relevant location
  and source metadata. `Layered` means the evidence is intentionally split
  across parser, evaluator, schema, or converter tests.
- **Partial** means the scope has real permanent evidence, but one local file
  is positive-only or validates the interpretation runtime `ISE` taxonomy
  rather than the Musoq `MQ` envelope. The adjacent evidence named in the row
  supplies the missing contract where applicable.
- **Weak local probe** identifies a test that is useful as a smoke/regression
  signal but would be insufficient as the only evidence. No weak local probe
  is the sole evidence for a campaign scope.
- Duplicate-looking tests are complementary layer coverage, not duplicate
  diagnostic definitions. No unresolved duplicate or contradictory test
  expectation was found. AUDIT-071 had already corrected the MQ4001-MQ4015
  phase drift from DataSource to Schema; this inventory verifies the corrected
  contract.

The shared evaluator assertions now require the code-derived source domain and
consistent located-envelope endpoints. The BINARY-044 direct schema visitor
regression also asserts Schema phase/source. These changes make the formerly
weak assertions fail if a future implementation regresses the structured
contract.

| Scope | Permanent evidence | Classification and result |
| --- | --- | --- |
| CORE-001 | `Musoq.Parser.Tests/Diagnostic.CORE-001LexicalTests.cs` | Strict: keyword, identifier, comment, malformed-lexeme, code, span, and query metadata coverage. |
| CORE-002 | `Musoq.Parser.Tests/Diagnostic.CORE-002StringLiteralTests.cs` | Strict: ordinary/raw literals, escapes, Unicode, malformed literals, and exact recovery diagnostics. |
| CORE-003 | `Musoq.Parser.Tests/Diagnostic.CORE-003NumericLiteralTests.cs` | Strict: inference, suffix/base boundaries, malformed literals, overflow, and exact diagnostics. |
| CORE-004 | `Musoq.Parser.Tests/Diagnostic.CORE-004OperatorTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.CORE-004OperatorTests.cs` | Partial, weak local probe: evaluator file is positive-only; parser tests provide exact invalid-operator coverage and evaluator behavior coverage. |
| CORE-005 | `Musoq.Parser.Tests/Diagnostic.CORE-005DataTypeTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.CORE-005DataTypeTests.cs` | Partial, weak local probe: evaluator file is positive-only; parser and broader evaluator tests cover invalid casts, inference, nullability, and boundaries. |
| CORE-006 | `Musoq.Parser.Tests/Diagnostic.CORE-006StatementParameterTests.cs` | Strict: statement batches, parameter placement/types/defaults, duplicate declarations, and malformed forms. |
| CORE-007 | `Musoq.Evaluator.Tests/Diagnostic.CORE-007ScriptParameterTests.cs` | Strict: required/default/runtime binding, exact runtime codes, type/null/collection behavior, and source-open ordering. |
| CORE-008 | `Musoq.Parser.Tests/Diagnostic.CORE-008ScriptVariableTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.CORE-008ScriptVariableTests.cs` | Strict, layered: declaration/parser contracts plus execution and invalid variable diagnostics. |
| CORE-009 | `Musoq.Parser.Tests/Diagnostic.CORE-009SelectTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.CORE-009SelectTests.cs` | Partial, weak local probe: dedicated files emphasize valid SELECT shape; structural and error-quality suites supply invalid SELECT diagnostics. |
| CORE-010 | `Musoq.Parser.Tests/Diagnostic.CORE-010StarModifierTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.CORE-010StarModifierTests.cs` | Strict, layered: parser grammar and evaluator expansion/diagnostic contracts. |
| CORE-011 | `Musoq.Parser.Tests/NamedDatasourceArgumentParserTests.cs`; `Musoq.Parser.Tests/Parser.RequiredSourceAliasTests.cs`; `Musoq.Evaluator.Tests/NamedDatasourceArgumentBinderMatrixTests.cs`; `Musoq.Evaluator.Tests/SymbolResolutionDiagnosticsTests.cs` | Strict, layered: source grammar, aliases, named arguments, binding, casing, and exact symbol diagnostics. |
| CORE-012 | `Musoq.Evaluator.Tests/Core012FromClauseTests.cs`; `Musoq.Evaluator.Tests/Core012ValuesDiagnosticsTests.cs`; `Musoq.Evaluator.Tests/FeatureCoverageBoundaryTests.cs` | Strict: FROM/VALUES boundaries, source spans, exact diagnostics, and subquery/source reachability. |
| CORE-013 | `Musoq.Evaluator.Tests/Core013NullComparisonSemanticsTests.cs`; `Musoq.Evaluator.Tests/PrecisionDiagnosticTests.cs`; `Musoq.Evaluator.Tests/MalformedQueryErrorTests.OperatorsAndCases.cs` | Strict, layered: NULL semantics, predicate execution, operator diagnostics, spans, phases, and source kinds. |
| CORE-014 | `Musoq.Evaluator.Tests/Core014PredicateSubqueryTests.cs` | Strict: IN/NOT IN/EXISTS, correlation, shape errors, exact envelopes, and locations. |
| CORE-015 | `Musoq.Evaluator.Tests/Core015ScalarQuantifiedSubqueryTests.cs` | Strict: scalar/ANY/ALL/SOME behavior, cardinality failures, and internal-error boundaries. |
| CORE-016 | `Musoq.Evaluator.Tests/Core016JoinSemanticsTests.cs` | Strict: join variants, NULL/non-equi behavior, invalid conditions, and diagnostic metadata. |
| CORE-017 | `Musoq.Evaluator.Tests/Core017SemiAntiPresenceTests.cs` | Strict: SEMI/ANTI presence semantics, aliases, and exact query diagnostics. |
| CORE-018 | `Musoq.Evaluator.Tests/Core018AsOfJoinSemanticsTests.cs` | Strict: ASOF matching/tie behavior and all invalid inequality forms with exact metadata. |
| CORE-019 | `Musoq.Evaluator.Tests/Core019FunctionOwnerResolutionTests.cs` | Strict: owner resolution, ambiguity, method/aggregate diagnostics, and source contracts. |
| CORE-020 | `Musoq.Evaluator.Tests/Core020ApplyFundamentalsTests.cs` | Strict: CROSS/OUTER APPLY, correlation, NULL rows, and invalid placement diagnostics. |
| CORE-021 | `Musoq.Evaluator.Tests/Core021ApplyAdvancedTests.cs` | Strict: ordinality, derived correlation, nested APPLY, planning, and diagnostics. |
| CORE-022 | `Musoq.Evaluator.Tests/Core022GroupByAggregationTests.cs` | Strict: grouping/aggregate semantics, NULL groups, invalid references, and exact diagnostics. |
| CORE-023 | `Musoq.Parser.Tests/Diagnostic.CORE-023GroupByFilterTests.cs`; `Musoq.Evaluator.Tests/Core023GroupByFilterTests.cs` | Strict, layered: FILTER/HAVING grammar, evaluation, and exact group/filter diagnostics. |
| CORE-024 | `Musoq.Parser.Tests/Diagnostic.CORE-024PivotUnpivotTests.cs`; `Musoq.Evaluator.Tests/Core024PivotUnpivotTests.cs` | Strict, layered: PIVOT/UNPIVOT shapes, measure/group boundaries, and diagnostic code coverage. |
| CORE-025 | `Musoq.Parser.Tests/Diagnostic.CORE-025WindowTests.cs`; `Musoq.Evaluator.Tests/Core025WindowTests.cs` | Strict, layered: window grammar, frames, ordering, and invalid-window diagnostics. |
| CORE-026 | `Musoq.Parser.Tests/Diagnostic.CORE-026WindowTests.cs`; `Musoq.Evaluator.Tests/Core026WindowTests.cs` | Strict, layered: second window tranche, QUALIFY/frame semantics, and boundary diagnostics. |
| CORE-027 | `Musoq.Parser.Tests/Diagnostic.CORE-027SetOperationTests.cs`; `Musoq.Evaluator.Tests/Core027SetOperationTests.cs` | Strict, layered: set shape/type/order behavior and exact mismatch diagnostics. |
| CORE-028 | `Musoq.Parser.Tests/Diagnostic.CORE-028OrderByTests.cs`; `Musoq.Evaluator.Tests/Core028OrderByTests.cs` | Strict, layered: ordinals, aliases, NULL ordering, and invalid ORDER BY diagnostics. |
| CORE-029 | `Musoq.Parser.Tests/Diagnostic.CORE-029CteTests.cs`; `Musoq.Evaluator.Tests/Core029NonRecursiveCteTests.cs` | Strict, layered: CTE scope, shape, aliases, ordering, and exact errors. |
| CORE-030 | `Musoq.Parser.Tests/Diagnostic.CORE-030RecursiveCteTests.cs`; `Musoq.Evaluator.Tests/Core030RecursiveCteTests.cs` | Strict, layered: recursive grammar/limits, shape validation, runtime codes, and generated loop contracts. |
| CORE-031 | `Musoq.Parser.Tests/Diagnostic.CORE-031DescAndFromFirstTests.cs`; `Musoq.Evaluator.Tests/Core031DescAndFromFirstTests.cs` | Strict, layered: DESC/FROM FIRST output and malformed statement-order diagnostics. |
| CORE-032 | `Musoq.Evaluator.Tests/Core032BuiltInsAndAccessTests.cs`; `Musoq.Plugins.Tests/Strings.BasicTests.cs` | Strict, layered: built-ins, nested/indexed access, coercion, and exact access diagnostics. |
| CORE-033 | `Musoq.Parser.Tests/Diagnostic.CORE-033ConformanceTests.cs` | Strict: specification conformance corpus with valid/invalid representatives and stable codes. |
| TABLE-034 | `Musoq.Parser.Tests/Diagnostic.TABLE-034TableSyntaxTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TABLE-034TableContractTests.cs` | Strict, layered: TABLE grammar, declaration contracts, spans, and query envelopes. |
| TABLE-035 | `Musoq.Evaluator.Tests/Diagnostic.TABLE-035TableSourceContractTests.cs` | Strict: source modifiers through metadata/planning/execution plus warning/error/info payloads. |
| COUPLE-036 | `Musoq.Parser.Tests/Diagnostic.COUPLE-036CoupleSyntaxTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.COUPLE-036CoupleContractTests.cs` | Strict, layered: COUPLE grammar, aliases, table contracts, and exact diagnostics. |
| COUPLE-037 | `Musoq.Evaluator.Tests/Diagnostic.COUPLE-037CoupleInvocationTests.cs` | Strict: invocation arity/types/names, argument binding, and exact source diagnostics. |
| COUPLE-038 | `Musoq.Evaluator.Tests/Diagnostic.COUPLE-038SourceRuntimeSettingsTests.cs` | Strict: settings profiles, runtime source behavior, and invalid setting diagnostics. |
| COUPLE-039 | `Musoq.Evaluator.Tests/Diagnostic.COUPLE-039CoupleCompositionTests.cs` | Strict: coupled aliases with CTE/sets/APPLY and scope diagnostics. |
| BINARY-040 | `Musoq.Parser.Tests/Diagnostic.BINARY-040BinaryDeclarationTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-040BinaryInterpretationTests.cs` | Partial, layered, weak local probe: evaluator file is positive interpretation coverage; parser file supplies declaration errors and exact spans. |
| BINARY-041 | `Musoq.Parser.Tests/Diagnostic.BINARY-041NestedArrayComputedTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-041NestedArrayComputedTests.cs` | Strict, layered: nested arrays/computed fields, valid interpretation, and exact query/schema diagnostics. |
| BINARY-042 | `Musoq.Parser.Tests/Diagnostic.BINARY-042BitAlignmentValidationTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-042BitAlignmentValidationTests.cs` | Strict, layered: alignment boundaries, schema/query source domains, spans, and exact codes. |
| BINARY-043 | `Musoq.Parser.Tests/Diagnostic.BINARY-043RepetitionDiscardTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-043RepetitionDiscardTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-043RepetitionSemanticTests.cs` | Strict, layered: repetition/discard execution and schema semantic diagnostics. |
| BINARY-044 | `Musoq.Parser.Tests/Diagnostic.BINARY-044SwitchTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-044SchemaDefinitionTests.cs` | Strict, hardened: switch parsing/semantics plus direct visitor tests now assert MQ code, span, Schema phase, and Schema source. |
| BINARY-045 | `Musoq.Evaluator.Tests/Diagnostic.BINARY-045SubstreamTests.cs` | Strict: substream behavior, invalid shape diagnostics, and exact query contracts. |
| BINARY-046 | `Musoq.Parser.Tests/Diagnostic.BINARY-046SchemaCompositionTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-046SchemaCompositionTests.cs` | Strict, layered: schema composition, inheritance/generic boundaries, analyzer envelopes, and generator failures. |
| TEXT-047 | `Musoq.Schema.Tests/Diagnostic.TEXT-047TextReaderTests.cs`; `Musoq.Parser.Tests/Diagnostic.TEXT-047TextDeclarationTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-047TextInterpretationTests.cs` | Partial, layered, weak local probe: evaluator interpretation file is positive-focused; parser and reader tests cover declaration/reader boundaries and diagnostics. |
| TEXT-048 | `Musoq.Schema.Tests/Diagnostic.TEXT-048TextReaderTests.cs`; `Musoq.Parser.Tests/Diagnostic.TEXT-048TextFieldsTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-048TextInterpretationTests.cs` | Partial, layered, weak local probe: positive reader/interpreter evidence is complemented by parser field diagnostics. |
| TEXT-049 | `Musoq.Parser.Tests/Diagnostic.TEXT-049TextAlternativesTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-049TextInterpretationTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-049TextSchemaDefinitionTests.cs` | Strict, layered: alternatives, schema definition errors, and runtime interpretation behavior. |
| TEXT-050 | `Musoq.Schema.Tests/Diagnostic.TEXT-050TextReaderTests.cs`; `Musoq.Parser.Tests/Diagnostic.TEXT-050TextDeclarationTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-050TextInterpretationTests.cs` | Strict, layered: declaration/reader/interpreter boundaries and exact schema diagnostics. |
| TEXT-051 | `Musoq.Parser.Tests/Diagnostic.TEXT-051TextSchemaCompositionTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-051TextSchemaCompositionTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.TEXT-051TextInterpretationTests.cs` | Partial, layered, weak local probe: composition and interpretation files emphasize valid behavior; schema/error-quality suites cover invalid composition. |
| INTERPRET-052 | `Musoq.Evaluator.Tests/Diagnostic.INTERPRET-052InterpretationPlacementTests.cs` | Strict: placement and source-shape errors assert exact MQ code, phase/source, span, and guidance. |
| INTERPRET-053 | `Musoq.Evaluator.Tests/Diagnostic.INTERPRET-053SuccessfulBindingTests.cs` | Partial: three positive generated-binding cases plus an exact unknown-schema diagnostic; positive cases intentionally have no error code. |
| INTERPRET-054 | `Musoq.Evaluator.Tests/Diagnostic.INTERPRET-054ApplyNullSemanticsTests.cs` | Partial, weak local probe: positive NULL-preservation behavior has no diagnostic assertion; invalid/apply contracts are covered by INTERPRET-052 and CROSS-068. |
| INTERPRET-055 | `Musoq.Evaluator.Tests/Diagnostic.INTERPRET-055InterpretAtAndPartialResultsTests.cs` | Partial: exact ISE0008/ISE0001 fields, positions, details, and partial results; this is the interpretation runtime taxonomy, not an MQ envelope. |
| INTERPRET-056 | `Musoq.Evaluator.Tests/Diagnostic.INTERPRET-056BinaryTextCompositionAndSqlTests.cs` | Partial: exact ISE0001 boundary failure plus binary/text/SQL integration; MQ cross-domain contracts are covered by CROSS-068 and AUDIT-071. |
| RUNTIME-057 | `Musoq.Evaluator.Tests/Diagnostic.RUNTIME-057InterpretationErrorTaxonomyTests.cs` | Partial: exact ISE0001-ISE0015 taxonomy and safe details/fields; these runtime interpreter errors deliberately do not masquerade as MQ diagnostics. |
| RUNTIME-058 | `Musoq.Evaluator.Tests/ScriptParameterExecutionTests.cs`; `Musoq.Evaluator.Tests/ScriptParameterExecutionTests.RuntimeDiagnostics.cs` | Strict, layered: runtime parameter success/failure, exact MQ700x codes, safe messages, and source-open ordering. |
| RUNTIME-059 | `Musoq.Evaluator.Tests/DataSourceLifecycleDiagnosticTests.cs`; `Musoq.Converter.Tests/CompiledQueryArtifactApiTests.cs`; `Musoq.Parser.Tests/StructuredDiagnosticPayloadTests.cs` | Strict, layered: datasource/generated-source/internal domains, safe details, arguments, and unknown-location behavior. |
| RUNTIME-060 | `Musoq.Parser.Tests/Diagnostic.RUNTIME-060EnvelopeSafetyTests.cs`; `Musoq.Converter.Tests/Diagnostic.RUNTIME-060EnvelopeSafetyTests.cs` | Strict, layered: safe envelope/formatter behavior, redaction, JSON, and no fabricated query locations. |
| UX-061 | `Musoq.Parser.Tests/Diagnostic.UX-061KeywordMisspellingsTests.cs` | Strict: bounded typo recognition, exact replacement spans, native guidance, and no false positives. |
| UX-062 | `Musoq.Parser.Tests/Diagnostic.UX-062DialectConfusionTests.cs` | Strict: foreign-dialect near-misses, exact codes/spans, actionable guidance, and valid Musoq equivalents. |
| UX-063 | `Musoq.Evaluator.Tests/Diagnostic.UX-063SymbolResolutionTests.cs` | Strict: symbol/callable/type/schema/CTE mistakes, candidate quality, casing, and safe edits. |
| UX-064 | `Musoq.Parser.Tests/Diagnostic.UX-064DiagnosticContractTests.cs`; `Musoq.Evaluator.Tests/Diagnostic.UX-064SourceContextTests.cs` | Strict, layered: spans, snippets, actions, docs, safe JSON/details, multiline and unknown locations. |
| CROSS-065 | `Musoq.Evaluator.Tests/Diagnostic.CROSS-065JoinApplyAggregateWindowTests.cs` | Strict: positive composition plus focused malformed variants with exact codes, phases, sources, and locations. |
| CROSS-066 | `Musoq.Evaluator.Tests/Diagnostic.CROSS-066CteSubqueryIntegrationTests.cs` | Strict: nested/recursive CTE and subquery composition, exact shape codes, spans, source, guidance, and cascade suppression. |
| CROSS-067 | `Musoq.Evaluator.Tests/Diagnostic.CROSS-067TableCoupleIntegrationTests.cs` | Strict: TABLE/COUPLE integration, settings, aliases, source-contract warning/error payloads, and exact locations. |
| CROSS-068 | `Musoq.Evaluator.Tests/Diagnostic.CROSS-068InterpretationSqlIntegrationTests.cs` | Strict, layered: interpretation with SQL/TABLE/COUPLE constructs, ISE partial results, and MQ cross-domain errors. |
| CROSS-069 | `Musoq.Evaluator.Tests/Diagnostic.CROSS-069MultipleErrorRecoveryTests.cs` | Strict: independent roots, dependent-cascade suppression, deterministic ordering, precise spans, and no internal diagnostics. |
| AUDIT-070 | `Musoq.Parser.Tests/Diagnostic.AUDIT-070RegistryCompletenessTests.cs` | Strict: every public code has one descriptor, coherent metadata, actions, and documentation; no orphan/duplicate registry entries. |
| AUDIT-071 | `Musoq.Converter.Tests/Diagnostic.AUDIT-071PayloadConsistencyTests.cs` | Strict: representative parse/bind/schema/datasource/runtime/generated/internal payloads, endpoints, arguments, related locations, edits, snippets, correlation IDs, and safe serialization. |
| AUDIT-072 | `docs/diagnostic-regression-inventory.md`; `Musoq.Evaluator.Tests/MusoqExceptionAssertions.cs`; `Musoq.Evaluator.Tests/DiagnosticContractTestAssertions.cs`; `Musoq.Evaluator.Tests/Diagnostic.BINARY-044SchemaDefinitionTests.cs` | Hardened: full-scope evidence inventory, explicit partial/weak/layered classification, shared source/location assertion hardening, and schema phase/source regression. |
| AUDIT-073 | Pending final gate; campaign ledger and Git history | Pending: whole-campaign completion, commit traceability, final full Release gate, clean worktree, and recorded drift review. |

## Audit conclusion

All scopes through AUDIT-072 have permanent evidence in the repository. The
partial classifications are bounded and documented: they identify a local
positive-only probe or a separate interpretation-runtime error taxonomy, not a
missing campaign obligation. There are no unresolved weak-only scopes, duplicate
diagnostic definitions, or contradictory expectations. AUDIT-073 remains the
sole final-readiness obligation.
