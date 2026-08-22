using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyList<StatementSyntax> CreateQueryRunContextAliasStatements(
        bool useQueryRunContext,
        bool useInstanceSender = true,
        bool includePhaseAlias = true,
        bool includeProgressAlias = true,
        bool includeProgressContextAlias = true,
        string? queryIdentifier = null)
    {
        if (useQueryRunContext)
        {
            return
            [
                SyntaxFactory.ParseStatement("var token = queryContext.CancellationToken;"),
                SyntaxFactory.ParseStatement("var __musoqRuntimeParameters = queryContext.RuntimeParameters;"),
                SyntaxFactory.ParseStatement("var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;"),
                SyntaxFactory.ParseStatement("Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;"),
                SyntaxFactory.ParseStatement("DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;"),
                SyntaxFactory.ParseStatement("QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;")
            ];
        }

        var sender = useInstanceSender ? "this" : "null";
        var effectiveQueryIdentifier = queryIdentifier ?? "compiled";
        var escapedQueryIdentifier = effectiveQueryIdentifier
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        var statements = new List<StatementSyntax>();
        if (includeProgressAlias)
            statements.Add(SyntaxFactory.ParseStatement("QueryProgressEventHandler OnQueryProgress = QueryProgress;"));
        if (includeProgressContextAlias)
            statements.Add(SyntaxFactory.ParseStatement(
                $"var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: {sender}, queryId: \"{escapedQueryIdentifier}\");"));
        if (includePhaseAlias)
            statements.Add(SyntaxFactory.ParseStatement("Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;"));
        return statements;
    }
}
