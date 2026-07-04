using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static void CollectRequiredCteColumns(
        PhysicalNode node,
        IReadOnlySet<string> cteNames,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas,
        IDictionary<string, HashSet<string>> required)
    {
        var refsByAlias = CollectCteRefsByAlias(node, cteNames);
        if (refsByAlias.Count != 0)
        {
            foreach (var expression in EnumerateNodeExpressions(node))
                AddRequiredColumns(expression, refsByAlias, outputSchemas, required);
        }

        foreach (var child in node.Children)
            CollectRequiredCteColumns(child, cteNames, outputSchemas, required);
    }

    private static Dictionary<string, List<PhysicalCteRefNode>> CollectCteRefsByAlias(
        PhysicalNode node,
        IReadOnlySet<string> cteNames)
    {
        var refs = new Dictionary<string, List<PhysicalCteRefNode>>(StringComparer.OrdinalIgnoreCase);
        CollectCteRefsByAlias(node, cteNames, refs);
        return refs;
    }

    private static void CollectCteRefsByAlias(
        PhysicalNode node,
        IReadOnlySet<string> cteNames,
        IDictionary<string, List<PhysicalCteRefNode>> refs)
    {
        if (node is PhysicalCteRefNode cteRef && cteNames.Contains(cteRef.CteName))
        {
            if (!refs.TryGetValue(cteRef.Alias, out var aliases))
                refs[cteRef.Alias] = aliases = [];

            aliases.Add(cteRef);
            return;
        }

        foreach (var child in node.Children)
            CollectCteRefsByAlias(child, cteNames, refs);
    }

    private static void AddRequiredColumns(
        IrExpression expression,
        IReadOnlyDictionary<string, List<PhysicalCteRefNode>> refsByAlias,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas,
        IDictionary<string, HashSet<string>> required)
    {
        foreach (var column in ColumnRefExtractor.Extract(expression))
        {
            if (!string.IsNullOrWhiteSpace(column.Alias))
            {
                if (refsByAlias.TryGetValue(column.Alias, out var refs))
                    AddRequiredColumns(column, refs, outputSchemas, required);

                continue;
            }

            var matches = refsByAlias.Values
                .SelectMany(static refs => refs)
                .Where(cteRef => MatchesCteColumn(cteRef, column.ColumnName, outputSchemas))
                .ToArray();

            if (matches.Length == 1)
                AddRequiredColumn(matches[0], column.ColumnName, outputSchemas, required);
            else if (matches.Length > 1)
                foreach (var cteRef in matches)
                    AddAllColumns(cteRef, outputSchemas, required);
        }
    }

    private static void AddRequiredColumns(
        ColumnRef column,
        IReadOnlyList<PhysicalCteRefNode> refs,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas,
        IDictionary<string, HashSet<string>> required)
    {
        if (refs.Count == 1)
            AddRequiredColumn(refs[0], column.ColumnName, outputSchemas, required);
        else
            foreach (var cteRef in refs)
                AddAllColumns(cteRef, outputSchemas, required);
    }

    private static void AddRequiredColumn(
        PhysicalCteRefNode cteRef,
        string columnName,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas,
        IDictionary<string, HashSet<string>> required)
    {
        var normalized = NormalizeColumnName(columnName, cteRef.Alias);
        var root = GetColumnRoot(normalized);
        var outputSchema = ResolveCteOutputSchema(cteRef, outputSchemas);
        var match = outputSchema.FindByName(normalized) ?? outputSchema.FindByName(root);

        if (match == null)
        {
            AddAllColumns(cteRef, outputSchemas, required);
            return;
        }

        required[cteRef.CteName].Add(match.Name);
    }

    private static void AddAllColumns(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas,
        IDictionary<string, HashSet<string>> required)
    {
        foreach (var column in ResolveCteOutputSchema(cteRef, outputSchemas).Columns)
            required[cteRef.CteName].Add(column.Name);
    }

    private static bool MatchesCteColumn(
        PhysicalCteRefNode cteRef,
        string columnName,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas)
    {
        var normalized = NormalizeColumnName(columnName, cteRef.Alias);
        var outputSchema = ResolveCteOutputSchema(cteRef, outputSchemas);
        return outputSchema.FindByName(normalized) != null ||
               outputSchema.FindByName(GetColumnRoot(normalized)) != null;
    }

    private static OutputSchema ResolveCteOutputSchema(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, OutputSchema> outputSchemas)
    {
        return outputSchemas.TryGetValue(cteRef.CteName, out var outputSchema)
            ? outputSchema
            : cteRef.OutputSchema;
    }

    private static IEnumerable<IrExpression> EnumerateNodeExpressions(PhysicalNode node)
    {
        return node switch
        {
            PhysicalProjectNode project => project.Fields.Select(static field => field.Expression),
            PhysicalFilterNode filter => [filter.Predicate],
            PhysicalHavingFilterNode filter => [filter.Predicate],
            PhysicalQualifyFilterNode filter => [filter.Predicate],
            PhysicalHashJoinNode join => join.BuildKeys.Concat(join.ProbeKeys).Concat(Optional(join.Residual)),
            PhysicalNestedLoopJoinNode join => join.TieBreak == null
                ? [join.OnPredicate]
                : [join.OnPredicate, join.TieBreak.Expression],
            PhysicalSortMergeJoinNode join => [join.LeftKey, join.RightKey, join.Residual],
            PhysicalSortNode sort => sort.Keys.Select(static key => key.Expression),
            PhysicalTopNNode top => top.Keys.Select(static key => key.Expression),
            PhysicalTopOffsetNode top => top.Keys.Select(static key => key.Expression),
            PhysicalSingleKeyAggregateNode aggregate => [aggregate.GroupKey, ..EnumerateAggregateArguments(aggregate.Bindings)],
            PhysicalValueTupleAggregateNode aggregate => [..aggregate.GroupKeys, ..EnumerateAggregateArguments(aggregate.Bindings)],
            PhysicalAggregateOnlyNode aggregate => EnumerateAggregateArguments(aggregate.Bindings),
            PhysicalWindowNode window => EnumerateWindowArguments(window.Registrations),
            PhysicalSchemaScanNode scan => scan.Arguments.Concat(scan.PushedPredicates),
            PhysicalInterpretSourceNode interpret => interpret.Arguments,
            PhysicalAccessMethodSourceNode access => [access.MethodCallExpression],
            _ => []
        };
    }

    private static IEnumerable<IrExpression> EnumerateAggregateArguments(IEnumerable<AggregateBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            foreach (var argument in binding.SetArguments)
                yield return argument;
            if (binding.FilterPredicate != null)
                yield return binding.FilterPredicate;
            foreach (var argument in binding.GetArguments)
                yield return argument;
        }
    }

    private static IEnumerable<IrExpression> EnumerateWindowArguments(IEnumerable<WindowRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            foreach (var key in registration.PartitionKeys)
                yield return key;
            foreach (var key in registration.OrderKeys)
                yield return key.Expression;
            foreach (var argument in registration.ValueArguments)
                yield return argument;
            if (registration.FilterPredicate != null)
                yield return registration.FilterPredicate;
        }
    }

    private static IEnumerable<IrExpression> Optional(IrExpression? expression)
    {
        if (expression != null)
            yield return expression;
    }

    private static string NormalizeColumnName(string columnName, string alias)
    {
        var prefix = $"{alias}.";
        return columnName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? columnName[prefix.Length..]
            : columnName;
    }

    private static string GetColumnRoot(string columnName)
    {
        var dotIndex = columnName.IndexOf('.', StringComparison.Ordinal);
        var bracketIndex = columnName.IndexOf('[', StringComparison.Ordinal);
        var index = dotIndex < 0 ? bracketIndex : bracketIndex < 0 ? dotIndex : Math.Min(dotIndex, bracketIndex);
        return index < 0 ? columnName : columnName[..index];
    }
}
