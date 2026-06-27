using System.Text;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;
public static partial class PhysicalPlanPrinter
{
    private static string FormatBinaryOperator(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.Equal => "=",
            BinaryOpKind.NotEqual => "<>",
            BinaryOpKind.IsDistinctFrom => "IS DISTINCT FROM",
            BinaryOpKind.IsNotDistinctFrom => "IS NOT DISTINCT FROM",
            BinaryOpKind.GreaterThan => ">",
            BinaryOpKind.GreaterOrEqual => ">=",
            BinaryOpKind.LessThan => "<",
            BinaryOpKind.LessOrEqual => "<=",
            BinaryOpKind.And => "AND",
            BinaryOpKind.Or => "OR",
            BinaryOpKind.Add => "+",
            BinaryOpKind.Subtract => "-",
            BinaryOpKind.Multiply => "*",
            BinaryOpKind.Divide => "/",
            BinaryOpKind.Modulo => "%",
            _ => kind.ToString()
        };
    }
    private static void AppendPushedPredicates(StringBuilder sb, IrExpression[] pushedPredicates)
    {
        if (pushedPredicates.Length == 0)
            return;

        sb.Append(" [pushdown: ");
        PlanPrinterHelpers.AppendExpressions(sb, pushedPredicates);
        sb.Append(']');
    }
}
