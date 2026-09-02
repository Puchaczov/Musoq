using System.Text;

namespace Musoq.Evaluator.IR.Expressions;
public sealed partial class IrExpressionPrinter : IrExpressionVisitor<string>
{
    public static string Print(IrExpression expression)
    {
        var printer = new IrExpressionPrinter();
        return printer.Visit(expression);
    }

    protected override string VisitColumnRef(ColumnRef node)
    {
        return string.IsNullOrEmpty(node.Alias)
            ? node.ColumnName
            : $"{node.Alias}.{node.ColumnName}";
    }

    protected override string VisitLiteral(Literal node)
    {
        if (!string.IsNullOrWhiteSpace(node.DisplayName))
            return node.DisplayName;

        if (node.Value is null)
            return "NULL";

        if (node.Value is string s)
            return $"'{s}'";

        if (node.Value is bool b)
            return b ? "TRUE" : "FALSE";

        return node.Value.ToString() ?? "NULL";
    }

    protected override string VisitScriptParameterRef(ScriptParameterRef node)
    {
        return $"${node.Name}";
    }
    protected override string VisitScriptVariableRef(ScriptVariableRef node)
    {
        return $"${node.Name}";
    }

    protected override string VisitWildcardLiteral(WildcardLiteral node)
    {
        return "*";
    }

    protected override string VisitBinaryOp(BinaryOp node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        var op = FormatBinaryOperator(node.Kind);
        return $"({left} {op} {right})";
    }

    protected override string VisitUnaryOp(UnaryOp node)
    {
        var operand = Visit(node.Operand);
        return node.Kind switch
        {
            UnaryOpKind.Not => $"NOT {operand}",
            UnaryOpKind.Negate => $"-{operand}",
            _ => $"?{operand}"
        };
    }

    protected override string VisitMethodCall(MethodCall node)
    {
        var sb = new StringBuilder();
        sb.Append(node.Method.Name);
        sb.Append('(');
        for (var i = 0; i < node.Arguments.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(Visit(node.Arguments[i]));
        }
        sb.Append(')');
        return sb.ToString();
    }

    protected override string VisitStrictCast(StrictCast node)
    {
        return $"{Visit(node.Expression)}::{node.TargetTypeName}";
    }

    protected override string VisitIsNullCheck(IsNullCheck node)
    {
        var expr = Visit(node.Expression);
        return node.IsNegated
            ? $"{expr} IS NOT NULL"
            : $"{expr} IS NULL";
    }

    protected override string VisitRowPresence(RowPresence node)
    {
        return node.IsPresent
            ? $"{node.Alias} IS PRESENT"
            : $"{node.Alias} IS MISSING";
    }

    protected override string VisitInCheck(InCheck node)
    {
        var sb = new StringBuilder();
        sb.Append(Visit(node.Expression));
        sb.Append(node.IsNegated ? " NOT IN (" : " IN (");
        for (var i = 0; i < node.Values.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(Visit(node.Values[i]));
        }
        sb.Append(')');
        return sb.ToString();
    }

    protected override string VisitPatternMatch(PatternMatch node)
    {
        var expr = Visit(node.Expression);
        var pattern = Visit(node.Pattern);
        var keyword = node.Kind == PatternKind.Like ? "LIKE" : "RLIKE";
        return $"{expr} {keyword} {pattern}";
    }

    protected override string VisitBetween(Between node)
    {
        var expr = Visit(node.Expression);
        var low = Visit(node.Low);
        var high = Visit(node.High);
        return $"{expr} BETWEEN {low} AND {high}";
    }

    protected override string VisitCaseWhen(CaseWhen node)
    {
        var sb = new StringBuilder();
        sb.Append("CASE");
        foreach (var branch in node.Branches)
        {
            sb.Append(" WHEN ");
            sb.Append(Visit(branch.Condition));
            sb.Append(" THEN ");
            sb.Append(Visit(branch.Result));
        }
        if (node.ElseExpression is not null)
        {
            sb.Append(" ELSE ");
            sb.Append(Visit(node.ElseExpression));
        }
        sb.Append(" END");
        return sb.ToString();
    }

    protected override string VisitCoalesce(Coalesce node)
    {
        var sb = new StringBuilder();
        sb.Append("COALESCE(");
        for (var i = 0; i < node.Expressions.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(Visit(node.Expressions[i]));
        }
        sb.Append(')');
        return sb.ToString();
    }

    protected override string VisitAggregateRef(AggregateRef node)
    {
        return $"AggRef({node.DisplayName ?? node.Identifier})";
    }

    protected override string VisitWindowFunctionRef(WindowFunctionRef node)
    {
        return $"WindowRef({node.WindowIndex})";
    }

    protected override string VisitCteTableRef(CteTableRef node)
    {
        return node.Name;
    }

    private static string FormatBinaryOperator(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.Add => "+",
            BinaryOpKind.Subtract => "-",
            BinaryOpKind.Multiply => "*",
            BinaryOpKind.Divide => "/",
            BinaryOpKind.Modulo => "%",
            BinaryOpKind.And => "AND",
            BinaryOpKind.Or => "OR",
            BinaryOpKind.Equal => "=",
            BinaryOpKind.NotEqual => "<>",
            BinaryOpKind.IsDistinctFrom => "IS DISTINCT FROM",
            BinaryOpKind.IsNotDistinctFrom => "IS NOT DISTINCT FROM",
            BinaryOpKind.GreaterThan => ">",
            BinaryOpKind.LessThan => "<",
            BinaryOpKind.GreaterOrEqual => ">=",
            BinaryOpKind.LessOrEqual => "<=",
            BinaryOpKind.BitwiseAnd => "&",
            BinaryOpKind.BitwiseOr => "|",
            BinaryOpKind.BitwiseXor => "^",
            BinaryOpKind.LeftShift => "<<",
            BinaryOpKind.RightShift => ">>",
            BinaryOpKind.StringConcatenate => "||",
            _ => "?"
        };
    }

    protected override string VisitArrayAccess(ArrayAccess node)
    {
        var arrayStr = Visit(node.Array);
        var indexStr = Visit(node.Index);
        return $"{arrayStr}[{indexStr}]";
    }
}
