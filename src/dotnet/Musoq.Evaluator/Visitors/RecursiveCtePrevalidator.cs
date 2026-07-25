using Musoq.Parser.Nodes;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Visitors;

public static class RecursiveCtePrevalidator
{
    public static void Validate(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ValidateNode(root);
    }

    private static void ValidateNode(Node node)
    {
        if (node is CteExpressionNode cte)
        {
            ValidateColumnNames(cte);
            _ = new RecursiveCteShapeAnalyzer().AnalyzeRawSyntax(cte);
        }

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            ValidateNode(child);
    }

    private static void ValidateColumnNames(CteExpressionNode cte)
    {
        foreach (var definition in cte.InnerExpression)
        {
            if (!CteColumnListValidator.TryFindDuplicate(definition, out var failure))
                continue;

            throw new CteColumnListValidationException(
                DiagnosticCode.MQ3078_DuplicateCteColumnName,
                failure.Message,
                failure.Span);
        }
    }
}
