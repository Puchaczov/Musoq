using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class UnusedDeclarationAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        var source = context.SourceQuery;
        AnalyzeCteScopes(context, source, new HashSet<Node>(ReferenceEqualityComparer.Instance));
        AnalyzeScriptVariables(context, source);
    }

    private static void AnalyzeCteScopes(
        SemanticAdvisoryContext context,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is CteExpressionNode cte)
        {
            var graph = new CteDependencyGraphBuilder().Build(cte);
            foreach (var deadCte in graph.DeadCtes)
            {
                if (deadCte.AstNode is not { HasSpan: true } definition)
                    continue;

                context.Report(
                    DiagnosticCode.MQ5022_UnusedCte,
                    ErrorCatalog.GetMessage(DiagnosticCode.MQ5022_UnusedCte, deadCte.Name),
                    definition.Span);
            }
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            AnalyzeCteScopes(context, child, visited);
    }

    private static void AnalyzeScriptVariables(SemanticAdvisoryContext context, RootNode source)
    {
        var declarations = new Dictionary<string, ScriptVariableDeclarationNode>(StringComparer.Ordinal);
        CollectDeclarations(source, declarations, new HashSet<Node>(ReferenceEqualityComparer.Instance));
        if (declarations.Count == 0)
            return;

        var declarationNames = declarations.Keys.ToHashSet(StringComparer.Ordinal);
        var dependencies = declarations.Keys.ToDictionary(
            static name => name,
            static _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var declaration in declarations.Values)
            CollectVariableReferences(declaration.Initializer, declarationNames, dependencies[declaration.Name]);

        var roots = new HashSet<string>(StringComparer.Ordinal);
        CollectLiveReferences(
            source,
            declarationNames,
            roots,
            new HashSet<Node>(ReferenceEqualityComparer.Instance));

        var live = new HashSet<string>(roots, StringComparer.Ordinal);
        var pending = new Stack<string>(roots);
        while (pending.Count > 0)
        {
            var name = pending.Pop();
            foreach (var dependency in dependencies[name])
            {
                if (live.Add(dependency))
                    pending.Push(dependency);
            }
        }

        foreach (var (name, declaration) in declarations)
        {
            if (live.Contains(name))
                continue;

            context.Report(
                DiagnosticCode.MQ5023_UnusedScriptVariable,
                ErrorCatalog.GetMessage(DiagnosticCode.MQ5023_UnusedScriptVariable, name),
                GetDeclarationNameSpan(context, declaration));
        }
    }

    private static void CollectDeclarations(
        Node node,
        IDictionary<string, ScriptVariableDeclarationNode> declarations,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is ScriptVariableDeclarationNode declaration)
            declarations.TryAdd(declaration.Name, declaration);

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            CollectDeclarations(child, declarations, visited);
    }

    private static void CollectLiveReferences(
        Node node,
        IReadOnlySet<string> declarationNames,
        ISet<string> references,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        if (node is ScriptVariableDeclarationNode)
            return;

        if (node is ParameterReferenceNode parameter && declarationNames.Contains(parameter.Name))
            references.Add(parameter.Name);
        else if (node is ScriptVariableReferenceNode variable && declarationNames.Contains(variable.Name))
            references.Add(variable.Name);

        if (node is CteExpressionNode cte)
        {
            var graph = new CteDependencyGraphBuilder().Build(cte);
            CollectLiveReferences(cte.OuterExpression, declarationNames, references, visited);
            foreach (var reachable in graph.ReachableCtes)
            {
                if (reachable.AstNode is { } definition)
                    CollectLiveReferences(definition.Value, declarationNames, references, visited);
            }

            return;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            CollectLiveReferences(child, declarationNames, references, visited);
    }

    private static void CollectVariableReferences(
        Node node,
        IReadOnlySet<string> declarationNames,
        ISet<string> references)
    {
        if (node is ParameterReferenceNode parameter && declarationNames.Contains(parameter.Name))
            references.Add(parameter.Name);
        else if (node is ScriptVariableReferenceNode variable && declarationNames.Contains(variable.Name))
            references.Add(variable.Name);

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            CollectVariableReferences(child, declarationNames, references);
    }

    private static TextSpan GetDeclarationNameSpan(
        SemanticAdvisoryContext context,
        ScriptVariableDeclarationNode declaration)
    {
        var sourceText = context.Diagnostics.SourceText?.Text;
        if (sourceText == null || !declaration.HasSpan)
            return declaration.Span;

        var start = Math.Max(0, declaration.Span.Start);
        var end = Math.Min(sourceText.Length, declaration.Span.End);
        var nameStart = sourceText.IndexOf(declaration.Name, start, end - start, StringComparison.Ordinal);
        return nameStart >= 0
            ? new TextSpan(nameStart, declaration.Name.Length)
            : declaration.Span;
    }
}
