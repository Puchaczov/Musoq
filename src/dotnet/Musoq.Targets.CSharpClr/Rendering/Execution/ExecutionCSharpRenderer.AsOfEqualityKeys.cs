using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax CreateNativeAsOfEqualitySelector(
        IReadOnlyList<ExecutionAsOfEqualityKey> keys,
        AsOfEqualityKeyShape shape,
        string candidateName,
        ExecutionRenderContext context
    )
    {
        if (keys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        var statements = CreateNativeAsOfEqualityStatements(
            keys.Select(key => key.Right).ToArray(),
            shape,
            "__asOfEquality",
            context
        );
        statements.Add(SyntaxFactory.ReturnStatement(CreateIdentifierName("__asOfEquality")));
        return SyntaxFactory
            .ParenthesizedLambdaExpression(SyntaxFactory.Block(statements))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName)))
                    )
                )
            );
    }

    private List<StatementSyntax> CreateNativeAsOfEqualityStatements(
        IReadOnlyList<ExecutionExpression> keys,
        AsOfEqualityKeyShape shape,
        string name,
        ExecutionRenderContext context
    )
    {
        var statements = new List<StatementSyntax>();
        var names = new List<ExpressionSyntax>();
        ExpressionSyntax present = CreateBooleanLiteral(true);
        for (var index = 0; index < keys.Count; index++)
        {
            var partName = name + "Part" + index;
            statements.Add(
                CreateLocalDeclaration(
                    CreateTypeSyntax(keys[index].ReturnType),
                    partName,
                    RenderExpression(keys[index], context)
                )
            );
            ExpressionSyntax part = CreateIdentifierName(partName);
            var partType = shape.PartTypes[index].RequireClrType();
            ExpressionSyntax? matches =
                partType == typeof(double) || partType == typeof(float)
                    ? CreateEvaluationHelperInvocation(nameof(EvaluationHelper.IsComparableJoinKey), part)
                : CanBeNull(keys[index].ReturnType)
                    ? SyntaxFactory.BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        part,
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    )
                : null;
            if (matches != null)
                present = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, present, matches);
            if (Nullable.GetUnderlyingType(keys[index].ReturnType.RequireClrType()) is not null)
                part = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        part,
                        SyntaxFactory.IdentifierName("GetValueOrDefault")
                    )
                );
            names.Add(SyntaxFactory.CastExpression(CreateTypeSyntax(shape.PartTypes[index]), part));
        }
        var keyType = CreateTypeSyntax(shape.KeyType);
        ExpressionSyntax value =
            keys.Count == 0 ? CreateBooleanLiteral(false)
            : keys.Count == 1 ? names[0]
            : SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(names.Select(SyntaxFactory.Argument)));
        value = SyntaxFactory.ConditionalExpression(present, value, SyntaxFactory.DefaultExpression(keyType));
        var nativeType = SyntaxFactory
            .GenericName("NativeEqualityKey")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(keyType)));
        var creation = SyntaxFactory
            .ObjectCreationExpression(nativeType)
            .WithArgumentList(CreateArgumentList(present, value));
        statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), name, creation));
        return statements;
    }
}
