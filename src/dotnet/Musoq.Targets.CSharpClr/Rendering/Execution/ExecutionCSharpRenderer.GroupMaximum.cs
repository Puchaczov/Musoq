using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static BlockSyntax CreateMaximumUpdateStatement(ExecutionHashAdd add, string key)
    {
        var name = HashBuildBucketNaming.Resolve(add);
        var exists = name + "Exists";
        var maximum = SyntaxFactory.IdentifierName(name);
        var candidate = SyntaxFactory.IdentifierName(add.Row.Name);
        ExpressionSyntax replace = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanExpression, candidate, maximum);
        if (Nullable.GetUnderlyingType(add.RowType.RequireClrType()) != null)
            replace = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    candidate,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                ),
                replace
            );
        replace = SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalOrExpression,
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.IdentifierName(exists)),
            replace
        );
        return StatementEmitter.CreateBlock(
            CreateHashBucketRefDeclaration(add.Hash.Name, key, name, exists),
            SyntaxFactory.IfStatement(
                replace,
                StatementEmitter.CreateBlock(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, maximum, candidate)
                    )
                )
            )
        );
    }
}
