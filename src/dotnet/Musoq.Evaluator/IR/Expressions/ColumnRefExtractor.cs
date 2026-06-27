using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ColumnRefExtractor : IrExpressionVisitor<IReadOnlyList<ColumnRef>>
{
    private readonly List<ColumnRef> _columns = [];

    public static IReadOnlyList<ColumnRef> Extract(IrExpression expression)
    {
        var extractor = new ColumnRefExtractor();
        extractor.Visit(expression);
        return extractor._columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitColumnRef(ColumnRef node)
    {
        _columns.Add(node);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitScriptParameterRef(ScriptParameterRef node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitScriptVariableRef(ScriptVariableRef node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitLiteral(Literal node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitWildcardLiteral(WildcardLiteral node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitBinaryOp(BinaryOp node)
    {
        Visit(node.Left);
        Visit(node.Right);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitUnaryOp(UnaryOp node)
    {
        Visit(node.Operand);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitMethodCall(MethodCall node)
    {
        foreach (var arg in node.Arguments)
            Visit(arg);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitStrictCast(StrictCast node)
    {
        Visit(node.Expression);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitIsNullCheck(IsNullCheck node)
    {
        Visit(node.Expression);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitRowPresence(RowPresence node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitInCheck(InCheck node)
    {
        Visit(node.Expression);
        foreach (var value in node.Values)
            Visit(value);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitPatternMatch(PatternMatch node)
    {
        Visit(node.Expression);
        Visit(node.Pattern);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitBetween(Between node)
    {
        Visit(node.Expression);
        Visit(node.Low);
        Visit(node.High);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitCaseWhen(CaseWhen node)
    {
        foreach (var branch in node.Branches)
        {
            Visit(branch.Condition);
            Visit(branch.Result);
        }

        if (node.ElseExpression is not null)
            Visit(node.ElseExpression);

        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitCoalesce(Coalesce node)
    {
        foreach (var expr in node.Expressions)
            Visit(expr);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitAggregateRef(AggregateRef node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitWindowFunctionRef(WindowFunctionRef node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitCteTableRef(CteTableRef node) => _columns;

    protected override IReadOnlyList<ColumnRef> VisitArrayAccess(ArrayAccess node)
    {
        Visit(node.Array);
        Visit(node.Index);
        return _columns;
    }
}
