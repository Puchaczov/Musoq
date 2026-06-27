using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionExpressionFingerprint
{
    internal static string ForWindowExpressionList(IEnumerable<IrExpression> expressions)
    {
        return string.Join("|", expressions.Select(ForWindowExpression));
    }

    internal static string ForWindowExpression(IrExpression expression)
    {
        var expressionText = IrExpressionPrinter.Print(expression);
        var returnType = expression.ReturnType.FullName ?? expression.ReturnType.Name;

        return string.Concat(returnType, "#", expressionText.Length.ToString(CultureInfo.InvariantCulture), ":", expressionText);
    }
}
