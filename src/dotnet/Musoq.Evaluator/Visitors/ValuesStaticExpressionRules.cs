using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ValuesStaticExpressionRules
{
    public static bool IsStaticScalarExpression(Node expression)
    {
        return expression switch
        {
            ConstantValueNode => true,
            NullNode => true,
            ParameterReferenceNode => true,
            ScriptVariableReferenceNode => true,
            BinaryNode binary => IsStaticScalarExpression(binary.Left) &&
                                 IsStaticScalarExpression(binary.Right),
            UnaryNode unary => IsStaticScalarExpression(unary.Expression),
            _ => false
        };
    }
}
