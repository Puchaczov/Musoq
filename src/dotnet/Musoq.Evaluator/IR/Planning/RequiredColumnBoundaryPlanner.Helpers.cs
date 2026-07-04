using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnBoundaryPlanner
{
    private static string[] FilterProducedColumns(PhysicalNode input, IEnumerable<string> columns)
    {
        var available = ResolveAvailableColumns(input);
        return OrderColumns(columns.Where(column => ContainsColumn(available, column)));
    }

    private static string[] ResolveSetOperationColumns(PhysicalNode input, IReadOnlyList<int> indexes)
    {
        var columns = ResolveAvailableColumns(input);
        return indexes
            .Where(index => index >= 0 && index < columns.Length)
            .Select(index => columns[index])
            .ToArray();
    }

    private static string[] ResolveAvailableColumns(PhysicalNode node)
    {
        return node switch
        {
            PhysicalSchemaScanNode scan => ResolveSchemaScanColumns(scan),
            PhysicalCteRefNode cteRef => cteRef.OutputSchema.Columns.Select(column => Qualify(cteRef.Alias, column.Name)).ToArray(),
            PhysicalValuesScanNode values => values.OutputSchema.Columns.Select(column => Qualify(values.Alias, column.Name)).ToArray(),
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
                    .Concat(CollectColumns(registration.ValueArguments))
                    .Concat(OptionalColumns(registration.FilterPredicate)))
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
            .SelectMany(binding => CollectColumns(binding.SetArguments)
                .Concat(OptionalColumns(binding.FilterPredicate))
                .Concat(CollectColumns(binding.GetArguments)))
            .ToArray();
    }

    private static string[] CollectOrderColumns(IReadOnlyList<OrderField> keys)
    {
        return keys.SelectMany(static key => CollectColumns(key.Expression)).ToArray();
    }

    private static string[] CollectColumns(IEnumerable<IrExpression> expressions)
    {
        return expressions.SelectMany(CollectColumns).ToArray();
    }

    private static string[] CollectColumns(IrExpression expression)
    {
        return ColumnRefExtractor.Extract(expression).Select(FormatColumn).ToArray();
    }

    private static string[] OptionalColumns(IrExpression? expression)
    {
        return expression == null ? [] : CollectColumns(expression);
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

    private static string[] CreateMappings(IEnumerable<string> columns)
    {
        return OrderColumns(columns)
            .Select(static column => $"{column}->{GetColumnName(column)}")
            .ToArray();
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

    private static string GetColumnName(string column)
    {
        var separatorIndex = column.LastIndexOf('.');
        return separatorIndex < 0 ? column : column[(separatorIndex + 1)..];
    }
}
