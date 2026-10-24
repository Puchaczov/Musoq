using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionExpressionCaptureNames
{
    internal static (string Value, string Pattern) Reserve(ExecutionRenderContext context,
        ExpressionSyntax value, ExpressionSyntax pattern)
    {
        var session = context.Session;
        var names = session.ValueCaptureNames ??= CollectNames(context);
        AddIdentifiers(value, names);
        AddIdentifiers(pattern, names);
        while (true)
        {
            var prefix = $"__musoqStringPredicate{++session.StringPredicateCaptureCount}";
            var valueName = prefix + "Value";
            var patternName = prefix + "Pattern";
            if (names.Contains(valueName) || names.Contains(patternName))
                continue;
            names.Add(valueName);
            names.Add(patternName);
            return (valueName, patternName);
        }
    }

    private static HashSet<string> CollectNames(ExecutionRenderContext context)
    {
        var names = context.Session.ValueCaptureScope is { } scope
            ? ExecutionIrAnalysis.CollectDeclaredVariableNames(scope).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
        names.UnionWith(context.Options.ScriptParameterLocalNames.Values);
        names.UnionWith(context.Options.ScriptVariableLocalNames.Values);
        return names;
    }

    private static void AddIdentifiers(ExpressionSyntax expression, HashSet<string> names)
    {
        foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            names.Add(identifier.Identifier.ValueText);
    }
}
