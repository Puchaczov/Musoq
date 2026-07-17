using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderHashAdd(ExecutionHashAdd hashAdd)
    {
        return RenderKeyBuild(
            hashAdd.Key,
            hashAdd.KeyType.RequireClrType(),
            hashAdd.PrecomputedKey,
            hashAdd.KeyVariableName ?? "key",
            hashAdd.NullHandling,
            keyName => CreateHashBucketAddOrCreateStatement(hashAdd, keyName, hashAdd.BucketVariableName ?? "matches"));
    }

    private List<StatementSyntax> RenderKeySetAdd(ExecutionKeySetAdd keySetAdd)
    {
        return RenderKeyBuild(
            keySetAdd.Key,
            keySetAdd.KeyType.RequireClrType(),
            keySetAdd.PrecomputedKey,
            keySetAdd.KeyVariableName ?? "key",
            keySetAdd.NullHandling,
            keyName => CreateKeySetAddStatement(keySetAdd.Set.Name, keyName));
    }

    private List<StatementSyntax> RenderKeySetProbe(ExecutionKeySetProbe keySetProbe, ExecutionRenderContext context)
    {
        return RenderKeyProbe(
            keySetProbe.Key,
            keySetProbe.KeyType.RequireClrType(),
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

}
