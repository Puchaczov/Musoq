using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ObjectCreationExpressionSyntax CreateFinalShapeCreation(
        string shapeTypeName,
        ExecutionAppendRow appendRow)
    {
        var arguments = appendRow.Values.Select((value, index) =>
        {
            var targetType = index < appendRow.RowShape.Fields.Count
                ? appendRow.RowShape.Fields[index].Type.RequireClrType()
                : value.Value.ReturnType.RequireClrType();

            return SyntaxFactory.Argument(RenderRowConstructorValue(value.Value, targetType));
        });

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(shapeTypeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }
}
