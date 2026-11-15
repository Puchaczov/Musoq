using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateNullPredicate(IsNullNode predicate)
    {
        if (predicate.Expression is NullNode)
            return predicate.IsNegated ? "false" : "true";

        var value = GenerateConditionExpression(predicate.Expression);
        var type = BinarySchemaExpressionTypes.Resolve(predicate.Expression, _expressionFields, _registry);
        if (type.IsValueType && !BinarySchemaExpressionTypes.IsNullable(type))
            value = $"(({MapClrTypeName(type)}?)({value}))";
        var operation = predicate.IsNegated ? "is not null" : "is null";
        return $"(({value}) {operation})";
    }
}
