using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Execution;
public static partial class ExecutionExpressionConverter
{
    private const int DefaultArrayInValueThreshold = 16;
    private const int PrimitiveArrayInValueThreshold = 32;

    public static ExecutionExpression Convert(IrExpression expression, RowShape? sourceShape = null) =>
        Convert(expression, RowShapeLookup.CreateSourceShapeLookupOrEmpty(sourceShape));

    public static ExecutionExpression Convert(IrExpression expression, IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        return Convert(expression, sourceShapes, null);
    }

    public static ExecutionExpression Convert(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes)
    {
        return Convert(expression, sourceShapes, cteTableIndexes, null);
    }

    public static ExecutionExpression Convert(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        var converted = expression switch
        {
            ColumnRef column => ConvertColumnRef(column, sourceShapes),
            ScriptParameterRef parameter => ConvertScriptParameter(parameter),
            ScriptVariableRef variable => new ExecutionScriptVariableRead(variable.Name, variable.ReturnType),
            Literal literal => new ExecutionLiteral(literal.Value, literal.ReturnType),
            WildcardLiteral => new ExecutionLiteral("*", typeof(string)),
            BinaryOp binary => new ExecutionBinary(
                binary.Kind,
                Convert(binary.Left, sourceShapes, cteTableIndexes, methodTargets),
                Convert(binary.Right, sourceShapes, cteTableIndexes, methodTargets),
                binary.ReturnType),
            UnaryOp unary => new ExecutionUnary(unary.Kind, Convert(unary.Operand, sourceShapes, cteTableIndexes, methodTargets), unary.ReturnType),
            MethodCall method => ConvertMethodCall(method, sourceShapes, cteTableIndexes, methodTargets),
            StrictCast strictCast => new ExecutionStrictCast(
                Convert(strictCast.Expression, sourceShapes, cteTableIndexes, methodTargets),
                strictCast.TargetTypeName,
                strictCast.ReturnType),
            ArrayAccess arrayAccess => new ExecutionArrayAccess(
                Convert(arrayAccess.Array, sourceShapes, cteTableIndexes, methodTargets),
                Convert(arrayAccess.Index, sourceShapes, cteTableIndexes, methodTargets),
                arrayAccess.ElementType,
                arrayAccess.ReturnType),
            RowPresence rowPresence => ConvertRowPresence(rowPresence, sourceShapes),
            IsNullCheck isNull => new ExecutionIsNullCheck(
                Convert(isNull.Expression, sourceShapes, cteTableIndexes, methodTargets),
                isNull.IsNegated,
                isNull.ReturnType),
            InCheck inCheck => new ExecutionInCheck(
                Convert(inCheck.Expression, sourceShapes, cteTableIndexes, methodTargets),
                inCheck.Values.Select(value => Convert(value, sourceShapes, cteTableIndexes, methodTargets)).ToArray(),
                inCheck.ReturnType,
                TryCreateConstantInSet(inCheck)),
            CollectionInCheck collectionInCheck => ConvertCollectionInCheck(collectionInCheck, sourceShapes, cteTableIndexes, methodTargets),
            PatternMatch patternMatch => new ExecutionPatternMatch(
                Convert(patternMatch.Expression, sourceShapes, cteTableIndexes, methodTargets),
                Convert(patternMatch.Pattern, sourceShapes, cteTableIndexes, methodTargets),
                patternMatch.Kind,
                patternMatch.ReturnType),
            Between between => new ExecutionBetween(
                Convert(between.Expression, sourceShapes, cteTableIndexes, methodTargets),
                Convert(between.Low, sourceShapes, cteTableIndexes, methodTargets),
                Convert(between.High, sourceShapes, cteTableIndexes, methodTargets),
                between.ReturnType),
            CaseWhen caseWhen => new ExecutionCaseWhen(
                caseWhen.Branches
                    .Select(branch => new ExecutionCaseWhenBranch(
                        Convert(branch.Condition, sourceShapes, cteTableIndexes, methodTargets),
                        Convert(branch.Result, sourceShapes, cteTableIndexes, methodTargets)))
                    .ToArray(),
                caseWhen.ElseExpression == null
                    ? null
                    : Convert(caseWhen.ElseExpression, sourceShapes, cteTableIndexes, methodTargets),
                caseWhen.ReturnType),
            Coalesce coalesce => new ExecutionCoalesce(
                coalesce.Expressions.Select(value => Convert(value, sourceShapes, cteTableIndexes, methodTargets)).ToArray(),
                coalesce.ReturnType),
            AggregateRef aggregateRef => new ExecutionAggregateResultRef(aggregateRef.Identifier, aggregateRef.DisplayName, aggregateRef.ReturnType),
            WindowFunctionRef windowRef => new ExecutionWindowResultRef(windowRef.WindowIndex, windowRef.ReturnType),
            CteTableRef cteTableRef when cteTableIndexes?.TryGetValue(cteTableRef.Name, out var tableIndex) == true => new ExecutionStoredTable(tableIndex),
            CteTableRef cteTableRef => throw Unsupported(expression, $"CTE table '{cteTableRef.Name}' requires a registered table index"),
            _ => throw Unsupported(expression, "no execution expression lowering is registered")
        };

        return converted;
    }
}
