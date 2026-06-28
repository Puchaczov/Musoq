using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class RuntimeErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ7001_DataSourceBindingFailed,
            "The runtime could not bind to the data source constructor.",
            [
                "Query the source directly and cast columns inline when possible.",
                "Use a supported typed source path when available."
            ],
            "TABLE/COUPLE Spec - Integration");

        yield return Entry(
            DiagnosticCode.MQ7002_DataSourceIteratorError,
            "The data source entered an invalid iterator state during execution.",
            [
                "Retry the query after resetting the data source.",
                "Check the data source implementation for iterator state errors."
            ],
            "Datasource Troubleshooting");

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
    }
}
