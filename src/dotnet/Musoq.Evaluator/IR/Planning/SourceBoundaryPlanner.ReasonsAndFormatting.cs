using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceBoundaryPlanner
{
    private static string CreateApplyBoundaryReason(
        ApplyKind applyKind,
        SourceBoundaryInputMode mode,
        IReadOnlyList<string> dependencyAliases)
    {
        var applyName = FormatApplyName(applyKind);
        return mode switch
        {
            SourceBoundaryInputMode.Independent => $"{applyName} keeps an explicit source boundary even though the right input has no proven left-alias dependency. It is a per-query cache candidate, but caching is not applied in this diagnostics-only wave.",
            SourceBoundaryInputMode.Correlated => $"{applyName} evaluates a lateral right source for each left row using alias(es): {FormatAliases(dependencyAliases)}. Per-row source calls are not cacheable by the current planner.",
            _ => $"{applyName} references alias(es) outside the proven left input: {FormatAliases(dependencyAliases)}."
        };
    }

    private static string CreateInterpretBoundaryReason(
        InterpretSourceNode interpret,
        ApplyKind applyKind,
        string[] inputAliases)
    {
        var applyName = FormatApplyName(applyKind);
        if (inputAliases.Length == 0)
            return $"{interpret.Kind} source {interpret.Alias} uses constant arguments and remains on the {applyName} boundary. Result shape is {FormatResultShape(interpret.ResultType)}; interpretation caching/pruning is not applied in this diagnostics-only wave.";

        return $"{interpret.Kind} source {interpret.Alias} reads argument alias(es): {FormatAliases(inputAliases)} and remains on the {applyName} boundary. Result shape is {FormatResultShape(interpret.ResultType)}; interpretation caching/pruning is not applied in this diagnostics-only wave.";
    }

    private static string CreatePropertyBoundaryReason(PropertySourceNode propertySource, ApplyKind applyKind)
    {
        if (string.IsNullOrWhiteSpace(propertySource.SourceAlias))
            return $"Property source {propertySource.Alias} has no resolved source alias.";

        return $"Property source {propertySource.Alias} expands {FormatPropertySource(propertySource)} with {FormatApplyName(applyKind)} semantics. Property expansion remains uncached because row multiplicity depends on the current source row.";
    }

    private static string CreateAccessMethodBoundaryReason(
        AccessMethodSourceNode accessMethod,
        ApplyKind applyKind,
        string[] inputAliases)
    {
        if (inputAliases.Length == 0)
            return $"Access method source {accessMethod.Alias} has no resolved input alias.";

        return $"Access method source {accessMethod.Alias} evaluates {IrExpressionPrinter.Print(accessMethod.MethodCallExpression)} with {FormatApplyName(applyKind)} semantics. Access-method source calls remain uncached because row multiplicity depends on the current invocation.";
    }

    private static string FormatResultShape(Type resultType)
    {
        return ResolveResultShape(resultType) switch
        {
            SourceResultShape.Declared => $"declared as {resultType.Name}",
            SourceResultShape.Dynamic => $"dynamic through {resultType.Name}",
            _ => "unknown"
        };
    }

    private static string FormatApplyTarget(IReadOnlyList<string> leftAliases, IReadOnlyList<string> rightAliases)
    {
        return $"{FormatAliases(leftAliases)} -> {FormatAliases(rightAliases)}";
    }

    private static string FormatPropertySource(PropertySourceNode propertySource)
    {
        if (propertySource.PropertiesChain.Length == 0)
            return propertySource.SourceAlias;

        return $"{propertySource.SourceAlias}.{string.Join(".", propertySource.PropertiesChain.Select(static property => property.PropertyName))}";
    }

    private static string FormatApplyName(ApplyKind applyKind)
    {
        return applyKind == ApplyKind.Outer ? "Outer APPLY" : "Cross APPLY";
    }

    private static string FormatAliases(IReadOnlyList<string> aliases)
    {
        return aliases.Count == 0 ? "none" : string.Join(", ", aliases);
    }

    private static string[] OrderAliases(IEnumerable<string> aliases)
    {
        return aliases
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddAlias(HashSet<string> aliases, string alias)
    {
        if (!string.IsNullOrWhiteSpace(alias))
            aliases.Add(alias);
    }
}
