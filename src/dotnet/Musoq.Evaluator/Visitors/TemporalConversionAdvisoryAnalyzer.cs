using System.Collections.Generic;
using System.Globalization;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class TemporalConversionAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        Visit(context, context.Query, new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void Visit(
        SemanticAdvisoryContext context,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is AccessMethodNode method)
        {
            ReportIfAmbiguousImplicitConversion(context, method);
            ReportIfImpossibleImplicitConversion(context, method);
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, visited);
    }

    private static void ReportIfAmbiguousImplicitConversion(
        SemanticAdvisoryContext context,
        AccessMethodNode method)
    {
        var targetName = method.Name switch
        {
            "ToDateTime" => "DateTime",
            "ToDateTimeOffset" => "DateTimeOffset",
            _ => null
        };
        if (targetName == null || method.ArgsCount != 1)
            return;

        var argument = method.Arguments.Args[0];
        if (!argument.HasSpan ||
            method.Span.Start != argument.Span.Start ||
            method.Span.Length != argument.Span.Length ||
            !context.Literals.TryResolve(argument, out var origin) ||
            !IsAmbiguousDateText(origin.Value))
        {
            return;
        }

        context.Report(
            DiagnosticCode.MQ5003_ImplicitTypeConversion,
            ErrorCatalog.GetMessage(
                DiagnosticCode.MQ5003_ImplicitTypeConversion,
                "string",
                targetName),
            origin.Span);
    }

    private static void ReportIfImpossibleImplicitConversion(
        SemanticAdvisoryContext context,
        AccessMethodNode method)
    {
        var targetName = method.Name switch
        {
            "ToDateTime" => "DateTime",
            "ToDateTimeOffset" => "DateTimeOffset",
            "ToTimeSpan" => "TimeSpan",
            _ => null
        };
        if (targetName == null || method.ArgsCount != 1)
            return;

        var argument = method.Arguments.Args[0];
        if (!argument.HasSpan ||
            method.Span.Start != argument.Span.Start ||
            method.Span.Length != argument.Span.Length ||
            !context.Literals.TryResolve(argument, out var origin) ||
            CanParse(targetName, origin.Value))
        {
            return;
        }

        context.Report(
            DiagnosticCode.MQ5025_ImpossibleImplicitConversion,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5025_ImpossibleImplicitConversion, targetName),
            origin.Span);
    }

    private static bool CanParse(string targetName, string value)
    {
        return targetName switch
        {
            "DateTime" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "DateTimeOffset" => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "TimeSpan" => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out _),
            _ => true
        };
    }

    private static bool IsAmbiguousDateText(string value)
    {
        var text = value.AsSpan();
        var position = 0;

        if (!TryReadComponent(text, ref position, out var first, out var firstDigits) ||
            position >= text.Length ||
            text[position] is not ('/' or '-'))
            return false;

        var separator = text[position++];
        if (!TryReadComponent(text, ref position, out var second, out _) ||
            position >= text.Length ||
            text[position] != separator)
            return false;

        position++;
        if (!TryReadComponent(text, ref position, out _, out var yearDigits))
            return false;

        return firstDigits != 4 &&
               yearDigits >= 2 &&
               first is >= 1 and <= 12 &&
               second is >= 1 and <= 12;
    }

    private static bool TryReadComponent(
        ReadOnlySpan<char> text,
        ref int position,
        out int value,
        out int digits)
    {
        var start = position;
        value = 0;

        while (position < text.Length && char.IsAsciiDigit(text[position]))
        {
            value = Math.Min(value * 10 + text[position] - '0', 10000);
            position++;
        }

        digits = position - start;
        return digits > 0;
    }
}
