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
                "Set the parameter in CompiledQuery.Parameters before calling Run().",
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
                "Remove the unknown parameter from the runtime parameter dictionary.",
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

        foreach (var entry in RecursiveCteRuntimeErrorMetadata.Build())
            yield return entry;
    }
}
