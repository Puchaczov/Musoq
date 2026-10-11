using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static InvocationExpressionSyntax CreateNativeRowValueRead(
        ExpressionSyntax row, int columnIndex, TypeSyntax valueType)
    {
        var method = SyntaxFactory.GenericName("GetValue")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(valueType)));
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, row, method))
            .WithArgumentList(CreateArgumentList(CreateIntLiteral(columnIndex)));
    }
}
