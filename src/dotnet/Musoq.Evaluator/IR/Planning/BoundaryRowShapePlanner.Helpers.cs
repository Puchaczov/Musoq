using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;
namespace Musoq.Evaluator.IR.Planning;

internal static partial class BoundaryRowShapePlanner
{
    private sealed class ColumnUsageIndex
    {
        private readonly HashSet<string> _requiredColumns = new(StringComparer.OrdinalIgnoreCase);

        public static ColumnUsageIndex Create(PlanProperties properties)
        {
            var index = new ColumnUsageIndex();

            foreach (var usage in properties.RequiredColumnUsagesBySourceId.Values.SelectMany(static usages => usages))
                index.AddUsage(usage);

            return index;
        }

        public bool IsRequired(string column)
        {
            return Contains(_requiredColumns, column);
        }

        private void AddUsage(RequiredColumnUsage usage)
        {
            AddColumn(_requiredColumns, usage.Alias, usage.ColumnName);
        }

        private static void AddColumn(HashSet<string> columns, string alias, string columnName)
        {
            if (!string.IsNullOrWhiteSpace(alias))
                columns.Add($"{alias}.{columnName}");

            columns.Add(columnName);
        }

        private static bool Contains(HashSet<string> columns, string column)
        {
            return columns.Contains(column) || columns.Contains(GetColumnName(column));
        }
    }

    private static PlanningDecision CreateDecision(BoundaryRowShapePlan plan)
    {
        var outcome = plan.BoundaryOnlyColumns.Length == 0 && plan.FutureDroppableColumns.Length == 0
            ? "NoOpportunity"
            : "DiagnosticOnlyOpportunity";

        return new PlanningDecision(
            PlanningDecisionCategory.BoundaryRowShape,
            "BoundaryRowShapePlan",
            plan.BoundaryId,
            outcome,
            plan.Confidence,
            plan.Reason);
    }

    private static PhysicalNode ResolveBuildSide(PhysicalHashJoinNode hashJoin)
    {
        var buildAliases = CollectColumns(hashJoin.BuildKeys)
            .Select(GetAlias)
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .ToArray();

        if (buildAliases.Any(alias => ProducesAlias(hashJoin.Left, alias)))
            return hashJoin.Left;

        return hashJoin.Right;
    }

