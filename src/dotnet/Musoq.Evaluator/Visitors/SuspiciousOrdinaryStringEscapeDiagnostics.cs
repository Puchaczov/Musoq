using System.Linq;
using System.Reflection;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Visitors;

internal static class SuspiciousOrdinaryStringEscapeDiagnostics
{
    public static void ReportRelativePathRisk(
        DiagnosticContext? diagnosticContext,
        Node literal,
        bool pathSensitive)
    {
        if (!pathSensitive || TryGetRelativeRisk(diagnosticContext, literal) is not { } risk)
            return;

        Report(diagnosticContext!, risk);
    }

    public static void ReportMethodArgumentRisks(
        DiagnosticContext? diagnosticContext,
        ArgsListNode arguments,
        ArgsListNode sourceArguments,
        MethodInfo method,
        bool canSkipInjectSource)
    {
        if (diagnosticContext == null)
            return;

        var parameters = method.GetParameters();
        var parameterOffset = canSkipInjectSource ? 0 : GetInjectedParameterOffset(parameters);
        for (var argumentIndex = 0; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            var parameterIndex = argumentIndex + parameterOffset;
            if (parameterIndex >= parameters.Length)
                break;

            var parameter = parameters[parameterIndex];
            if (!IsPathSensitiveStringParameter(parameter.Name, parameter.ParameterType))
                continue;

            ReportRelativePathRisk(diagnosticContext, sourceArguments.Args[argumentIndex], true);
        }
    }

    public static void ReportSchemaArgumentRisks(
        DiagnosticContext? diagnosticContext,
        ArgsListNode arguments,
        ArgsListNode sourceArguments,
        SchemaSourceBindingResult bindingResult,
        SchemaMethodInfo[] methods)
    {
        if (diagnosticContext == null)
            return;

        foreach (var argumentIndex in SchemaSourceArgumentBinder.GetPathSensitiveArgumentIndexes(
                     arguments,
                     methods,
                     bindingResult.Invocation))
            ReportRelativePathRisk(diagnosticContext, sourceArguments.Args[argumentIndex], true);
    }

    public static bool IsPathSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized.EndsWith("path", StringComparison.Ordinal) ||
               normalized.EndsWith("paths", StringComparison.Ordinal) ||
               normalized is "file" or "files" or "filename" or "directory" or "dir" or "folder" or "root";
    }

    public static bool IsPathSensitiveStringParameter(string? name, Type parameterType) =>
        parameterType == typeof(string) && IsPathSensitiveName(name);

    private static StringEscapeRisk? TryGetRelativeRisk(
        DiagnosticContext? diagnosticContext,
        Node literal)
    {
        if (diagnosticContext?.SourceText is not { } sourceText || literal is not WordNode)
            return null;

        var span = literal.Span;
        if (span.Length < 2 || span.Start < 0 || span.End > sourceText.Text.Length)
            return null;

        var source = sourceText.Text.AsSpan(span.Start, span.Length);
        if (source[0] != '\'' || source[^1] != '\'')
            return null;

        var risk = StringEscapeRiskDetector.Find(source[1..^1], span.Start + 1);
        if (risk is not { IsRootedPath: false, HasNonEscapeContent: true } relativeRisk)
            return null;

        return relativeRisk;
    }

    private static void Report(DiagnosticContext diagnosticContext, StringEscapeRisk risk)
    {
        diagnosticContext.ReportWarning(
            DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, risk.EscapeText),
            risk.Span);
    }

    private static int GetInjectedParameterOffset(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0)
            return 0;

        return parameters[0].GetCustomAttributes(true).Any(static attribute =>
            attribute.GetType().Name is "InjectSpecificSourceAttribute" or "InjectSourceAttribute")
            ? 1
            : 0;
    }
}
