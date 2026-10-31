using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ObjectCreationExpressionSyntax CreateFinalShapeCreation(
        string shapeTypeName,
        ExecutionAppendRow appendRow,
        ExecutionRenderContext context)
    {
        var arguments = appendRow.Values.Select((value, index) =>
        {
            var targetType = index < appendRow.RowShape.Fields.Count
                ? appendRow.RowShape.Fields[index].Type.RequireClrType()
                : value.Value.ReturnType.RequireClrType();

            return SyntaxFactory.Argument(RenderRowConstructorValue(value.Value, targetType, context));
        });

        var creation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(shapeTypeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
        var owned = context.Session.FinalShapeYieldSink?.OwnedBuffer;
        if (owned?.SequenceField == null || owned.SourceName != appendRow.Table.Name)
            return creation;
        return creation.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(owned.SequenceField),
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(owned.SourceName), SyntaxFactory.IdentifierName("Count"))))));
    }
}
