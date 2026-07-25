using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
using Musoq.Plugins;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax? TryRenderInlineAggregateSet(
        ExecutionAggregateSet aggregateSet,
        IReadOnlyList<ExecutionExpression> setArguments,
        IReadOnlyList<ParameterInfo> setParameters,
        bool useExplicitAccumulatorInput)
    {
        var kind = AggregateInlinePolicy.Resolve(aggregateSet.Accumulator.Kernel);
        if (kind == AggregateInlineKind.None)
            return null;

        var state = CreateAggregateAccumulatorAccess(aggregateSet.Group, aggregateSet.Accumulator);
        return kind switch
        {
            AggregateInlineKind.CountAll => RenderInlineCountAllSet(state),
            AggregateInlineKind.CountNullable => RenderInlineCountNullableSet(
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput)),
            AggregateInlineKind.CountReference => RenderInlineCountReferenceSet(
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput)),
            AggregateInlineKind.Sum => RenderInlineNullableValueSet(
                aggregateSet,
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput),
                CreateInlineSumStatements),
            AggregateInlineKind.Avg => RenderInlineNullableValueSet(
                aggregateSet,
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput),
                CreateInlineAvgStatements),
            AggregateInlineKind.Min => RenderInlineNullableValueSet(
                aggregateSet,
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput),
                (current, aggregateState) => CreateInlineExtremumStatements(current, aggregateState, SyntaxKind.LessThanExpression)),
            AggregateInlineKind.Max => RenderInlineNullableValueSet(
                aggregateSet,
                state,
                CreateInlineAggregateInput(aggregateSet, setArguments, setParameters, useExplicitAccumulatorInput),
                (current, aggregateState) => CreateInlineExtremumStatements(current, aggregateState, SyntaxKind.GreaterThanExpression)),
            _ => null
        };
    }

    private static ExpressionSyntax? TryCreateInlineAggregateGet(ExecutionAggregateCall aggregateCall)
    {
        var kind = AggregateInlinePolicy.Resolve(aggregateCall.Accumulator.Kernel);
        if (kind == AggregateInlineKind.None)
            return null;

        var state = CreateAggregateAccumulatorAccess(aggregateCall.Group, aggregateCall.Accumulator);
        return kind switch
        {
            AggregateInlineKind.CountAll or
                AggregateInlineKind.CountNullable or
                AggregateInlineKind.CountReference => CreateStateFieldAccess(state, "Count"),
            AggregateInlineKind.Sum or
                AggregateInlineKind.Min or
                AggregateInlineKind.Max => CreateNullableStateValueRead(state, aggregateCall.Accumulator.Kernel.ResultType),
            AggregateInlineKind.Avg => CreateNullableAverageRead(
                state,
                aggregateCall.Accumulator.Kernel.UnderlyingResultType,
                aggregateCall.Accumulator.Kernel.ResultType),
            _ => null
        };
    }

    private ExpressionSyntax CreateInlineAggregateInput(
        ExecutionAggregateSet aggregateSet,
        IReadOnlyList<ExecutionExpression> setArguments,
        IReadOnlyList<ParameterInfo> setParameters,
        bool useExplicitAccumulatorInput)
    {
        if (useExplicitAccumulatorInput)
            return CastIfNeeded(RenderExpression(aggregateSet.AccumulatorInput!), setParameters[0].ParameterType);

        if (setArguments.Count != 1 || setParameters.Count != 1)
            throw new InvalidOperationException("Inline aggregate set requires exactly one input expression.");

        return CastIfNeeded(RenderExpression(setArguments[0]), setParameters[0].ParameterType);
    }

    private static ExpressionStatementSyntax RenderInlineCountAllSet(ExpressionSyntax state)
    {
        return CreateCountIncrementStatement(state);
    }

    private static IfStatementSyntax RenderInlineCountNullableSet(
        ExpressionSyntax state,
        ExpressionSyntax input)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(input),
                SyntaxFactory.IdentifierName("HasValue")),
            StatementEmitter.CreateBlock(CreateCountIncrementStatement(state)));
    }

    private static IfStatementSyntax RenderInlineCountReferenceSet(
        ExpressionSyntax state,
        ExpressionSyntax input)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                input,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            StatementEmitter.CreateBlock(CreateCountIncrementStatement(state)));
    }

    private static ExpressionStatementSyntax CreateCountIncrementStatement(ExpressionSyntax state)
    {
        var count = CreateStateFieldAccess(state, "Count");
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            count,
            SyntaxFactory.CheckedExpression(
                SyntaxKind.CheckedExpression,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    count,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(1L))))));
    }

    private static BlockSyntax RenderInlineNullableValueSet(
        ExecutionAggregateSet aggregateSet,
        ExpressionSyntax state,
        ExpressionSyntax input,
        Func<IdentifierNameSyntax, ExpressionSyntax, IReadOnlyList<StatementSyntax>> createBody)
    {
        var inputName = CreateInlineAggregateInputName(aggregateSet.Accumulator);
        var currentName = CreateInlineAggregateCurrentName(aggregateSet.Accumulator);
        var current = SyntaxFactory.IdentifierName(currentName);
        var inputIdentifier = SyntaxFactory.IdentifierName(inputName);

        return StatementEmitter.CreateBlock(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                inputName,
                input), SyntaxFactory.IfStatement(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    inputIdentifier,
                    SyntaxFactory.IdentifierName("HasValue")),
                StatementEmitter.CreateBlock(
                [
                    CreateLocalDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        currentName,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                inputIdentifier,
                                SyntaxFactory.IdentifierName("GetValueOrDefault")))),
                    ..createBody(current, state)
                ])));
    }

    private static IReadOnlyList<StatementSyntax> CreateInlineSumStatements(
        IdentifierNameSyntax current,
        ExpressionSyntax state)
    {
        var value = CreateStateFieldAccess(state, "Value");
        return
        [
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                value,
                SyntaxFactory.ConditionalExpression(
                    CreateStateFieldAccess(state, "HasValue"),
                    SyntaxFactory.CheckedExpression(
                        SyntaxKind.CheckedExpression,
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.AddExpression,
                            value,
                            current)),
                    current))),
            CreateHasValueAssignment(state)
        ];
    }

    private static IReadOnlyList<StatementSyntax> CreateInlineAvgStatements(
        IdentifierNameSyntax current,
        ExpressionSyntax state)
    {
        var sum = CreateStateFieldAccess(state, "Sum");
        return
        [
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                sum,
                SyntaxFactory.ConditionalExpression(
                    CreateStateFieldAccess(state, "HasValue"),
                    SyntaxFactory.CheckedExpression(
                        SyntaxKind.CheckedExpression,
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.AddExpression,
                            sum,
                            current)),
                    current))),
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateStateFieldAccess(state, "Count"),
                SyntaxFactory.CheckedExpression(
                    SyntaxKind.CheckedExpression,
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        CreateStateFieldAccess(state, "Count"),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(1L)))))),
            CreateHasValueAssignment(state)
        ];
    }

    private static IReadOnlyList<StatementSyntax> CreateInlineExtremumStatements(
        IdentifierNameSyntax current,
        ExpressionSyntax state,
        SyntaxKind comparisonKind)
    {
        var value = CreateStateFieldAccess(state, "Value");
        return
        [
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LogicalOrExpression,
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateStateFieldAccess(state, "HasValue")),
                    SyntaxFactory.BinaryExpression(
                        comparisonKind,
                        current,
                        value)),
                StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        value,
                        current)))),
            CreateHasValueAssignment(state)
        ];
    }

    private static ExpressionStatementSyntax CreateHasValueAssignment(ExpressionSyntax state)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            CreateStateFieldAccess(state, "HasValue"),
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
    }

    private static ParenthesizedExpressionSyntax CreateNullableStateValueRead(ExpressionSyntax state, Type resultType)
    {
        return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.ConditionalExpression(
            CreateStateFieldAccess(state, "HasValue"),
            CastToResultType(CreateStateFieldAccess(state, "Value"), resultType),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    private static ParenthesizedExpressionSyntax CreateNullableAverageRead(ExpressionSyntax state, Type underlyingResultType, Type resultType)
    {
        return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.ConditionalExpression(
            CreateStateFieldAccess(state, "HasValue"),
            CastToResultType(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.DivideExpression,
                    CreateStateFieldAccess(state, "Sum"),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                CreateTypeSyntax(underlyingResultType),
                                SyntaxFactory.IdentifierName("CreateChecked")))
                        .WithArgumentList(CreateArgumentList(CreateStateFieldAccess(state, "Count")))),
                resultType),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    private static ExpressionSyntax CastToResultType(ExpressionSyntax expression, Type resultType)
    {
        return Nullable.GetUnderlyingType(resultType) is null
            ? expression
            : SyntaxFactory.CastExpression(CreateTypeSyntax(resultType), expression);
    }

    private static MemberAccessExpressionSyntax CreateStateFieldAccess(ExpressionSyntax state, string fieldName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            state,
            SyntaxFactory.IdentifierName(fieldName));
    }

    private static string CreateInlineAggregateInputName(AggregateAccumulatorField accumulator)
    {
        return $"{accumulator.FieldName}Input";
    }

    private static string CreateInlineAggregateCurrentName(AggregateAccumulatorField accumulator)
    {
        return $"{accumulator.FieldName}Current";
    }
}
