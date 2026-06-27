using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    private static bool TryMapEqualityCondition(
        BinaryOp equality,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases,
        List<IrExpression> leftKeys,
        List<IrExpression> rightKeys,
        bool allowConstantKeys)
    {
        var leftColumns = ColumnRefExtractor.Extract(equality.Left);
        var rightColumns = ColumnRefExtractor.Extract(equality.Right);

        var leftHasLeft = ReferencesAliases(leftColumns, leftAliases);
        var leftHasRight = ReferencesAliases(leftColumns, rightAliases);
        var rightHasLeft = ReferencesAliases(rightColumns, leftAliases);
        var rightHasRight = ReferencesAliases(rightColumns, rightAliases);

        var leftIsLeft = leftHasLeft && !leftHasRight;
        var leftIsRight = leftHasRight && !leftHasLeft;
        var leftIsConstant = !leftHasLeft && !leftHasRight;

        var rightIsLeft = rightHasLeft && !rightHasRight;
        var rightIsRight = rightHasRight && !rightHasLeft;
        var rightIsConstant = !rightHasLeft && !rightHasRight;

        if (leftIsConstant && rightIsConstant)
            return false;

        if (!allowConstantKeys && (leftIsConstant || rightIsConstant))
            return false;

        IrExpression? leftExpression = null;
        IrExpression? rightExpression = null;

        if (leftIsLeft && rightIsRight)
        {
            leftExpression = equality.Left;
            rightExpression = equality.Right;
        }
        else if (leftIsRight && rightIsLeft)
        {
            leftExpression = equality.Right;
            rightExpression = equality.Left;
        }
        else if (leftIsConstant && rightIsRight)
        {
            leftExpression = equality.Left;
            rightExpression = equality.Right;
        }
        else if (leftIsConstant && rightIsLeft)
        {
            leftExpression = equality.Right;
            rightExpression = equality.Left;
        }
        else if (rightIsConstant && leftIsLeft)
        {
            leftExpression = equality.Left;
            rightExpression = equality.Right;
        }
        else if (rightIsConstant && leftIsRight)
        {
            leftExpression = equality.Right;
            rightExpression = equality.Left;
        }

        if (leftExpression == null || rightExpression == null)
            return false;

        var leftType = Nullable.GetUnderlyingType(leftExpression.ReturnType) ?? leftExpression.ReturnType;
        var rightType = Nullable.GetUnderlyingType(rightExpression.ReturnType) ?? rightExpression.ReturnType;

        if (leftType != rightType)
            return false;

        leftKeys.Add(leftExpression);
        rightKeys.Add(rightExpression);
        return true;
    }

    private static bool ReferencesAliases(IReadOnlyList<ColumnRef> columns, HashSet<string> aliases)
    {
        foreach (var column in columns)
        {
            if (aliases.Contains(column.Alias))
                return true;
        }

        return false;
    }

    private static HashSet<string> CollectAliases(PhysicalNode node)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAliases(node, aliases);
        return aliases;
    }

    private static void AddAliases(PhysicalNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case PhysicalSchemaScanNode scan:
                aliases.Add(scan.Alias);
                break;
            case PhysicalInterpretSourceNode interpret:
                aliases.Add(interpret.Alias);
                break;
            case PhysicalCteRefNode cteRef:
                aliases.Add(cteRef.Alias);
                break;
            case PhysicalValuesScanNode values:
                aliases.Add(values.Alias);
                break;
            case PhysicalUnpivotNode unpivot:
                aliases.Add(unpivot.Alias);
                break;
            case PhysicalPropertySourceNode property:
                aliases.Add(property.Alias);
                break;
            case PhysicalAccessMethodSourceNode accessMethod:
                aliases.Add(accessMethod.Alias);
                break;
        }

        foreach (var child in node.Children)
            AddAliases(child, aliases);
    }
}
