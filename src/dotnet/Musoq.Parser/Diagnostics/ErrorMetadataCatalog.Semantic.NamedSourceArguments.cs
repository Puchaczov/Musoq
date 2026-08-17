using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildNamedSourceArgumentMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3079_UnknownSourceArgument,
            "A named datasource argument does not match any parameter in the selected source signature.",
            ["Check the parameter spelling in DESC output.", "Use the constructor parameter name exposed by the datasource."],
            "Core Spec - FROM Arguments");

        yield return Entry(
            DiagnosticCode.MQ3080_DuplicateSourceArgument,
            "A datasource parameter was supplied more than once, either by name or by positional prefix plus name.",
            ["Supply each datasource parameter exactly once."],
            "Core Spec - FROM Arguments");

        yield return Entry(
            DiagnosticCode.MQ3081_MissingRequiredSourceArgument,
            "A required datasource parameter was not supplied and has no usable reflected default.",
            ["Provide the parameter positionally or by name.", "Use a datasource constructor with an optional default if omission is intended."],
            "Core Spec - FROM Arguments");

        yield return Entry(
            DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata,
            "The datasource does not expose reflected constructor metadata required to bind argument names and defaults.",
            ["Use positional arguments for this datasource.", "Expose a reflection-backed table constructor in GetRawConstructors()."],
            "Core Spec - FROM Arguments");

        yield return Entry(
            DiagnosticCode.MQ3086_UnknownCallable,
            "No scalar, aggregate, row, library, or source callable with this name is available in the selected owner.",
            ["Check the callable name for a spelling error.", "Use DESC or the schema documentation to inspect available callables."],
            "Core Spec - Method Resolution");

        yield return Entry(
            DiagnosticCode.MQ3087_InvalidCallableArity,
            "The callable name is known, but none of its overloads accepts the supplied number of arguments.",
            ["Check the callable signature and supplied argument count.", "Provide required arguments or remove extra arguments."],
            "Core Spec - Method Resolution");

        yield return Entry(
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            "At least one callable overload has the requested arity, but none accepts the supplied argument types.",
            ["Convert the arguments explicitly or choose an overload-compatible value.", "Check the candidate signatures in the diagnostic."],
            "Core Spec - Method Resolution");

        yield return Entry(
            DiagnosticCode.MQ3089_AmbiguousCallableOverload,
            "More than one callable overload is equally valid for the supplied arguments.",
            ["Convert an argument explicitly or qualify the call so one overload is selected.", "Check the candidate signatures in the diagnostic."],
            "Core Spec - Method Resolution");
    }
}
