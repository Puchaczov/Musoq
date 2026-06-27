using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public abstract class IrExpressionVisitor<T>
{
    public T Visit(IrExpression expression)
    {
        return expression switch
        {
            ColumnRef node => VisitColumnRef(node),
            ScriptParameterRef node => VisitScriptParameterRef(node),
            ScriptVariableRef node => VisitScriptVariableRef(node),
            Literal node => VisitLiteral(node),
            WildcardLiteral node => VisitWildcardLiteral(node),
            BinaryOp node => VisitBinaryOp(node),
            UnaryOp node => VisitUnaryOp(node),
            MethodCall node => VisitMethodCall(node),
            StrictCast node => VisitStrictCast(node),
            IsNullCheck node => VisitIsNullCheck(node),
            RowPresence node => VisitRowPresence(node),
            InCheck node => VisitInCheck(node),
            CollectionInCheck node => VisitCollectionInCheck(node),
            PatternMatch node => VisitPatternMatch(node),
            Between node => VisitBetween(node),
            CaseWhen node => VisitCaseWhen(node),
            Coalesce node => VisitCoalesce(node),
            AggregateRef node => VisitAggregateRef(node),
            WindowFunctionRef node => VisitWindowFunctionRef(node),
            ArrayAccess node => VisitArrayAccess(node),
            CteTableRef node => VisitCteTableRef(node),
            _ => throw UnsupportedShape.Of($"IR expression type '{expression.GetType().Name}'")
        };
    }

    protected abstract T VisitColumnRef(ColumnRef node);
    protected abstract T VisitScriptParameterRef(ScriptParameterRef node);
    protected abstract T VisitScriptVariableRef(ScriptVariableRef node);
    protected abstract T VisitLiteral(Literal node);
    protected abstract T VisitWildcardLiteral(WildcardLiteral node);
    protected abstract T VisitBinaryOp(BinaryOp node);
    protected abstract T VisitUnaryOp(UnaryOp node);
    protected abstract T VisitMethodCall(MethodCall node);
    protected abstract T VisitStrictCast(StrictCast node);
    protected abstract T VisitIsNullCheck(IsNullCheck node);
    protected abstract T VisitRowPresence(RowPresence node);
    protected abstract T VisitInCheck(InCheck node);
    protected abstract T VisitCollectionInCheck(CollectionInCheck node);
    protected abstract T VisitPatternMatch(PatternMatch node);
    protected abstract T VisitBetween(Between node);
    protected abstract T VisitCaseWhen(CaseWhen node);
    protected abstract T VisitCoalesce(Coalesce node);
    protected abstract T VisitAggregateRef(AggregateRef node);
    protected abstract T VisitWindowFunctionRef(WindowFunctionRef node);
    protected abstract T VisitArrayAccess(ArrayAccess node);
    protected abstract T VisitCteTableRef(CteTableRef node);
}
