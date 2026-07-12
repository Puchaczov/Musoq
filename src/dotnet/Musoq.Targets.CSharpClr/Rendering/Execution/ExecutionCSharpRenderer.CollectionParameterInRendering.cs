using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanRenderCollectionInCheck(ExecutionCollectionInCheck collectionInCheck)
    {
        return CanRenderExpression(collectionInCheck.Expression) &&
               CanRenderExpression(collectionInCheck.Collection) &&
               CanReferenceType(collectionInCheck.ElementType);
    }

    private ExpressionSyntax RenderCollectionInCheck(
        ExecutionCollectionInCheck collectionInCheck,
        ExecutionRenderContext context)
    {
        var helper = SyntaxFactory.GenericName("CollectionParameterContains")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(CreateTypeSyntax(collectionInCheck.ElementType))));

        return SyntaxFactory.InvocationExpression(helper)
            .WithArgumentList(CreateArgumentList(
                RenderExpression(collectionInCheck.Expression, context),
                RenderExpression(collectionInCheck.Collection, context)));
    }

    private static MethodDeclarationSyntax CreateCollectionParameterContainsFunction()
    {
        return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
            """
            private static bool CollectionParameterContains<T>(T value, IReadOnlyList<T> values)
            {
                var comparer = EqualityComparer<T>.Default;
                for (var index = 0; index < values.Count; index++)
                {
                    if (comparer.Equals(value, values[index]))
                        return true;
                }

                return false;
            }
            """)!;
    }
}
