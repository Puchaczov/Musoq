using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static partial class PredicateAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        var resolver = new PredicateConstantResolver(context.Query);
        var variables = context.Metadata.ScriptVariableDefinitions
            .ToDictionary(static definition => definition.Name, StringComparer.Ordinal);
        VisitStructure(context, resolver, variables, context.Query,
            new HashSet<Node>(ReferenceEqualityComparer.Instance));
    }

    private static void VisitStructure(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        Node node,
        HashSet<Node> visited)
    {
        if (!visited.Add(node))
            return;

        switch (node)
        {
            case WhereNode where:
                AnalyzePredicate(context, resolver, variables, where.Expression, "WHERE");
                break;
            case HavingNode having:
                AnalyzePredicate(context, resolver, variables, having.Expression, "HAVING");
                break;
            case QualifyNode qualify:
                AnalyzePredicate(context, resolver, variables, qualify.Expression, "QUALIFY");
                break;
            case JoinFromNode join:
                AnalyzePredicate(context, resolver, variables, join.Expression, "ON");
                break;
            case JoinSourcesTableFromNode join:
                AnalyzePredicate(context, resolver, variables, join.Expression, "ON");
                break;
            case JoinInMemoryWithSourceTableFromNode join:
                AnalyzePredicate(context, resolver, variables, join.Expression, "ON");
                break;
            case AccessMethodNode method when method.FilterExpression is { } filter:
                AnalyzePredicate(context, resolver, variables, filter, "FILTER");
                break;
        }

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            VisitStructure(context, resolver, variables, child, visited);
    }

    private static void AnalyzePredicate(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        Node predicate,
        string clauseName)
    {
        var analyzed = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        AnalyzePredicateNode(context, resolver, variables, predicate, analyzed, true, clauseName);
    }

    private static void AnalyzePredicateNode(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        Node node,
        HashSet<Node> analyzed,
        bool reportGeneric,
        string clauseName)
    {
        if (!analyzed.Add(node))
            return;

        if (TryReportNullComparison(context, resolver, node))
            return;

        if (reportGeneric &&
            node is IsNullNode { IsNegated: false, Expression: AccessColumnNode column } &&
            IsStaticallyNonNullable(column.ReturnType))
        {
            context.Report(
                DiagnosticCode.MQ5011_ContradictoryCondition,
                "Predicate checks IS NULL on a statically non-nullable column and can never be true.",
                node.Span);
            return;
        }

        if (reportGeneric)
            ReportConstantPredicate(context, variables, node, clauseName);

        if (node is AndNode and)
        {
            ReportProvenAnd(context, resolver, and);
            AnalyzePredicateNode(context, resolver, variables, and.Left, analyzed, false, clauseName);
            AnalyzePredicateNode(context, resolver, variables, and.Right, analyzed, false, clauseName);
            return;
        }

        if (node is OrNode or)
        {
            ReportProvenOr(context, resolver, or);
            AnalyzePredicateNode(context, resolver, variables, or.Left, analyzed, false, clauseName);
            AnalyzePredicateNode(context, resolver, variables, or.Right, analyzed, false, clauseName);
            return;
        }

        if (node is NotNode not)
        {
            AnalyzePredicateNode(context, resolver, variables, not.Expression, analyzed, false, clauseName);
            return;
        }
    }

    private static bool TryReportNullComparison(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        Node node)
    {
        if (!IsComparison(node) || !TryFindNullOperand(resolver, node, out var nullOperand))
            return false;

        var symbol = GetComparisonSymbol(node);
        var span = nullOperand.HasSpan ? nullOperand.Span : node.Span;
        context.Report(
            DiagnosticCode.MQ5017_NullComparison,
            ErrorCatalog.GetMessage(DiagnosticCode.MQ5017_NullComparison, symbol),
            span);
        return true;
    }

    private static void ReportConstantPredicate(
        SemanticAdvisoryContext context,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        Node node,
        string clauseName)
    {
        if (ContainsNullComparison(node, variables))
            return;

        var result = ScriptVariableInitializerEvaluator.EvaluateStaticExpression(node, variables);
        if (!result.Success || result.Value is not bool value)
            return;

        var code = value
            ? DiagnosticCode.MQ5010_TautologicalCondition
            : DiagnosticCode.MQ5011_ContradictoryCondition;
        var message = value
            ? $"{clauseName} clause always evaluates to true and has no effect."
            : $"{clauseName} clause always evaluates to false; no rows will be returned.";
        context.Report(code, message, node.Span);
    }

    private static bool ContainsNullComparison(
        Node node,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (IsComparison(node) && HasNullConstant(node, variables))
            return true;

        if (node is not AndNode and not OrNode and not NotNode)
            return false;

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            if (ContainsNullComparison(child, variables))
                return true;

        return false;
    }

    private static bool IsComparison(Node node) => node is EqualityNode or DiffNode or GreaterNode or
        GreaterOrEqualNode or LessNode or LessOrEqualNode;

    private static string GetComparisonSymbol(Node node) => node switch
    {
        EqualityNode => "=",
        DiffNode => "<>",
        GreaterNode => ">",
        GreaterOrEqualNode => ">=",
        LessNode => "<",
        LessOrEqualNode => "<=",
        _ => "comparison"
    };

    private static bool IsStaticallyNonNullable(Type? type)
    {
        return type is { IsValueType: true } && Nullable.GetUnderlyingType(type) is null;
    }
}
