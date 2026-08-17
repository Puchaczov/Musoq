using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class UnreachableBranchAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        var variables = context.Metadata.ScriptVariableDefinitions
            .ToDictionary(static definition => definition.Name, StringComparer.Ordinal);

        Visit(context, context.Query, variables, new HashSet<Node>(ReferenceEqualityComparer.Instance));

        if (!ReferenceEquals(context.SourceQuery, context.Query))
        {
            Visit(context, context.SourceQuery, variables, new HashSet<Node>(ReferenceEqualityComparer.Instance));
        }
    }

    private static void Visit(
        SemanticAdvisoryContext context,
        Node node,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case CaseNode caseNode:
                AnalyzeCase(context, caseNode, variables);
                break;
            case CoalesceNode coalesce:
                AnalyzeCoalesce(context, coalesce, variables);
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Visit(context, child, variables, visited);
    }

    private static void AnalyzeCase(
        SemanticAdvisoryContext context,
        CaseNode caseNode,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        var seenDeterministicConditions = new HashSet<string>(StringComparer.Ordinal);
        var tailIsUnreachable = false;
        var tailWarningReported = false;

        for (var index = 0; index < caseNode.WhenThenPairs.Length; index++)
        {
            var pair = caseNode.WhenThenPairs[index];
            if (tailIsUnreachable)
            {
                if (!tailWarningReported)
                {
                    Report(context, pair.When, "A CASE branch follows a condition that is always true.");
                    tailWarningReported = true;
                }

                continue;
            }

            var condition = pair.When is WhenNode when ? when.Expression : pair.When;
            if (TryEvaluate(condition, variables, out var value))
            {
                if (value is bool boolValue && boolValue)
                {
                    tailIsUnreachable = index + 1 < caseNode.WhenThenPairs.Length;
                }
                else
                {
                    Report(context, pair.When, "A CASE WHEN condition is always false or UNKNOWN.");
                }

                continue;
            }

            if (IsDeterministic(condition))
            {
                if (!seenDeterministicConditions.Add(condition.Id))
                    Report(context, pair.When, "A deterministic CASE WHEN condition is duplicated and cannot be selected.");
            }
        }
    }

    private static void AnalyzeCoalesce(
        SemanticAdvisoryContext context,
        CoalesceNode coalesce,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (!IsProvablyNonNull(coalesce.Left, variables))
            return;

        var span = coalesce.Right.HasSpan ? coalesce.Right.Span : coalesce.Span;
        context.Report(
            DiagnosticCode.MQ5008_UnreachableCode,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5008_UnreachableCode),
            span);
    }

    private static bool IsProvablyNonNull(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (TryEvaluate(expression, variables, out var value))
            return value != null;

        if (expression is NullNode || expression.ReturnType is NullNode.NullType)
            return false;

        var type = expression.ReturnType;
        return type is { IsValueType: true } &&
               Nullable.GetUnderlyingType(type) == null &&
               type != typeof(void);
    }

    private static bool TryEvaluate(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        out object? value)
    {
        var result = ScriptVariableInitializerEvaluator.EvaluateStaticExpression(expression, variables);
        if (!result.Success)
        {
            value = null;
            return false;
        }

        value = result.Value;
        return true;
    }

    private static bool IsDeterministic(Node node)
    {
        if (node is AccessMethodNode or CaseNode or CoalesceNode)
            return false;

        if (node is ConstantValueNode or NullNode or AccessColumnNode or
            ScriptVariableReferenceNode or ParameterReferenceNode)
            return true;

        var children = ParserNodeTraversalRegistry.EnumerateChildren(node).ToArray();
        return children.Length > 0 && children.All(IsDeterministic);
    }

    private static void Report(SemanticAdvisoryContext context, Node node, string message)
    {
        if (!node.HasSpan && !ReferenceEquals(context.SourceQuery, context.Query))
            return;

        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        context.Report(DiagnosticCode.MQ5008_UnreachableCode, message, span);
    }
}
