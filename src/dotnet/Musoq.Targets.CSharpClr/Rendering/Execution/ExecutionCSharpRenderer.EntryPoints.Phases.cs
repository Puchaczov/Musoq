using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static IEnumerable<StatementSyntax> CreateOpeningPhaseStatements(ExecutionBlock block, string queryIdentifier)
    {
        if (ContainsInjectQueryStatsMethodCall(block))
            yield return CreateStatsDeclaration();
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
        yield return QueryEmitter.GenerateCompletionAndEndStatement(queryIdentifier);
    }
}
