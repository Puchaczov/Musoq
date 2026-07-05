using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record InlineAggregateFusionCandidate(
        ExecutionAggregateSet AggregateSet,
        AggregateInlineKind Kind,
        ExecutionExpression Input,
        string GroupName,
        string InputFingerprint,
        Type ParameterType);

    private IEnumerable<StatementSyntax> RenderBlockNodes(
        IReadOnlyList<ExecutionNode> nodes,
        ExecutionRenderContext context)
    {
        for (var index = 0; index < nodes.Count;)
        {
            if (TryRenderFusedInlineAggregateSets(nodes, index, context, out var fusedStatements, out var consumed))
            {
                foreach (var statement in fusedStatements)
                    yield return statement;

                index += consumed;
                continue;
            }

            foreach (var statement in RenderNode(nodes[index], context))
                yield return statement;

            index++;
        }
    }

    private bool TryRenderFusedInlineAggregateSets(
        IReadOnlyList<ExecutionNode> nodes,
        int start,
        ExecutionRenderContext context,
        out IReadOnlyList<StatementSyntax> statements,
        out int consumed)
    {
        statements = [];
        consumed = 0;

        if (IsOperatorProfilingEnabledFor(context) ||
            nodes[start] is not ExecutionAggregateSet firstAggregateSet ||
            !TryCreateInlineAggregateFusionCandidate(firstAggregateSet, out var firstCandidate))
        {
            return false;
        }

        var candidates = new List<InlineAggregateFusionCandidate> { firstCandidate };
        var index = start + 1;
        while (index < nodes.Count &&
               nodes[index] is ExecutionAggregateSet nextAggregateSet &&
               TryCreateInlineAggregateFusionCandidate(nextAggregateSet, out var nextCandidate) &&
               CanFuseInlineAggregateCandidate(firstCandidate, nextCandidate))
        {
            candidates.Add(nextCandidate);
            index++;
        }

        if (candidates.Count < 2)
            return false;

        statements = [RenderFusedInlineAggregateSets(candidates)];
        consumed = candidates.Count;
        return true;
    }

    private bool TryCreateInlineAggregateFusionCandidate(
        ExecutionAggregateSet aggregateSet,
        out InlineAggregateFusionCandidate candidate)
    {
        candidate = null!;

        if (aggregateSet.FilterPredicate != null)
        {
            return false;
        }

        var kind = AggregateInlinePolicy.Resolve(aggregateSet.Accumulator.Kernel);
        if (!CanFuseInlineAggregateKind(kind))
            return false;

        var setParameters = GetAggregateSetValueParameters(aggregateSet);
        var setArguments = AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(aggregateSet.Arguments);
        var input = setArguments.Length == 0 && aggregateSet.AccumulatorInput is not null
            ? aggregateSet.AccumulatorInput
            : setArguments.Length == 1
                ? setArguments[0]
                : null;

        if (setParameters.Length != 1 ||
            !CanFuseNullableAggregateInput(setParameters[0].ParameterType) ||
            input == null ||
            !CanFuseInlineAggregateInput(input))
        {
            return false;
        }

        candidate = new InlineAggregateFusionCandidate(
            aggregateSet,
            kind,
            input,
            aggregateSet.Group.Name,
            ExecutionExpressionFingerprint.ForHoist(input),
            setParameters[0].ParameterType);
        return true;
    }

    private static ParameterInfo[] GetAggregateSetValueParameters(ExecutionAggregateSet aggregateSet)
    {
        return aggregateSet.Accumulator.Kernel.SetMethod.GetParameters().Skip(1).ToArray();
    }

    private static bool CanFuseInlineAggregateCandidate(
        InlineAggregateFusionCandidate first,
        InlineAggregateFusionCandidate next)
    {
        return first.GroupName == next.GroupName &&
               first.InputFingerprint == next.InputFingerprint &&
               first.ParameterType == next.ParameterType;
    }

    private static bool CanFuseInlineAggregateKind(AggregateInlineKind kind)
    {
        return kind is AggregateInlineKind.Sum or
            AggregateInlineKind.Avg or
            AggregateInlineKind.Min or
            AggregateInlineKind.Max;
    }

    private static bool CanFuseNullableAggregateInput(Type type)
    {
        return Nullable.GetUnderlyingType(type)?.IsValueType == true;
    }

    private static bool CanFuseInlineAggregateInput(ExecutionExpression input)
    {
        return input is ExecutionVariableRead or ExecutionFieldRead or ExecutionLiteral;
    }

    private BlockSyntax RenderFusedInlineAggregateSets(
        IReadOnlyList<InlineAggregateFusionCandidate> candidates)
    {
        var first = candidates[0];
        var inputName = CreateInlineAggregateInputName(first.AggregateSet.Accumulator);
        var currentName = CreateInlineAggregateCurrentName(first.AggregateSet.Accumulator);
        var current = SyntaxFactory.IdentifierName(currentName);
        var inputIdentifier = SyntaxFactory.IdentifierName(inputName);
        var bodyStatements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                currentName,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        inputIdentifier,
                        SyntaxFactory.IdentifierName("GetValueOrDefault"))))
        };

        foreach (var candidate in candidates)
            bodyStatements.AddRange(CreateFusedInlineAggregateUpdateStatements(candidate, current));

        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                inputName,
                CastIfNeeded(RenderExpression(first.Input), first.ParameterType)),
            SyntaxFactory.IfStatement(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    inputIdentifier,
                    SyntaxFactory.IdentifierName("HasValue")),
                StatementEmitter.CreateBlock(bodyStatements)));
    }

    private static IReadOnlyList<StatementSyntax> CreateFusedInlineAggregateUpdateStatements(
        InlineAggregateFusionCandidate candidate,
        IdentifierNameSyntax current)
    {
        var state = CreateAggregateAccumulatorAccess(candidate.AggregateSet.Group, candidate.AggregateSet.Accumulator);
        return candidate.Kind switch
        {
            AggregateInlineKind.Sum => CreateInlineSumStatements(current, state),
            AggregateInlineKind.Avg => CreateInlineAvgStatements(current, state),
            AggregateInlineKind.Min => CreateInlineExtremumStatements(current, state, SyntaxKind.LessThanExpression),
            AggregateInlineKind.Max => CreateInlineExtremumStatements(current, state, SyntaxKind.GreaterThanExpression),
            _ => []
        };
    }
}
