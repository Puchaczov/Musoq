using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyList<StatementSyntax> CreateQueryRunContextAliasStatements()
    {
        return
        [
            SyntaxFactory.ParseStatement("var token = queryContext.CancellationToken;"),
            SyntaxFactory.ParseStatement("var __musoqRuntimeParameters = queryContext.RuntimeParameters;"),
            SyntaxFactory.ParseStatement("Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;"),
            SyntaxFactory.ParseStatement("DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;")
        ];
    }
}
