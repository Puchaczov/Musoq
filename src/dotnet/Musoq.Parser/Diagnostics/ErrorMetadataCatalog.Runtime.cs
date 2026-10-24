using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class RuntimeErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "A required script parameter was declared without a default value, but the host did not provide a runtime value.",
            [
                "Set the parameter with CompiledQuery.TypedParameters.Set, SetCollection or SetJson before calling Run().",
                "Add a default value in the param(...) block if the parameter should be optional."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Runtime script parameter values are strict CLR casts and are not converted from strings or other types.",
            [
                "Pass a value whose CLR type matches the declared Musoq parameter type.",
                "For example, pass 10 as an int for param(limit: int), not \"10\"."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            "A null runtime value was supplied for a non-nullable script parameter.",
            [
                "Provide a non-null value for this parameter.",
                "Declare the parameter as nullable, for example int?, if null is allowed."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ7006_UnknownScriptParameter,
            "The host supplied a runtime script parameter that is not declared by the query.",
            [
                "Remove the unknown value from the typed parameter slots or structured input.",
                "Add the parameter to the param(...) block if the query should accept it."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ7010_DataSourceOpenFailed,
            "A schema or source could not be opened during query execution. The source arguments are intentionally not included in diagnostics.",
            [
                "Check that the schema is available and that the source can be constructed with the supplied query values.",
                "Inspect the verbose exception details when debugging a trusted local provider."
            ],
            "Core Spec - Data Sources");

        yield return Entry(
            DiagnosticCode.MQ7011_DataSourceReadFailed,
            "A data source failed while producing or reading rows. The query was not classified as a syntax or binding error.",
            [
                "Check the provider's connection, stream, and iterator implementation.",
                "Inspect the verbose exception details when debugging a trusted local provider."
            ],
            "Core Spec - Data Sources");

        yield return Entry(
            DiagnosticCode.MQ7012_DataSourceCleanupFailed,
            "A data source failed while releasing its row-enumeration resources.",
            [
                "Check the provider's enumerator and resource-disposal implementation.",
                "Inspect the verbose exception details when debugging a trusted local provider."
            ],
            "Core Spec - Data Sources");

        yield return Entry(
            DiagnosticCode.MQ7013_StructuralInputLimitExceeded,
            "A structural input exceeded the configured depth, node, or string-payload bound before it could be used by a query source.",
            [
                "Reduce the supplied structure or increase the receiving contract's limit.",
                "Check the reported origin and path to locate the large or deeply nested value."
            ],
            "Core Spec - Structured Inputs");

        yield return Entry(
            DiagnosticCode.MQ7014_MalformedRuntimeInput,
            "Runtime input was rejected because its structure or encoding was malformed. The diagnostic does not include input content or provider exception text.",
            [
                "Check that the supplied input matches the format expected by the source or interpreter.",
                "Use the input location, when present, to inspect the corresponding value in a trusted environment."
            ],
            "Core Spec - Runtime Input");

        foreach (var diagnosticCode in InterpretationDiagnosticCodes.All)
        {
            yield return Entry(
                diagnosticCode,
                "An input interpretation failed. The original ISE code is available in the interpretationCode argument; raw parser details are reserved for trusted verbose diagnostics.",
                [
                    "Check the input format and the interpretation schema at the reported input location.",
                    "Use trusted verbose diagnostics when the safe summary is not enough to identify the malformed field."
                ],
                "Core Spec - Interpretation");
        }

        yield return Entry(
            DiagnosticCode.MQ7015_InvalidRuntimeRegex,
            "A runtime RLIKE pattern is not valid for the regex engine.",
            ["Correct the supplied regular expression syntax or escape intended metacharacters."],
            "Core Spec - Pattern Predicates");

        yield return Entry(
            DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation,
            "A scalar subquery produced more than one result row during execution, even if the values are identical.",
            ["Add an aggregate, TAKE 1, or a predicate that returns at most one row.", "Use IN or EXISTS when multiple rows are intended."],
            "Core Spec - Subqueries");

        yield return Entry(
            DiagnosticCode.MQ7017_RegexMatchTimeout,
            "A LIKE or RLIKE regex match exhausted its finite elapsed-time budget. Machine load can also consume that budget.",
            ["Simplify the pattern or reduce the input size.", "Review the configured regex match timeout and machine load."],
            "Core Spec - Pattern Predicates");

        yield return Entry(
            DiagnosticCode.MQ7018_NumericOverflow,
            "Numeric arithmetic, checked aggregation, or a strict numeric conversion overflowed its declared type during execution.",
            ["Widen operands before arithmetic or use a wider aggregate input type.",
                "Casting an already computed result does not prevent overflow; reduce the values when a wider type is unsuitable."],
            "Core Spec - Numeric Arithmetic");

        foreach (var entry in RecursiveCteRuntimeErrorMetadata.Build())
            yield return entry;
    }
}
