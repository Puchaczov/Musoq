using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.Physical.PhysicalColumnUsageFacts;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static partial class ProjectionPruningCteDefinitions
{
    public static (PhysicalMultiStatementNode Node, int PrunedFields) PruneIntermediateOutputs(PhysicalMultiStatementNode multi)
    {
        var producers = IntermediateSourceProducers.Resolve(multi);
        if (producers.Count == 0 || HasPresenceDependency(multi))
            return (multi, 0);
        var names = producers.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceSchemas = producers.ToDictionary(static producer => producer.Key,
            producer => multi.Statements[producer.Value].OutputSchema, StringComparer.OrdinalIgnoreCase);
        var requiredByName = names.ToDictionary(static name => name,
            static _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        var statements = multi.Statements.ToArray();
        var schemas = new Dictionary<string, OutputSchema>(StringComparer.OrdinalIgnoreCase);
        var prunedFields = 0;

        for (var index = statements.Length - 1; index >= 0; index--)
        {
            foreach (var producer in producers.Where(producer => producer.Value == index))
            {
                var required = requiredByName[producer.Key];
                if (required.Count == 0)
                    continue;
                var pruned = PrunePassthroughOutput(statements[index], required, sourceSchemas);
                prunedFields += statements[index].OutputSchema.Columns.Length - pruned.OutputSchema.Columns.Length;
                statements[index] = pruned;
                schemas[producer.Key] = pruned.OutputSchema;
            }
            CteColumnUsageCollector.CollectRequiredCteColumns(statements[index], names, sourceSchemas, requiredByName);
        }
        if (prunedFields == 0)
            return (multi, 0);

        for (var index = 0; index < statements.Length; index++)
            statements[index] = RewriteCteRefSchemas(statements[index], schemas);
        return (new PhysicalMultiStatementNode(statements) { IntermediateProducers = multi.IntermediateProducers }, prunedFields);
    }

    private static PhysicalNode PrunePassthroughOutput(PhysicalNode node, IReadOnlySet<string> required,
        IReadOnlyDictionary<string, OutputSchema> sources)
    {
        if (node is not PhysicalProjectNode { IsDistinct: false } project ||
            ColumnUsage.HasAmbiguousOutputNames(project.Fields) ||
            required.Any(name => !project.Fields.Any(field => ColumnUsage.NameEquals(name, field.OutputName))))
            return node;

        var retained = project.Fields.Where(field => required.Contains(field.OutputName) ||
                                                    !CanRemovePassthrough(field, project.Input, sources))
            .Select((field, index) => field with { OutputIndex = index }).ToArray();
        if (retained.Length == 0 || retained.Length == project.Fields.Length)
            return node;
        return new PhysicalProjectNode(retained, project.Input);
    }

    private static bool CanRemovePassthrough(ProjectedField field, PhysicalNode input,
        IReadOnlyDictionary<string, OutputSchema> sources)
        => field.Expression is Literal || field.Expression is ColumnRef column &&
           ExpressionStabilityAnalyzer.IsStable(column) && IsDirectColumnReference(input, column, sources);

    private static bool IsDirectColumnReference(PhysicalNode input, ColumnRef column,
        IReadOnlyDictionary<string, OutputSchema> sources)
    {
        if (input is PhysicalCteRefNode reference &&
            string.Equals(reference.Alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            sources.TryGetValue(reference.CteName, out var schema))
            return schema.FindByName(CteColumnUsageCollector.NormalizeColumnName(column.ColumnName, column.Alias)) != null;

        if (input.Children.Count == 0 && ColumnUsage.ProducesAlias(input, column.Alias))
            return input.OutputSchema.FindByName(CteColumnUsageCollector.NormalizeColumnName(column.ColumnName, column.Alias)) != null;

        return input.Children.Any(child => IsDirectColumnReference(child, column, sources));
    }

    private static bool HasPresenceDependency(PhysicalNode node)
        => CteColumnUsageCollector.EnumerateNodeExpressions(node).Any(expression =>
               RowPresenceAliasExtractor.Extract(expression).Count != 0) || node.Children.Any(HasPresenceDependency);
}
