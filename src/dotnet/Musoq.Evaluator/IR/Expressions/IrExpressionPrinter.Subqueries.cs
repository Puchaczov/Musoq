using System.Linq;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class IrExpressionPrinter
{
    protected override string VisitSubquery(SubqueryExpression node)
    {
        var inputs = string.Join(", ", node.Inputs.Select(Visit));
        var operation =
            $"Apply{node.Kind}{(node.Kind is SubqueryResultKind.Any or SubqueryResultKind.All ? $"<{node.Comparison}>" : string.Empty)}";
        return ForBinding && node.BindingKey is not null
            ? $"{operation}[{node.BindingKey}]({inputs})"
            : $"{operation}[{node.Identifier}]({inputs}; {LogicalPlanPrinter.Print(node.Query)})";
    }

    protected override string VisitSubqueryParameter(SubqueryParameterRef node) => $"outer[{node.Index}]";
}
