using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateHash(
        ExecutionCreateHash createHash,
        ExecutionRenderContext context)
    {
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            createHash.Hash.Name,
            CreateHashTypeSyntax(
                createHash.KeyType.RequireClrType(),
                createHash.RowType.RequireClrType(),
                createHash.GeneratedRowTypeName),
            createHash.CapacityHint == null ? null : RenderCapacityHint(createHash.CapacityHint, context));
    }

    private LocalDeclarationStatementSyntax RenderCreateKeySet(
        ExecutionCreateKeySet createSet,
        ExecutionRenderContext context)
    {
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            createSet.Set.Name,
            CreateKeySetTypeSyntax(createSet.KeyType),
            createSet.CapacityHint == null ? null : RenderCapacityHint(createSet.CapacityHint, context));
    }

    private static ExpressionStatementSyntax RenderStoreCteIndex(ExecutionStoreCteIndex store)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateCteIndexSlotAccess(store.IndexSlot),
                SyntaxFactory.IdentifierName(store.Index.Name)));
    }

    private static LocalDeclarationStatementSyntax RenderLoadCteIndex(ExecutionLoadCteIndex load)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            load.Index.Name,
            CreateCteIndexSlotAccess(load.IndexSlot));
    }

    private static TypeSyntax CreateCteIndexTypeSyntax(
        ExecutionCteSidecarIndexKind kind,
        Type keyType,
        Type? rowType = null,
        string? generatedRowTypeName = null)
    {
        return kind switch
        {
            ExecutionCteSidecarIndexKind.Hash => CreateHashTypeSyntax(
                keyType,
                rowType ?? typeof(Row),
                generatedRowTypeName),
            ExecutionCteSidecarIndexKind.KeySet => CreateKeySetTypeSyntax(keyType),
            _ => throw UnsupportedShape.Of($"CTE sidecar index kind {kind}")
        };
    }

    private LocalDeclarationStatementSyntax RenderCreateAsOfIndex(
        ExecutionCreateAsOfIndex createIndex,
        ExecutionRenderContext context)
    {
        var candidateName = createIndex.Candidate.Name;
        var typeArguments = new List<TypeSyntax>
        {
            CreateVariableTypeSyntax(createIndex.Candidate),
            CreateTypeSyntax(createIndex.ComparisonKeyType),
            CreateTypeSyntax(createIndex.EqualityShape.KeyType)
        };
        if (createIndex.TieBreak != null)
            typeArguments.Add(CreateTypeSyntax(createIndex.TieBreak.Key.ReturnType));

        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.CreateAsOfIndex))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                typeArguments)));
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(createIndex.Candidates, context),
            CreateNativeAsOfEqualitySelector(createIndex.EqualityKeys, createIndex.EqualityShape, candidateName, context),
            CreateAsOfKeySelector(createIndex.CandidateKey, candidateName, createIndex.ComparisonKeyType.RequireClrType(), context),
            CreateBinaryOpKindExpression(createIndex.ComparisonKind)
        };
        if (createIndex.TieBreak != null)
        {
            arguments.Add(CreateAsOfKeySelector(createIndex.TieBreak.Key, candidateName, createIndex.TieBreak.Key.ReturnType.RequireClrType(), context));
            arguments.Add(CreateBooleanLiteral(createIndex.TieBreak.Descending));
            arguments.Add(CreateNullOrderingExpression(createIndex.TieBreak.NullOrdering));
        }

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    method))
            .WithArgumentList(CreateArgumentList(arguments));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createIndex.Index.Name,
            invocation);
    }

    private BlockSyntax RenderAsOfProbe(
        ExecutionAsOfProbe asOfProbe,
        ExecutionRenderContext context)
    {
        if (asOfProbe.Index is null)
            throw new InvalidOperationException("ASOF probes require a planner-selected typed index.");
        var equalityName = asOfProbe.Match.Name + "Equality";
        var equalityStatements = CreateNativeAsOfEqualityStatements(asOfProbe.EqualityKeys.Select(key => key.Left).ToArray(),
            asOfProbe.EqualityShape, equalityName, context);
        var matchDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            asOfProbe.Match.Name,
            CreateAsOfIndexFindInvocation(asOfProbe, equalityName, context));
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            CreateIdentifierName(asOfProbe.Match.Name),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        var elseStatement = asOfProbe.NoMatchBody is { Nodes.Count: > 0 }
            ? RenderBlock(asOfProbe.NoMatchBody, context)
            : null;

        equalityStatements.Add(matchDeclaration);
        equalityStatements.Add(StatementEmitter.CreateIf(condition, RenderBlock(asOfProbe.Body, context), elseStatement));
        return SyntaxFactory.Block(equalityStatements);
    }

    private InvocationExpressionSyntax CreateAsOfIndexFindInvocation(
        ExecutionAsOfProbe asOfProbe,
        string equalityName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(asOfProbe.Index!.Name),
                    SyntaxFactory.IdentifierName("Find")))
            .WithArgumentList(CreateArgumentList(
                CreateIdentifierName(equalityName),
                CreateAsOfComparisonKeyExpression(asOfProbe.ProbeKey, asOfProbe.ComparisonKeyType!.RequireClrType(), context)));
    }

    private ParenthesizedLambdaExpressionSyntax CreateAsOfKeySelector(
        ExecutionExpression candidateKey,
        string candidateName,
        Type comparisonKeyType,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(
                CreateAsOfComparisonKeyExpression(candidateKey, comparisonKeyType, context))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ExpressionSyntax CreateAsOfComparisonKeyExpression(
        ExecutionExpression key,
        Type comparisonKeyType,
        ExecutionRenderContext context)
    {
        if (comparisonKeyType == typeof(object))
            throw new InvalidOperationException("ASOF comparison keys must remain concrete.");
        var expression = RenderExpression(key, context);
        return key.ReturnType.RequireClrType() == comparisonKeyType ? expression :
            SyntaxFactory.CastExpression(CreateTypeSyntax(comparisonKeyType), SyntaxFactory.ParenthesizedExpression(expression));
    }

    private LocalDeclarationStatementSyntax RenderCreateRangeIndex(
        ExecutionCreateRangeIndex createIndex,
        ExecutionRenderContext context)
    {
        var candidateName = createIndex.Candidate.Name;
        var typeArguments = new List<TypeSyntax>
        {
            CreateVariableTypeSyntax(createIndex.Candidate)
        };
        if (createIndex.PartitionKeys is { Count: > 0 })
            typeArguments.Add(CreateTypeSyntax(createIndex.PartitionKeyType!.RequireClrType()));
        typeArguments.Add(CreateTypeSyntax(createIndex.KeyType));
        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.CreateRangeJoinIndex))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                typeArguments)));
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(createIndex.Candidates, context)
        };
        if (createIndex.PartitionKeys is { Count: > 0 } partitionKeys)
        {
            arguments.Add(CreateRangePartitionKeySelector(
                partitionKeys,
                static key => key.Right,
                createIndex.PartitionKeyType!,
                candidateName,
                context));
        }

        arguments.Add(CreateRangeKeySelector(createIndex.CandidateKey, candidateName, context));
        arguments.Add(CreateBinaryOpKindExpression(createIndex.ComparisonKind));
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    method))
            .WithArgumentList(CreateArgumentList(arguments));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createIndex.Index.Name,
            invocation);
    }

    private InvocationExpressionSyntax CreateRangeIndexFindInvocation(
        ExecutionRangeProbe rangeProbe,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>();
        if (rangeProbe.PartitionKeys is { Count: > 0 } partitionKeys)
        {
            arguments.Add(CreateRangePartitionKeyExpression(
                partitionKeys,
                static key => key.Left,
                rangeProbe.PartitionKeyType!,
                context));
        }

        arguments.Add(RenderExpression(rangeProbe.ProbeKey, context));
        SimpleNameSyntax findMethod = rangeProbe.PartitionKeys is { Count: > 0 }
            ? SyntaxFactory.GenericName(nameof(RangeJoinIndex<object, object>.Find))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(
                    CreateTypeSyntax(rangeProbe.PartitionKeyType!))))
            : SyntaxFactory.IdentifierName(nameof(RangeJoinIndex<object, object>.Find));
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rangeProbe.Index.Name),
                    findMethod))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private ParenthesizedLambdaExpressionSyntax CreateRangeKeySelector(
        ExecutionExpression candidateKey,
        string candidateName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(RenderExpression(candidateKey, context))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ParenthesizedLambdaExpressionSyntax CreateRangePartitionKeySelector(
        IReadOnlyList<ExecutionAsOfEqualityKey> partitionKeys,
        Func<ExecutionAsOfEqualityKey, ExecutionExpression> keySelector,
        ExecutionTypeRef partitionKeyType,
        string candidateName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(
                CreateRangePartitionKeyExpression(partitionKeys, keySelector, partitionKeyType, context))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ExpressionSyntax CreateRangePartitionKeyExpression(
        IReadOnlyList<ExecutionAsOfEqualityKey> partitionKeys,
        Func<ExecutionAsOfEqualityKey, ExecutionExpression> keySelector,
        ExecutionTypeRef partitionKeyType,
        ExecutionRenderContext context)
    {
        if (partitionKeyType.RequireClrType() == typeof(object))
            throw new InvalidOperationException("Range partition keys require a concrete typed carrier.");

        if (partitionKeys.Count == 1)
            return RenderExpression(keySelector(partitionKeys[0]), context);

        var tuple = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(partitionKeys
            .Select(key => SyntaxFactory.Argument(RenderExpression(keySelector(key), context)))));
        if (Nullable.GetUnderlyingType(partitionKeyType.RequireClrType()) == null)
            return tuple;

        var nullableKeys = partitionKeys
            .Select(keySelector)
            .Where(static key => CanBeNull(key.ReturnType))
            .ToArray();
        if (nullableKeys.Length == 0)
            return tuple;

        var nullCondition = nullableKeys
            .Select(key => (ExpressionSyntax)SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                RenderExpression(key, context),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))
            .Aggregate(static (left, right) => SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                left,
                right));
        return SyntaxFactory.ConditionalExpression(
            nullCondition,
            SyntaxFactory.DefaultExpression(CreateTypeSyntax(partitionKeyType)),
            tuple);
    }

    private static MemberAccessExpressionSyntax CreateBinaryOpKindExpression(BinaryOpKind kind)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ParseName(typeof(BinaryOpKind).FullName!),
            SyntaxFactory.IdentifierName(kind.ToString()));
    }

    private static MemberAccessExpressionSyntax CreateNullOrderingExpression(NullOrdering nullOrdering)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ParseName(typeof(NullOrdering).FullName!),
            SyntaxFactory.IdentifierName(nullOrdering.ToString()));
    }
}
