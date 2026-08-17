using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderRecursiveCteAppend(
        ExecutionRecursiveCteAppend append,
        ExecutionRenderContext context)
    {
        var acceptedRowCounter = CreateAcceptedRecursiveRowCounter(append, context);
        foreach (var cancellationStatement in CreateRecursiveCandidateCancellationCheck(context))
            yield return cancellationStatement;
        if (append.Seen == null)
        {
            yield return CreateRecursiveRowLimitGuard(append);
            yield return RenderAppendRow(append.AppendRow, context);
            if (acceptedRowCounter != null)
                yield return acceptedRowCounter;
            yield break;
        }

        var candidateValues = new ExecutionRowValue[append.AppendRow.Values.Count];
        var candidateVariables = new ExecutionVariable[append.AppendRow.Values.Count];
        for (var index = 0; index < append.AppendRow.Values.Count; index++)
        {
            var value = append.AppendRow.Values[index];
            var isNullLiteral = value.Value is ExecutionLiteral
            {
                Value.Kind: ExecutionConstantKind.Null
            };
            var candidateType = isNullLiteral
                ? append.AppendRow.RowShape.Fields[index].Type
                : value.Value.ReturnType;
            var variable = new ExecutionVariable(
                $"__{append.Frontier.Name}Candidate{index}",
                candidateType);
            yield return CreateLocalDeclaration(
                isNullLiteral ? CreateTypeSyntax(candidateType) : SyntaxFactory.IdentifierName("var"),
                variable.Name,
                RenderExpression(value.Value, context));
            candidateVariables[index] = variable;
            candidateValues[index] = value with { Value = new ExecutionVariableRead(variable) };
        }

        var acceptedStatements = new List<StatementSyntax>
        {
            CreateRecursiveRowLimitGuard(append)
        };
        acceptedStatements.Add(RenderAppendRow(
            append.AppendRow with { Values = candidateValues },
            context));
        if (acceptedRowCounter != null)
            acceptedStatements.Add(acceptedRowCounter);
        yield return SyntaxFactory.IfStatement(
            RecursiveCteIdentitySyntaxFactory.CreateSeenAddExpression(append, candidateVariables),
            StatementEmitter.CreateBlock(acceptedStatements));
    }

    private static IEnumerable<StatementSyntax> CreateRecursiveCandidateCancellationCheck(
        ExecutionRenderContext context)
    {
        var counterName = context.Session.RecursiveCteCancellationCounterName;
        if (counterName == null)
            return [];

        return
        [
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.PreIncrementExpression,
                    SyntaxFactory.IdentifierName(counterName))),
            CreatePeriodicCancellationCheck(counterName)
        ];
    }

    private StatementSyntax? CreateAcceptedRecursiveRowCounter(
        ExecutionRecursiveCteAppend append,
        ExecutionRenderContext context)
    {
        return IsOperatorProfilingEnabledFor(context) &&
               context.Session.OperatorCatalog.TryGetDescriptor(append, out var descriptor)
            ? OperatorProfileCounterFacts.CreateCounterRowsStatement(
                OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(1)))
            : null;
    }
}
