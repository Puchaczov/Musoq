using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal static class ExistsProjection
{
    // Binding has already checked the original projection. A simple EXISTS needs
    // only row presence. Distinct/grouped/window/set shapes keep their value inputs.
    internal static LogicalNode Prepare(LogicalNode query) => query switch
    {
        MultiStatementNode { Statements.Length: 1 } statements => new MultiStatementNode([Prepare(statements.Statements[0])]),
        SortNode sort => Prepare(sort.Input),
        TakeNode take => new TakeNode(take.Count, Prepare(take.Input)),
        SkipNode skip => new SkipNode(skip.Count, Prepare(skip.Input)),
        ProjectNode { IsDistinct: false } project when HasOnlyRowFilters(project.Input) =>
            new ProjectNode([new ProjectedField("__existsValue", new Literal(1, typeof(int)), 0)], project.Input),
        CteNode cte => new CteNode(cte.Definitions, Prepare(cte.Query)),
        _ => query
    };

    private static bool HasOnlyRowFilters(LogicalNode node) => node is SchemaScanNode or CteRefNode or ValuesScanNode ||
        node is FilterNode filter && HasOnlyRowFilters(filter.Input);
}
