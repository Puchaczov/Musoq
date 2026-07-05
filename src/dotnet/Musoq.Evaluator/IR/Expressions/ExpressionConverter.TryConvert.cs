using Musoq.Parser.Nodes;
namespace Musoq.Evaluator.IR.Expressions;
public sealed partial class ExpressionConverter
{
    internal ExpressionConversionResult TryConvert(Node node)
    {
        try { return ExpressionConversionResult.Success(Convert(node)); } catch (UnsupportedIrShapeException exception) { return ExpressionConversionResult.Unsupported(exception.Message); }
    }
}
internal sealed record ExpressionConversionResult(bool IsSupported, IrExpression Value, string UnsupportedReason)
{
    public static ExpressionConversionResult Success(IrExpression value) => new(true, value, string.Empty);
    public static ExpressionConversionResult Unsupported(string reason) => new(false, null!, reason);
}
internal sealed class UnsupportedIrShapeException(string message) : NotSupportedException(message);
