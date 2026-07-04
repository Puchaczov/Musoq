using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class PhysicalColumnUsageFacts
{
    public static HashSet<string> CollectAggregateRequiredNames(
        IReadOnlyList<AggregateBinding> bindings)
    {
        return CollectAggregateRequiredNames([], bindings);
    }

    public static HashSet<string> CollectAggregateRequiredNames(
        IReadOnlyList<IrExpression> groupKeys,
        IReadOnlyList<AggregateBinding> bindings)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddExpressionColumns(names, groupKeys);
        foreach (var binding in bindings)
        {
            AddExpressionColumns(names, binding.SetArguments);
            if (binding.FilterPredicate != null)
                AddExpressionColumns(names, binding.FilterPredicate);
            AddExpressionColumns(names, binding.GetArguments);
        }

        return names;
    }

    public static HashSet<string> CollectReferencedNames(
        IReadOnlyList<ProjectedField> fields)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
            AddExpressionColumns(names, field.Expression);

        return names;
    }

    public static ColumnRef[] CollectReferencedColumns(IReadOnlyList<ProjectedField> fields)
    {
        return fields
            .SelectMany(static field => ColumnRefExtractor.Extract(field.Expression))
            .ToArray();
    }

    public static ColumnRef[] CollectReferencedColumns(IReadOnlyList<IrExpression> expressions)
    {
        return expressions
            .SelectMany(ColumnRefExtractor.Extract)
            .ToArray();
    }

    public static ColumnRef[] CollectReferencedColumns(IrExpression expression)
    {
        return ColumnRefExtractor.Extract(expression).ToArray();
    }

    public static void AddOrderColumns(HashSet<string> names, IReadOnlyList<OrderField> keys)
    {
        foreach (var key in keys)
            AddExpressionColumns(names, key.Expression);
    }

    public static void AddExpressionColumns(HashSet<string> names, IrExpression expression)
    {
        foreach (var column in ColumnRefExtractor.Extract(expression))
            names.Add(column.ColumnName);
    }

    public static void AddExpressionColumns(HashSet<string> names, IEnumerable<IrExpression> expressions)
    {
        foreach (var expression in expressions)
            AddExpressionColumns(names, expression);
    }

    public static void AddWindowRegistrationColumns(
        HashSet<string> names,
        IReadOnlyList<WindowRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            AddExpressionColumns(names, registration.PartitionKeys);
            AddExpressionColumns(names, registration.OrderKeys.Select(static key => key.Expression));
            AddExpressionColumns(names, registration.ValueArguments);
            if (registration.FilterPredicate != null)
                AddExpressionColumns(names, registration.FilterPredicate);
        }
    }

    public static HashSet<string> CollectRequiredNamesForSide(
        PhysicalNode side,
        IReadOnlyList<ColumnRef> columns)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            if (string.IsNullOrWhiteSpace(column.Alias) ||
                ProducesAlias(side, column.Alias))
            {
                names.Add(column.ColumnName);
            }
        }

        return names;
    }

    public static bool ProducesAlias(PhysicalNode node, string alias)
    {
        return node switch
        {
            PhysicalSchemaScanNode scan => NameEquals(scan.Alias, alias),
            PhysicalCteRefNode cteRef => NameEquals(cteRef.Alias, alias),
            PhysicalValuesScanNode values => NameEquals(values.Alias, alias),
            _ => node.Children.Any(child => ProducesAlias(child, alias))
        };
    }

    public static bool TrySelectSetOperationRetainedIndexes(
        PhysicalSetOperationNode setOperation,
        IReadOnlySet<string> requiredNames,
        out int[] retainedIndexes)
    {
        retainedIndexes = [];

        var leftColumns = setOperation.Left.OutputSchema.Columns;
        if (leftColumns.Length == 0 ||
            requiredNames.Count == 0 ||
            requiredNames.Any(name => !leftColumns.Any(column => NameEquals(column.Name, name))))
        {
            return false;
        }

        var retained = new SortedSet<int>();
        for (var index = 0; index < leftColumns.Length; index++)
        {
            if (requiredNames.Contains(leftColumns[index].Name))
                retained.Add(index);
        }

        if (setOperation.Kind != SetOpKind.UnionAll)
        {
            foreach (var fieldIndex in setOperation.FieldIndexes)
                retained.Add(fieldIndex);
        }

        if (retained.Count == 0 || retained.Count >= leftColumns.Length)
            return false;

        retainedIndexes = retained.ToArray();
        return true;
    }

    public static string[] CollectColumnNames(IEnumerable<IrExpression> expressions)
    {
        return expressions.SelectMany(CollectColumnNames).ToArray();
    }

    public static string[] CollectColumnNames(IrExpression expression)
    {
        return ColumnRefExtractor.Extract(expression)
            .Select(static column => string.IsNullOrWhiteSpace(column.Alias)
                ? column.ColumnName
                : $"{column.Alias}.{column.ColumnName}")
            .ToArray();
    }

    public static string[] FilterProducedColumns(PhysicalNode input, IEnumerable<string> columns)
    {
        var available = ResolveAvailableColumnNames(input);
        return columns.Where(column => ContainsColumn(available, column)).ToArray();
    }

    public static string[] ResolveAvailableColumnNames(PhysicalNode input)
    {
        return input switch
        {
            PhysicalCteRefNode cteRef => cteRef.OutputSchema.Columns
                .Select(column => $"{cteRef.Alias}.{column.Name}")
                .ToArray(),
            PhysicalSchemaScanNode scan => scan.OutputSchema.Columns
                .Select(column => $"{scan.Alias}.{column.Name}")
                .ToArray(),
            PhysicalValuesScanNode values => values.OutputSchema.Columns
                .Select(column => $"{values.Alias}.{column.Name}")
                .ToArray(),
            PhysicalProjectNode project => project.OutputSchema.Columns
                .Select(static column => column.Name)
                .ToArray(),
            _ => SchemaColumns(input.OutputSchema)
        };
    }

    public static string[] ResolveSetOperationColumns(PhysicalNode input, IReadOnlyList<int> indexes)
    {
        var columns = SchemaColumns(input.OutputSchema);
        return indexes
            .Where(index => index >= 0 && index < columns.Length)
            .Select(index => columns[index])
            .ToArray();
    }

    public static string[] SchemaColumns(OutputSchema schema)
    {
        return schema.Columns.Select(static column => column.Name).ToArray();
    }

    public static bool HasAmbiguousOutputNames(IReadOnlyList<ProjectedField> fields)
    {
        return fields
            .GroupBy(static field => field.OutputName, StringComparer.OrdinalIgnoreCase)
            .Any(static group => group.Count() > 1);
    }

    public static bool NameEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static string[] Merge(IEnumerable<string> left, IEnumerable<string> right)
    {
        return left.Concat(right)
            .Where(static column => !string.IsNullOrWhiteSpace(column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool ContainsColumn(IReadOnlyList<string> columns, string column)
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

