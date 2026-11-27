using Musoq.Parser.Helpers;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class IrExpressionPrinter
{
    protected override string VisitLiteral(Literal node)
    {
        if (!string.IsNullOrWhiteSpace(node.DisplayName))
            return node.DisplayName;

        if (node.Value is null)
            return "NULL";

        if (node.Value is string s)
            return $"'{s.Escape()}'";

        if (node.Value is bool b)
            return b ? "TRUE" : "FALSE";

        if (ForBinding && NumericLiteralText.Format(node.Value) is { } numericText)
            return numericText;

        return node.Value.ToString() ?? "NULL";
    }
}
