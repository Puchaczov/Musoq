using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax RenderAggregateSet(ExecutionAggregateSet aggregateSet)
    {
        var kernel = aggregateSet.Accumulator.Kernel;
        var isUnitKernel = kernel.InputShape.ArgumentTypes.Count == 0;
        var input = aggregateSet.AccumulatorInput;
        if (!isUnitKernel && input == null)
            throw new InvalidOperationException("Typed aggregate set requires an input expression.");

        var setParameters = kernel.SetMethod.GetParameters().Skip(1).ToArray();
        var setArguments = AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(
            aggregateSet.Arguments);
        var useExplicitAccumulatorInput = setArguments.Length == 0 &&
                                          input is not null &&
                                          setParameters.Length == 1;
        if (!useExplicitAccumulatorInput && setParameters.Length != setArguments.Length)
        {
            throw new InvalidOperationException(
                $"Typed aggregate kernel {kernel.KernelType.FullName}.Set expects {setParameters.Length.ToString(CultureInfo.InvariantCulture)} value arguments, but aggregate set has {setArguments.Length.ToString(CultureInfo.InvariantCulture)}.");
        }

        var inlineSet = TryRenderInlineAggregateSet(
            aggregateSet,
            setArguments,
            setParameters,
            useExplicitAccumulatorInput);
        if (inlineSet is not null)
            return WrapAggregateSetWithFilter(aggregateSet.FilterPredicate, inlineSet);

        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(CreateAggregateAccumulatorAccess(aggregateSet.Group, aggregateSet.Accumulator))
                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword))
        };

        if (useExplicitAccumulatorInput)
        {
            arguments.Add(SyntaxFactory.Argument(CastIfNeeded(
                RenderExpression(input!),
                setParameters[0].ParameterType)));
        }
        else
        {
            for (var index = 0; index < setArguments.Length; index++)
            {
                arguments.Add(SyntaxFactory.Argument(CastIfNeeded(
                    RenderExpression(setArguments[index]),
                    setParameters[index].ParameterType)));
            }
        }

        var kernelInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateTypeSyntax(kernel.KernelType),
                    SyntaxFactory.IdentifierName("Set")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

        return WrapAggregateSetWithFilter(
            aggregateSet.FilterPredicate,
            SyntaxFactory.ExpressionStatement(kernelInvocation));
    }

    private StatementSyntax WrapAggregateSetWithFilter(
        ExecutionExpression? filterPredicate,
        StatementSyntax statement)
    {
        if (filterPredicate == null)
            return statement;

        return StatementEmitter.CreateIf(
            this.RenderBooleanCondition(filterPredicate, CreateIsolatedRenderContext()),
            StatementEmitter.CreateBlock(statement));
    }

    private ExpressionStatementSyntax RenderAggregateCapturedValueSet(ExecutionAggregateCapturedValueSet capturedValueSet)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(capturedValueSet.Group.Name),
                SyntaxFactory.IdentifierName(capturedValueSet.CapturedField.FieldName)),
            CastIfNeeded(RenderExpression(capturedValueSet.Value), capturedValueSet.CapturedField.Type)));
    }
}
