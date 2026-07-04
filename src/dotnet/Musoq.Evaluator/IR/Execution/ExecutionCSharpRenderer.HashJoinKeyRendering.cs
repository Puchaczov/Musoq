using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderHashAdd(ExecutionHashAdd hashAdd)
    {
        return RenderKeyBuild(
            hashAdd.Key,
            hashAdd.KeyType,
            hashAdd.PrecomputedKey,
            hashAdd.KeyVariableName ?? "key",
            hashAdd.NullHandling,
            keyName => CreateHashBucketAddOrCreateStatement(hashAdd, keyName, hashAdd.BucketVariableName ?? "matches"));
    }

    private List<StatementSyntax> RenderHashProbe(ExecutionHashProbe hashProbe, ExecutionRenderContext context)
    {
        return RenderKeyProbe(
            hashProbe.Key,
            hashProbe.KeyType,
            hashProbe.Body,
            hashProbe.NoMatchBody,
            hashProbe.MatchFound,
            hashProbe.KeyVariableName ?? "key",
            keyName => CreateHashTryGetValueExpression(hashProbe.Hash.Name, keyName, hashProbe.Matches.Name),
            context);
    }

    private List<StatementSyntax> RenderKeySetAdd(ExecutionKeySetAdd keySetAdd)
    {
        return RenderKeyBuild(
            keySetAdd.Key,
            keySetAdd.KeyType,
            keySetAdd.PrecomputedKey,
            keySetAdd.KeyVariableName ?? "key",
            keySetAdd.NullHandling,
            keyName => CreateKeySetAddStatement(keySetAdd.Set.Name, keyName));
    }

    private List<StatementSyntax> RenderKeySetProbe(ExecutionKeySetProbe keySetProbe, ExecutionRenderContext context)
    {
        return RenderKeyProbe(
            keySetProbe.Key,
            keySetProbe.KeyType,
            keySetProbe.Body,
            keySetProbe.NoMatchBody,
            keySetProbe.MatchFound,
            keySetProbe.KeyVariableName ?? "key",
            keyName => CreateKeySetContainsExpression(keySetProbe.Set.Name, keyName),
            context);
    }

    private List<StatementSyntax> RenderKeyBuild(
        ExecutionExpression key,
        Type keyType,
        ExecutionVariable? precomputedKey,
        string keyVariableName,
        ExecutionKeyBuildNullHandling nullHandling,
        Func<string, StatementSyntax> createAddStatement)
    {
        var statements = new List<StatementSyntax>();
        if (precomputedKey is { } precomputed)
        {
            statements.Add(createAddStatement(precomputed.Name));
            return statements;
        }

        if (nullHandling == ExecutionKeyBuildNullHandling.ConditionalSkip)
        {
            AddConditionalKeyBuild(statements, key, keyType, keyVariableName, createAddStatement);
            return statements;
        }

        AddKeyBuildLocal(statements, key, keyType, keyVariableName);
        statements.Add(createAddStatement(keyVariableName));
        return statements;
    }

    private List<StatementSyntax> RenderKeyProbe(
        ExecutionExpression key, Type keyType, ExecutionBlock body, ExecutionBlock? noMatchBody,
        ExecutionVariable? matchFound, string keyVariableName,
        Func<string, ExpressionSyntax> createLookupExpression, ExecutionRenderContext context)
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

        if (matchFound is null)
        {
            statements.Add(StatementEmitter.CreateIf(condition, RenderBlock(body, context), elseStatement));
            return statements;
        }

        statements.Add(StatementEmitter.CreateIf(condition, RenderBlock(body, context)));
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
