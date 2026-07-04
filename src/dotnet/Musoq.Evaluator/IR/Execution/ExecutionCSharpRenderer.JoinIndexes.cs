using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateHash(ExecutionCreateHash createHash)
    {
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            createHash.Hash.Name,
            CreateHashTypeSyntax(
                createHash.KeyType,
                createHash.RowType,
                createHash.GeneratedRowTypeName),
            createHash.CapacityHint == null ? null : RenderCapacityHint(createHash.CapacityHint));
    }

    private LocalDeclarationStatementSyntax RenderCreateKeySet(ExecutionCreateKeySet createSet)
    {
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            createSet.Set.Name,
            CreateKeySetTypeSyntax(createSet.KeyType),
            createSet.CapacityHint == null ? null : RenderCapacityHint(createSet.CapacityHint));
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

    private LocalDeclarationStatementSyntax RenderCreateAsOfIndex(ExecutionCreateAsOfIndex createIndex)
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
            RenderExpression(createIndex.Candidates),
            CreateAsOfEqualityKeySelector(createIndex.EqualityKeys, static key => key.Right, candidateName),
            CreateAsOfKeySelector(createIndex.CandidateKey, candidateName, createIndex.ComparisonKeyType),
            CreateBinaryOpKindExpression(createIndex.ComparisonKind)
        };
        if (createIndex.TieBreak != null)
        {
            arguments.Add(CreateAsOfKeySelector(createIndex.TieBreak.Key, candidateName, createIndex.TieBreak.Key.ReturnType));
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
            CreateAsOfMatchInvocation(asOfProbe));
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            CreateIdentifierName(asOfProbe.Match.Name),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        var elseStatement = asOfProbe.NoMatchBody is { Nodes.Count: > 0 }
            ? RenderBlock(asOfProbe.NoMatchBody, context)
            : null;

        return StatementEmitter.CreateBlock(matchDeclaration, StatementEmitter.CreateIf(condition, RenderBlock(asOfProbe.Body, context), elseStatement));
    }

    private InvocationExpressionSyntax CreateAsOfMatchInvocation(ExecutionAsOfProbe asOfProbe)
    {
        if (asOfProbe.Index is not null)
            return CreateAsOfIndexFindInvocation(asOfProbe);

        var candidateName = asOfProbe.Candidate.Name;
        var typeArguments = new List<TypeSyntax> { CreateVariableTypeSyntax(asOfProbe.Match) };
        if (asOfProbe.TieBreak != null)
            typeArguments.Add(CreateTypeSyntax(asOfProbe.TieBreak.Key.ReturnType));

        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.FindAsOfMatch))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments)));
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(asOfProbe.Candidates),
            CreateAsOfEqualityPredicate(asOfProbe, candidateName),
            CreateAsOfKeySelector(asOfProbe.CandidateKey, candidateName, typeof(object)),
            CreateObjectCastExpression(RenderExpression(asOfProbe.ProbeKey)),
            CreateBinaryOpKindExpression(asOfProbe.ComparisonKind)
        };
        if (asOfProbe.TieBreak != null)
        {
            arguments.Add(CreateAsOfKeySelector(asOfProbe.TieBreak.Key, candidateName, asOfProbe.TieBreak.Key.ReturnType));
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

    private InvocationExpressionSyntax CreateAsOfIndexFindInvocation(ExecutionAsOfProbe asOfProbe)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(asOfProbe.Index!.Name),
                    SyntaxFactory.IdentifierName(nameof(AsOfJoinIndex<,>.Find))))
            .WithArgumentList(CreateArgumentList(
                CreateAsOfEqualityKeyExpression(asOfProbe.EqualityKeys, static key => key.Left),
                CreateAsOfComparisonKeyExpression(asOfProbe.ProbeKey, asOfProbe.ComparisonKeyType ?? typeof(object))));
    }

    private ExpressionSyntax CreateAsOfEqualityPredicate(ExecutionAsOfProbe asOfProbe, string candidateName)
    {
        if (asOfProbe.EqualityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        var condition = asOfProbe.EqualityKeys
            .Select(CreateAsOfEqualityCondition)
            .Aggregate(static (left, right) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, left, right));

        return SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))),
            condition);
    }

    private ExpressionSyntax CreateAsOfEqualityCondition(ExecutionAsOfEqualityKey key)
    {
        var left = CreateObjectCastExpression(RenderExpression(key.Left));
        var right = CreateObjectCastExpression(RenderExpression(key.Right));
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
        string candidateName)
    {
        if (equalityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return SyntaxFactory.ParenthesizedLambdaExpression(CreateAsOfEqualityKeyExpression(equalityKeys, keySelector))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ExpressionSyntax CreateAsOfEqualityKeyExpression(
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        Func<ExecutionAsOfEqualityKey, ExecutionExpression> keySelector)
    {
        if (equalityKeys.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        if (equalityKeys.Count == 1)
            return CreateObjectCastExpression(RenderExpression(keySelector(equalityKeys[0])));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.CreateAsOfEqualityKey))))
            .WithArgumentList(CreateArgumentList(equalityKeys
                .Select<ExecutionAsOfEqualityKey, ExpressionSyntax>(key =>
                    CreateObjectCastExpression(RenderExpression(keySelector(key))))
                .ToArray()));
    }

    private ParenthesizedLambdaExpressionSyntax CreateAsOfKeySelector(
        ExecutionExpression candidateKey,
        string candidateName,
        Type comparisonKeyType)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(
                CreateAsOfComparisonKeyExpression(candidateKey, comparisonKeyType))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
    }

    private ExpressionSyntax CreateAsOfComparisonKeyExpression(ExecutionExpression key, Type comparisonKeyType)
    {
        return comparisonKeyType == typeof(object)
            ? CreateObjectCastExpression(RenderExpression(key))
            : RenderExpression(key);
    }

    private LocalDeclarationStatementSyntax RenderCreateRangeIndex(ExecutionCreateRangeIndex createIndex)
    {
        var candidateName = createIndex.Candidate.Name;
        var method = SyntaxFactory.GenericName(nameof(EvaluationHelper.CreateRangeJoinIndex))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
            [
                CreateVariableTypeSyntax(createIndex.Candidate),
                CreateTypeSyntax(createIndex.KeyType)
            ])));
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    method))
            .WithArgumentList(CreateArgumentList(
                RenderExpression(createIndex.Candidates),
                CreateRangeKeySelector(createIndex.CandidateKey, candidateName),
                CreateBinaryOpKindExpression(createIndex.ComparisonKind)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createIndex.Index.Name,
            invocation);
    }

    private ForEachStatementSyntax RenderRangeProbe(
        ExecutionRangeProbe rangeProbe,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateForeach(
            EscapeIdentifier(rangeProbe.Match.Name),
            CreateRangeIndexFindInvocation(rangeProbe),
            RenderBlock(rangeProbe.Body, context));
    }

    private InvocationExpressionSyntax CreateRangeIndexFindInvocation(ExecutionRangeProbe rangeProbe)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rangeProbe.Index.Name),
                    SyntaxFactory.IdentifierName(nameof(RangeJoinIndex<,>.Find))))
            .WithArgumentList(CreateArgumentList(
                RenderExpression(rangeProbe.ProbeKey)));
    }

    private ParenthesizedLambdaExpressionSyntax CreateRangeKeySelector(
        ExecutionExpression candidateKey,
        string candidateName)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(RenderExpression(candidateKey))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(candidateName))))));
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
