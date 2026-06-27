using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    internal static IEnumerable<StatementSyntax> CreateOpeningPhaseStatements(ExecutionBlock block, string queryIdentifier)
    {
        yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.Begin);
        yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.From);

        if (ContainsNode<ExecutionIf>(block))
            yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.Where);

        if (ContainsAggregateNode(block))
            yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.GroupBy);

        if (ContainsInjectQueryStatsMethodCall(block))
            yield return CreateStatsDeclaration();

        foreach (var relatedQueryIdentifier in CreateRelatedPhaseQueryIdentifiers(block, queryIdentifier))
            yield return QueryEmitter.GeneratePhaseChangeStatement(relatedQueryIdentifier, QueryPhase.Begin);

        yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.Select);
    }

    private static LocalDeclarationStatementSyntax CreateStatsDeclaration()
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            StatsVariableName,
            SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(typeof(AmendableQueryStats)))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    internal static IEnumerable<StatementSyntax> CreateClosingPhaseStatements(ExecutionBlock block, string queryIdentifier)
    {
        foreach (var relatedQueryIdentifier in CreateRelatedPhaseQueryIdentifiers(block, queryIdentifier))
            yield return QueryEmitter.GeneratePhaseChangeStatement(relatedQueryIdentifier, QueryPhase.End);

        yield return QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.End);
    }

    private static string CreateRelatedCtePhaseQueryIdentifier(string queryIdentifier, int tableIndex)
    {
        return $"{queryIdentifier}:cte{tableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
