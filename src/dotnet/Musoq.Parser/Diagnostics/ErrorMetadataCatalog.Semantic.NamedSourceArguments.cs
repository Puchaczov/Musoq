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
            DiagnosticCode.MQ3082_AmbiguousSourceInvocation,
            "More than one datasource signature can receive the supplied arguments, or runtime positional binding cannot identify one signature deterministically.",
            ["Provide values with more specific types or use a datasource method with one unambiguous signature."],
            "Core Spec - FROM Arguments");

        yield return Entry(
            DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata,
            "The datasource does not expose reflected constructor metadata required to bind argument names and defaults.",
            ["Use positional arguments for this datasource.", "Expose a reflection-backed table constructor in GetRawConstructors()."],
            "Core Spec - FROM Arguments");
    }
}
