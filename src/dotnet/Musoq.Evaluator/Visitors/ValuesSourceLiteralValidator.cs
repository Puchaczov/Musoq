using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class ValuesSourceLiteralValidator
{
    public static void Validate(ValuesFromNode node)
    {
        foreach (var row in node.Rows)
        foreach (var field in row.Fields)
        {
            if (field.Expression is ParameterReferenceNode parameter &&
                ValuesStaticExpressionRules.IsCollectionParameter(parameter))
                throw ValuesSourceDiagnostics.Error(
                    $"VALUES field '{field.Name}' cannot use collection parameter '${parameter.Name}'. VALUES fields must be scalar; expand or index the collection before constructing the row.",
                    ValuesSourceDiagnostics.ExpressionSpan(field, node),
                    ("constraint", "collection-parameter"),
                    ("field", field.Name));

            if (!ValuesStaticExpressionRules.IsStaticScalarExpression(field.Expression))
                throw ValuesSourceDiagnostics.Error(
                    $"VALUES field '{field.Name}' must be a constant literal expression or scalar script parameter/let expression. Use literals, NULL, scalar script parameters, scalar let variables, or arithmetic over them.",
                    ValuesSourceDiagnostics.ExpressionSpan(field, node),
                    ("constraint", "non-static-expression"),
                    ("field", field.Name));
        }
    }
}
