using System.Collections;
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
            ParameterReferenceNode parameter => !IsCollectionParameter(parameter),
            ScriptVariableReferenceNode => true,
            BinaryNode binary => IsStaticScalarExpression(binary.Left) &&
                                 IsStaticScalarExpression(binary.Right),
            UnaryNode unary => IsStaticScalarExpression(unary.Expression),
            _ => false
        };
    }

    public static bool IsCollectionParameter(ParameterReferenceNode parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var type = parameter.ReturnType;
        return type != null && type != typeof(string) &&
               (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type));
    }
}
