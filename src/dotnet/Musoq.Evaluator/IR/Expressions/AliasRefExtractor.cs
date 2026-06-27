using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AliasRefExtractor : IrExpressionVisitor<IReadOnlyList<string>>
{
    private readonly HashSet<string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Extract(IrExpression expression)
    {
        var extractor = new AliasRefExtractor();
        extractor.Visit(expression);
        return extractor._aliases
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    protected override IReadOnlyList<string> VisitColumnRef(ColumnRef node)
    {
        if (!string.IsNullOrWhiteSpace(node.Alias))
            _aliases.Add(node.Alias);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitRowPresence(RowPresence node)
    {
        if (!string.IsNullOrWhiteSpace(node.Alias))
            _aliases.Add(node.Alias);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitScriptParameterRef(ScriptParameterRef node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitScriptVariableRef(ScriptVariableRef node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitLiteral(Literal node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitWildcardLiteral(WildcardLiteral node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitBinaryOp(BinaryOp node)
    {
        Visit(node.Left);
        Visit(node.Right);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitUnaryOp(UnaryOp node)
    {
        Visit(node.Operand);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitMethodCall(MethodCall node)
    {
        foreach (var argument in node.Arguments)
            Visit(argument);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitStrictCast(StrictCast node)
    {
        Visit(node.Expression);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitIsNullCheck(IsNullCheck node)
    {
        Visit(node.Expression);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitInCheck(InCheck node)
    {
        Visit(node.Expression);
        foreach (var value in node.Values)
            Visit(value);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitPatternMatch(PatternMatch node)
    {
        Visit(node.Expression);
        Visit(node.Pattern);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitBetween(Between node)
    {
        Visit(node.Expression);
        Visit(node.Low);
        Visit(node.High);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitCaseWhen(CaseWhen node)
    {
        foreach (var branch in node.Branches)
        {
            Visit(branch.Condition);
            Visit(branch.Result);
        }

        if (node.ElseExpression is not null)
            Visit(node.ElseExpression);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitCoalesce(Coalesce node)
    {
        foreach (var expression in node.Expressions)
            Visit(expression);

        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitAggregateRef(AggregateRef node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitWindowFunctionRef(WindowFunctionRef node) => _aliases.ToArray();

    protected override IReadOnlyList<string> VisitArrayAccess(ArrayAccess node)
    {
        Visit(node.Array);
        Visit(node.Index);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitCteTableRef(CteTableRef node) => _aliases.ToArray();
}
