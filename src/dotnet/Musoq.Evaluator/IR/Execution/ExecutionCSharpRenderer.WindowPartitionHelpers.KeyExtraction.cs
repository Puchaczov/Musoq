using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateWindowPartitionKeyExtractionStatements(
        ExecutionWindowKeyArray partitionKeys,
        ExecutionVariable? partitionBuilder,
        string indexVariableName,
        ExpressionSyntax partitionKeyExpression)
    {
        if (partitionBuilder == null)
        {
            if (!partitionKeys.ShouldMaterialize)
                throw new InvalidOperationException("Builder-only window partition keys require a partition builder.");

            yield return CreateWindowKeyArrayAssignment(
                partitionKeys,
                indexVariableName,
                partitionKeyExpression);
            yield break;
        }

        var keyName = $"{partitionKeys.Variable.Name}Value";
        var keyValue = HasGeneratedWindowKeyType(partitionKeys)
            ? partitionKeyExpression
            : CastIfNeeded(partitionKeyExpression, GetArrayElementType(partitionKeys.Variable));

        yield return CreateLocalDeclaration(
            CreateWindowKeyElementTypeSyntax(partitionKeys),
            keyName,
            keyValue);

        if (partitionKeys.ShouldMaterialize)
        {
            yield return CreateWindowKeyArrayAssignment(
                partitionKeys,
                indexVariableName,
                SyntaxFactory.IdentifierName(keyName));
        }

        yield return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(partitionBuilder.Name),
                        SyntaxFactory.IdentifierName(nameof(WindowPartitionBuilder<>.Add))))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(keyName),
                    SyntaxFactory.IdentifierName(indexVariableName))));
    }
}
