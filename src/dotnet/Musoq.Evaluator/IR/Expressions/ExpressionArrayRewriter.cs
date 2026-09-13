using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public abstract partial class ExpressionArrayRewriter : IrExpressionVisitor<IrExpression>
{
    protected IrExpression[] RewriteExpressions(IReadOnlyList<IrExpression> expressions, out bool changed)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        changed = false;

        if (expressions.Count == 0)
            return [];

        var rewritten = new IrExpression[expressions.Count];

        for (var index = 0; index < expressions.Count; index++)
        {
            rewritten[index] = Visit(expressions[index]);

            if (!ReferenceEquals(rewritten[index], expressions[index]))
                changed = true;
        }

        return rewritten;
    }

    protected CaseWhenBranch[] RewriteBranches(IReadOnlyList<CaseWhenBranch> branches, out bool changed)
    {
        ArgumentNullException.ThrowIfNull(branches);
        changed = false;

        if (branches.Count == 0)
            return [];

        var rewritten = new CaseWhenBranch[branches.Count];

        for (var index = 0; index < branches.Count; index++)
        {
            var branch = branches[index];
            var condition = Visit(branch.Condition);
            var result = Visit(branch.Result);

            rewritten[index] = ReferenceEquals(condition, branch.Condition) && ReferenceEquals(result, branch.Result)
                ? branch
                : new CaseWhenBranch(condition, result);

            if (!ReferenceEquals(rewritten[index], branch))
                changed = true;
        }

        return rewritten;
    }

    protected override IrExpression VisitArrayAccess(ArrayAccess node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var array = Visit(node.Array);
        var index = Visit(node.Index);
        return ReferenceEquals(array, node.Array) && ReferenceEquals(index, node.Index)
            ? node
            : new ArrayAccess(array, index, node.ElementType, node.ReturnType);
    }

    protected override IrExpression VisitStrictCast(StrictCast node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : node with { Expression = expression };
    }

    protected override IrExpression VisitRowPresence(RowPresence node)
    {
        return node;
    }

    protected override IrExpression VisitCteCollectionInput(CteCollectionInput node) => node;
    protected override IrExpression VisitStructuralRecordLiteral(StructuralRecordLiteral node)
    {
        var fields = new StructuralFieldExpression[node.Fields.Count];
        var changed = false;
        for (var index = 0; index < node.Fields.Count; index++)
        {
            var field = node.Fields[index];
            var value = Visit(field.Value);
            fields[index] = ReferenceEquals(value, field.Value) ? field : new StructuralFieldExpression(field.Name, value);
            changed |= !ReferenceEquals(value, field.Value);
        }

        return changed ? new StructuralRecordLiteral(node.ReturnType, fields, node.TargetType) : node;
    }

    protected override IrExpression VisitStructuralArrayLiteral(StructuralArrayLiteral node)
    {
        var elements = new IrExpression[node.Elements.Count];
        var changed = false;
        for (var index = 0; index < node.Elements.Count; index++)
        {
            elements[index] = Visit(node.Elements[index]);
            changed |= !ReferenceEquals(elements[index], node.Elements[index]);
        }

        return changed ? new StructuralArrayLiteral(node.ReturnType, node.ElementType, elements) : node;
    }

    protected override IrExpression VisitStructuralConversion(StructuralConversion node)
    {
        var value = Visit(node.Value);
        return ReferenceEquals(value, node.Value) ? node : new StructuralConversion(value, node.TargetType);
    }}
