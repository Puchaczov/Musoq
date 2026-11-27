using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class NumericLiteralNode : ConstantValueNode
{
    private bool _includeTypeInExpression;

    public NumericLiteralNode Copy() => CopyForBinding(_includeTypeInExpression);

    public NumericLiteralNode CopyForBinding(bool includeType)
    {
        var copy = CreateCopy();
        copy._includeTypeInExpression = includeType;
        return copy;
    }

    protected abstract NumericLiteralNode CreateCopy();

    internal static string FormatOperand(Node operand) =>
        operand is NumericLiteralNode { _includeTypeInExpression: true } numeric
            ? NumericLiteralText.Format(numeric.ObjValue) ?? numeric.ToString()
            : operand.ToString();
}