    private static bool ProducesAlias(PhysicalNode node, string alias)
    {
        return node switch
        {
            PhysicalSchemaScanNode scan => string.Equals(scan.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalCteRefNode cteRef => string.Equals(cteRef.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalValuesScanNode values => string.Equals(values.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalUnpivotNode unpivot => string.Equals(unpivot.Alias, alias, StringComparison.OrdinalIgnoreCase),
            _ => node.Children.Any(child => ProducesAlias(child, alias))
        };
    }

    private static string[] FilterColumnsForCte(string name, IReadOnlyList<string> queryColumns)
    {
        return queryColumns
            .Where(column => string.Equals(GetAlias(column), name, StringComparison.OrdinalIgnoreCase))
            .Select(GetColumnName)
            .ToArray();
    }

    private static string[] ResolveSetOperationColumns(PhysicalNode input, IReadOnlyList<int> indexes)
    {
        var columns = ResolveAvailableColumns(input);
        return indexes
            .Where(index => index >= 0 && index < columns.Length)
            .Select(index => columns[index])
            .ToArray();
    }

    private static PhysicalNode UnwrapMaterialize(PhysicalNode node)
    {
        return node is PhysicalMaterializeNode materialize ? materialize.Input : node;
    }
    private static string[] ResolveAvailableColumns(PhysicalNode node)
    {
        return node switch
        {
            PhysicalSchemaScanNode scan => ResolveSchemaScanColumns(scan),
            PhysicalCteRefNode cteRef => cteRef.OutputSchema.Columns.Select(column => Qualify(cteRef.Alias, column.Name)).ToArray(),
            PhysicalValuesScanNode values => values.OutputSchema.Columns.Select(column => Qualify(values.Alias, column.Name)).ToArray(),
            PhysicalUnpivotNode unpivot => unpivot.OutputSchema.Columns.Select(column => Qualify(unpivot.Alias, column.Name)).ToArray(),
            PhysicalProjectNode project => project.Fields.Select(static field => field.OutputName).ToArray(),
            PhysicalMaterializeNode materialize => ResolveAvailableColumns(materialize.Input),
            _ => SchemaColumns(node.OutputSchema)
        };
    }
    private static string[] ResolveSchemaScanColumns(PhysicalSchemaScanNode scan)
    {
        var columns = scan.ProjectedColumns.Length == 0
            ? scan.OutputSchema.Columns.Select(static column => column.Name)
            : scan.ProjectedColumns;

        return columns.Select(column => Qualify(scan.Alias, column)).ToArray();
    }
    private static string[] CollectWindowColumns(IReadOnlyList<WindowRegistration> registrations)
    {
        return registrations
            .SelectMany(registration =>
                CollectColumns(registration.PartitionKeys)
                    .Concat(CollectOrderColumns(registration.OrderKeys))
                    .Concat(CollectColumns(registration.ValueArguments)))
            .ToArray();
    }
    private static string[] CollectAggregateColumns(IrExpression groupKey, IReadOnlyList<AggregateBinding> bindings)
    {
        return CollectColumns([groupKey]).Concat(CollectAggregateColumns(bindings)).ToArray();
    }
    private static string[] CollectAggregateColumns(IReadOnlyList<IrExpression> groupKeys, IReadOnlyList<AggregateBinding> bindings)
    {
        return CollectColumns(groupKeys).Concat(CollectAggregateColumns(bindings)).ToArray();
    }

    private static string[] CollectAggregateColumns(IReadOnlyList<AggregateBinding> bindings)
    {
        return bindings
            .SelectMany(binding => CollectColumns(binding.SetArguments).Concat(CollectColumns(binding.GetArguments)))
            .ToArray();
    }

    private static string[] CollectOrderColumns(IReadOnlyList<OrderField> keys)
    {
        return keys.SelectMany(static key => CollectColumns(key.Expression)).ToArray();
    }

    private static string[] CollectColumns(PhysicalNode node)
    {
        var columns = new List<string>();
        AddColumns(node, columns);
        return columns.ToArray();
    }

    private static void AddColumns(PhysicalNode node, List<string> columns)
    {
        switch (node)
        {
            case PhysicalProjectNode project:
                columns.AddRange(CollectColumns(project.Fields.Select(static field => field.Expression)));
                break;
            case PhysicalFilterNode filter:
                columns.AddRange(CollectColumns(filter.Predicate));
                break;
            case PhysicalHavingFilterNode having:
                columns.AddRange(CollectColumns(having.Predicate));
                break;
            case PhysicalQualifyFilterNode qualify:
                columns.AddRange(CollectColumns(qualify.Predicate));
                break;
            case PhysicalUnpivotNode unpivot:
                AddUnpivotColumns(unpivot, columns);
                break;
        }

        foreach (var child in node.Children)
            AddColumns(child, columns);
    }

    private static string[] CollectColumns(IEnumerable<IrExpression> expressions)
    {
        return expressions.SelectMany(CollectColumns).ToArray();
    }

    private static string[] CollectColumns(IrExpression expression)
    {
        return ColumnRefExtractor.Extract(expression).Select(FormatColumn).ToArray();
    }

    private static string FormatColumn(ColumnRef column)
    {
        return Qualify(column.Alias, column.ColumnName);
    }

    private static string Qualify(string alias, string columnName)
    {
        return string.IsNullOrWhiteSpace(alias) ? columnName : $"{alias}.{columnName}";
    }

    private static string[] SchemaColumns(OutputSchema schema)
    {
        return schema.Columns.Select(static column => column.Name).ToArray();
    }

    private static string[] Merge(IEnumerable<string> left, IEnumerable<string> right)
    {
        return OrderColumns(left.Concat(right));
    }

    private static string[] OrderColumns(IEnumerable<string> columns)
    {
        return columns
            .Where(static column => !string.IsNullOrWhiteSpace(column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ContainsColumn(IReadOnlyList<string> columns, string column)
    {
        return columns.Any(candidate =>
            string.Equals(candidate, column, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(GetColumnName(candidate), GetColumnName(column), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAlias(string column)
    {
        var separatorIndex = column.IndexOf('.', StringComparison.Ordinal);
        return separatorIndex < 0 ? string.Empty : column[..separatorIndex];
    }

    private static string GetColumnName(string column)
    {
        var separatorIndex = column.LastIndexOf('.');
        return separatorIndex < 0 ? column : column[(separatorIndex + 1)..];
    }

    private static string FormatKindPrefix(BoundaryRowShapeKind kind)
    {
        return kind switch
        {
            BoundaryRowShapeKind.Sort => "sort", BoundaryRowShapeKind.Aggregate => "aggregate",
            BoundaryRowShapeKind.TopN => "top-n",
            BoundaryRowShapeKind.TopOffset => "top-offset",
            BoundaryRowShapeKind.Distinct => "distinct", BoundaryRowShapeKind.Window => "window",
            BoundaryRowShapeKind.SetOperation => "setoperation",
            BoundaryRowShapeKind.HashJoinBuild => "hash-join-build", BoundaryRowShapeKind.HashJoinProbe => "hash-join-probe",
            BoundaryRowShapeKind.CteMaterialization => "cte",
            _ => kind.ToString()
        };
    }
}
