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
    private static PhysicalNode PruneProjectInput(PhysicalNode input, IReadOnlyList<IrExpression> expressions)
    {
        return expressions.Any(ContainsSourceInjectedMethod) ? input : PruneProjectInputCore(input, expressions);
    }

    private static PhysicalNode PruneProjectInputCore(PhysicalNode input, IReadOnlyList<IrExpression> expressions)
    {
        return input switch
        {
            PhysicalFilterNode filter => new PhysicalFilterNode(
                filter.Predicate,
                PruneProjectInputCore(filter.Input, [..expressions, filter.Predicate])),
            PhysicalSchemaScanNode scan => PruneSchemaScan(scan, expressions),
            _ => input
        };
    }

    private static PhysicalSchemaScanNode PruneSchemaScan(
        PhysicalSchemaScanNode scan,
        IReadOnlyList<IrExpression> expressions)
    {
        var required = expressions
            .SelectMany(ColumnRefExtractor.Extract)
            .Where(column => string.IsNullOrWhiteSpace(column.Alias) ||
                             string.Equals(column.Alias, scan.Alias, StringComparison.OrdinalIgnoreCase))
            .Select(column => GetColumnRoot(NormalizeColumnName(column.ColumnName, scan.Alias)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (required.Count == 0)
            return scan;

        var columns = scan.OutputSchema.Columns
            .Where(column => required.Contains(column.Name))
            .ToArray();

        return columns.Length == 0 || columns.Length == scan.OutputSchema.Columns.Length
            ? scan
            : scan with
            {
                OutputSchema = new OutputSchema(columns),
                ProjectedColumns = columns.Select(static column => column.Name).ToArray()
            };
    }

    private static bool ContainsSourceInjectedMethod(IrExpression expression)
    {
        return expression switch
        {
            MethodCall method when !string.IsNullOrWhiteSpace(method.Alias) => true,
            MethodCall method => method.Arguments.Any(ContainsSourceInjectedMethod),
            BinaryOp binary => ContainsSourceInjectedMethod(binary.Left) || ContainsSourceInjectedMethod(binary.Right),
            UnaryOp unary => ContainsSourceInjectedMethod(unary.Operand),
            IsNullCheck isNull => ContainsSourceInjectedMethod(isNull.Expression),
            InCheck check => ContainsSourceInjectedMethod(check.Expression) || check.Values.Any(ContainsSourceInjectedMethod),
            PatternMatch match => ContainsSourceInjectedMethod(match.Expression) || ContainsSourceInjectedMethod(match.Pattern),
            Between between => ContainsSourceInjectedMethod(between.Expression) ||
                               ContainsSourceInjectedMethod(between.Low) ||
                               ContainsSourceInjectedMethod(between.High),
            CaseWhen caseWhen => caseWhen.Branches.Any(branch =>
                                     ContainsSourceInjectedMethod(branch.Condition) ||
                                     ContainsSourceInjectedMethod(branch.Result)) ||
                                 (caseWhen.ElseExpression != null &&
                                  ContainsSourceInjectedMethod(caseWhen.ElseExpression)),
            Coalesce coalesce => coalesce.Expressions.Any(ContainsSourceInjectedMethod),
            ArrayAccess access => ContainsSourceInjectedMethod(access.Array) || ContainsSourceInjectedMethod(access.Index),
            _ => false
        };
    }
}
