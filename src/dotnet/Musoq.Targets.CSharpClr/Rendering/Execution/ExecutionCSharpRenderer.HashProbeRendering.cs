using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderHashProbe(ExecutionHashProbe hashProbe, ExecutionRenderContext context)
    {
        return RenderKeyProbe(
            hashProbe.Key,
            hashProbe.KeyType.RequireClrType(),
            hashProbe.Body,
            hashProbe.NoMatchBody,
            hashProbe.MatchFound,
            hashProbe.KeyVariableName ?? "key",
            keyName => CreateHashTryGetValueExpression(hashProbe.Hash.Name, keyName, hashProbe.Matches.Name),
            context);
    }

    private List<StatementSyntax> RenderKeyProbe(
        ExecutionExpression key, Type keyType, ExecutionBlock body, ExecutionBlock? noMatchBody,
        ExecutionVariable? matchFound, string keyVariableName,
        Func<string, ExpressionSyntax> createLookupExpression, ExecutionRenderContext context,
        Func<BlockSyntax, BlockSyntax>? decorateMatchBody = null)
    {
        var statements = new List<StatementSyntax>();
        if (matchFound is not null)
            statements.Add(CreateLocalDeclaration(
                CreateTypeSyntax(typeof(bool)),
                matchFound.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)));

        var keyLocal = AddKeyProbeLocal(statements, key, keyType, keyVariableName);
        var lookupExpression = createLookupExpression(keyVariableName);
        var condition = CreateKeyProbeCondition(keyLocal, keyType, keyVariableName, lookupExpression);
        var elseStatement = noMatchBody is { Nodes.Count: > 0 }
            ? RenderBlock(noMatchBody, context)
            : null;
        var matchBody = RenderBlock(body, context);
        if (decorateMatchBody != null)
            matchBody = decorateMatchBody(matchBody);

        if (matchFound is null)
        {
            statements.Add(StatementEmitter.CreateIf(condition, matchBody, elseStatement));
            return statements;
        }

        statements.Add(StatementEmitter.CreateIf(condition, matchBody));
        if (elseStatement is not null)
        {
            statements.Add(StatementEmitter.CreateIf(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(matchFound.Name)),
                elseStatement));
        }

        return statements;
    }

}
