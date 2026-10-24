using System.Linq;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class IrExpressionPrinter
{
    protected override string VisitSubquery(SubqueryExpression node) =>
        $"Apply{node.Kind}{(node.Kind is SubqueryResultKind.Any or SubqueryResultKind.All ? $"<{node.Comparison}>" : string.Empty)}[{node.Identifier}]({string.Join(", ", node.Inputs.Select(Visit))}; {LogicalPlanPrinter.Print(node.Query)})";

    protected override string VisitSubqueryParameter(SubqueryParameterRef node) => $"outer[{node.Index}]";
}
