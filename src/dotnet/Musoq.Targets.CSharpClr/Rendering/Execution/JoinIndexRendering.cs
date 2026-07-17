using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

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
            CreateTypeSyntax(createIndex.ComparisonKeyType)
        };
        if (createIndex.TieBreak != null)
            typeArguments.Add(CreateTypeSyntax(createIndex.TieBreak.Key.ReturnType));

        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.CreateAsOfIndex))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                typeArguments)));
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(createIndex.Candidates, context),
            CreateAsOfEqualityKeySelector(createIndex.EqualityKeys, static key => key.Right, candidateName, context),
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
        var matchDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            asOfProbe.Match.Name,
            CreateAsOfMatchInvocation(asOfProbe, context));
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            CreateIdentifierName(asOfProbe.Match.Name),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        var elseStatement = asOfProbe.NoMatchBody is { Nodes.Count: > 0 }
            ? RenderBlock(asOfProbe.NoMatchBody, context)
            : null;

        return StatementEmitter.CreateBlock(matchDeclaration, StatementEmitter.CreateIf(condition, RenderBlock(asOfProbe.Body, context), elseStatement));
    }

    private InvocationExpressionSyntax CreateAsOfMatchInvocation(
        ExecutionAsOfProbe asOfProbe,
        ExecutionRenderContext context)
    {
        if (asOfProbe.Index is not null)
            return CreateAsOfIndexFindInvocation(asOfProbe, context);

        var candidateName = asOfProbe.Candidate.Name;
        var typeArguments = new List<TypeSyntax> { CreateVariableTypeSyntax(asOfProbe.Match) };
        if (asOfProbe.TieBreak != null)
            typeArguments.Add(CreateTypeSyntax(asOfProbe.TieBreak.Key.ReturnType));

        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.FindAsOfMatch))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments)));
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(asOfProbe.Candidates, context),
            CreateAsOfEqualityPredicate(asOfProbe, candidateName, context),
            CreateAsOfKeySelector(asOfProbe.CandidateKey, candidateName, typeof(object), context),
            CreateObjectCastExpression(RenderExpression(asOfProbe.ProbeKey, context)),
            CreateBinaryOpKindExpression(asOfProbe.ComparisonKind)
        };
        if (asOfProbe.TieBreak != null)
        {
            arguments.Add(CreateAsOfKeySelector(asOfProbe.TieBreak.Key, candidateName, asOfProbe.TieBreak.Key.ReturnType.RequireClrType(), context));
            arguments.Add(CreateBooleanLiteral(asOfProbe.TieBreak.Descending));
            arguments.Add(CreateNullOrderingExpression(asOfProbe.TieBreak.NullOrdering));
        }

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    method))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private InvocationExpressionSyntax CreateAsOfIndexFindInvocation(
        ExecutionAsOfProbe asOfProbe,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(asOfProbe.Index!.Name),
                    SyntaxFactory.IdentifierName(nameof(AsOfJoinIndex<,>.Find))))
            .WithArgumentList(CreateArgumentList(
                CreateAsOfEqualityKeyExpression(asOfProbe.EqualityKeys, static key => key.Left, context),
                CreateAsOfComparisonKeyExpression(asOfProbe.ProbeKey, asOfProbe.ComparisonKeyType?.RequireClrType() ?? typeof(object), context)));
    }

    private ExpressionSyntax CreateAsOfEqualityPredicate(
        ExecutionAsOfProbe asOfProbe,
        string candidateName,
        ExecutionRenderContext context)
    {
        if (asOfProbe.EqualityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        var condition = asOfProbe.EqualityKeys
            .Select(key => CreateAsOfEqualityCondition(key, context))
            .Aggregate(static (left, right) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, left, right));

        return SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))),
            condition);
    }

    private ExpressionSyntax CreateAsOfEqualityCondition(
        ExecutionAsOfEqualityKey key,
        ExecutionRenderContext context)
    {
        var left = CreateObjectCastExpression(RenderExpression(key.Left, context));
        var right = CreateObjectCastExpression(RenderExpression(key.Right, context));
        var equality = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.IdentifierName(nameof(object.Equals))))
            .WithArgumentList(CreateArgumentList(left, right));

        if (!CanBeNull(key.Left.ReturnType) && !CanBeNull(key.Right.ReturnType))
            return equality;

        return SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                left,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    right,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                equality));
    }

    private ExpressionSyntax CreateAsOfEqualityKeySelector(
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        Func<ExecutionAsOfEqualityKey, ExecutionExpression> keySelector,
        string candidateName,
        ExecutionRenderContext context)
    {
        if (equalityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return SyntaxFactory.ParenthesizedLambdaExpression(CreateAsOfEqualityKeyExpression(equalityKeys, keySelector, context))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ExpressionSyntax CreateAsOfEqualityKeyExpression(
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        Func<ExecutionAsOfEqualityKey, ExecutionExpression> keySelector,
        ExecutionRenderContext context)
    {
        if (equalityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        if (equalityKeys.Count == 1)
            return CreateObjectCastExpression(RenderExpression(keySelector(equalityKeys[0]), context));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.CreateAsOfEqualityKey))))
            .WithArgumentList(CreateArgumentList(equalityKeys
                .Select<ExecutionAsOfEqualityKey, ExpressionSyntax>(key =>
                    CreateObjectCastExpression(RenderExpression(keySelector(key), context)))
                .ToArray()));
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
        return comparisonKeyType == typeof(object)
            ? CreateObjectCastExpression(RenderExpression(key, context))
            : RenderExpression(key, context);
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

    private StatementSyntax RenderRangeProbe(
        ExecutionRangeProbe rangeProbe,
        ExecutionRenderContext context)
    {
        var loop = StatementEmitter.CreateForeach(
            EscapeIdentifier(rangeProbe.Match.Name),
            CreateRangeIndexFindInvocation(rangeProbe, context),
            RenderBlock(rangeProbe.Body, context));
        if (rangeProbe.MatchFound == null)
            return loop;

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(bool)),
                rangeProbe.MatchFound.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
            loop
        };
        if (rangeProbe.NoMatchBody is { Nodes.Count: > 0 } noMatchBody)
        {
            statements.Add(StatementEmitter.CreateIf(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(rangeProbe.MatchFound.Name)),
                RenderBlock(noMatchBody, context)));
        }

        return SyntaxFactory.Block(statements);
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
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rangeProbe.Index.Name),
                    SyntaxFactory.IdentifierName(nameof(RangeJoinIndex<,>.Find))))
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
            return CreateAsOfEqualityKeyExpression(partitionKeys, keySelector, context);

        if (partitionKeys.Count == 1)
            return RenderExpression(keySelector(partitionKeys[0]), context);

        var tuple = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(partitionKeys
            .Select(key => SyntaxFactory.Argument(RenderExpression(keySelector(key), context)))));
        if (Nullable.GetUnderlyingType(partitionKeyType.RequireClrType()) == null)
            return tuple;

        var nullCondition = partitionKeys
            .Select(keySelector)
            .Where(static key => CanBeNull(key.ReturnType))
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
