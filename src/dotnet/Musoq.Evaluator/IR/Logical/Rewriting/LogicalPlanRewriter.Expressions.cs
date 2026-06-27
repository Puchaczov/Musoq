using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical.Rewriting;

internal static partial class LogicalPlanRewriter
{
    public static IrExpression[] RewriteExpressions(
        IReadOnlyList<IrExpression> expressions,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new IrExpression[expressions.Count];
        changed = false;

        for (var index = 0; index < expressions.Count; index++)
        {
            rewritten[index] = rewriteExpression(expressions[index]);
            changed |= !ReferenceEquals(rewritten[index], expressions[index]);
        }

        return rewritten;
    }

    public static AggregateBinding[] RewriteAggregateBindings(
        AggregateBinding[] bindings,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new AggregateBinding[bindings.Length];
        changed = false;

        for (var index = 0; index < bindings.Length; index++)
        {
            var binding = bindings[index];
            var setArguments = RewriteExpressions(binding.SetArguments, rewriteExpression, out var setChanged);
            var getArguments = RewriteExpressions(binding.GetArguments, rewriteExpression, out var getChanged);
            rewritten[index] = setChanged || getChanged
                ? binding with { SetArguments = setArguments, GetArguments = getArguments }
                : binding;
            changed |= !ReferenceEquals(rewritten[index], binding);
        }

        return rewritten;
    }

    public static ProjectedField[] RewriteProjectedFields(
        ProjectedField[] fields,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new ProjectedField[fields.Length];
        changed = false;

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var expression = rewriteExpression(field.Expression);
            rewritten[index] = ReferenceEquals(expression, field.Expression)
                ? field
                : field with { Expression = expression };
            changed |= !ReferenceEquals(rewritten[index], field);
        }

        return rewritten;
    }

    public static OrderField[] RewriteOrderFields(
        OrderField[] fields,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new OrderField[fields.Length];
        changed = false;

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var expression = rewriteExpression(field.Expression);
            rewritten[index] = ReferenceEquals(expression, field.Expression)
                ? field
                : field with { Expression = expression };
            changed |= !ReferenceEquals(rewritten[index], field);
        }

        return rewritten;
    }

    public static IReadOnlyList<ValuesScanRow> RewriteValuesRows(
        IReadOnlyList<ValuesScanRow> rows,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new ValuesScanRow[rows.Count];
        changed = false;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var fields = new ValuesScanField[row.Fields.Count];
            var rowChanged = false;

            for (var fieldIndex = 0; fieldIndex < row.Fields.Count; fieldIndex++)
            {
                var field = row.Fields[fieldIndex];
                var value = rewriteExpression(field.Value);
                fields[fieldIndex] = ReferenceEquals(value, field.Value)
                    ? field
                    : field with { Value = value };
                rowChanged |= !ReferenceEquals(fields[fieldIndex], field);
            }

            rewritten[rowIndex] = rowChanged ? new ValuesScanRow(fields) : row;
            changed |= rowChanged;
        }

        return rewritten;
    }

    public static WindowRegistration[] RewriteWindowRegistrations(
        WindowRegistration[] registrations,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new WindowRegistration[registrations.Length];
        changed = false;

        for (var index = 0; index < registrations.Length; index++)
        {
            var registration = registrations[index];
            var partitionKeys = RewriteExpressions(registration.PartitionKeys, rewriteExpression, out var partitionChanged);
            var orderKeys = RewriteOrderFields(registration.OrderKeys, rewriteExpression, out var orderChanged);
            var valueArguments = RewriteExpressions(registration.ValueArguments, rewriteExpression, out var valueChanged);
            rewritten[index] = partitionChanged || orderChanged || valueChanged
                ? registration with
                {
                    PartitionKeys = partitionKeys,
                    OrderKeys = orderKeys,
                    ValueArguments = valueArguments
                }
                : registration;
            changed |= !ReferenceEquals(rewritten[index], registration);
        }

        return rewritten;
    }
}
