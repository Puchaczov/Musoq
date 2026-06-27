using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ObjectCreationExpressionSyntax CreateFinalShapeCreation(
        string shapeTypeName,
        ExecutionAppendRow appendRow)
    {
        var arguments = appendRow.Values.Select((value, index) =>
        {
            var targetType = index < appendRow.RowShape.Fields.Count
                ? appendRow.RowShape.Fields[index].Type
                : value.Value.ReturnType;

            return SyntaxFactory.Argument(RenderRowConstructorValue(value.Value, targetType));
        });

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(shapeTypeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }
}
