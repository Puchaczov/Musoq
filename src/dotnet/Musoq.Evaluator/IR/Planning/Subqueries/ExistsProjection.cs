using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Logical.Rewriting;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal static class ExistsProjection
{
    // Binding has already checked the original projection. A simple EXISTS needs
    // only row presence. Duplicate elimination, grouping and windows retain their value inputs.
    internal static LogicalNode Prepare(LogicalNode query) => Prepare(query, false);

    private static LogicalNode Prepare(LogicalNode query, bool preserveSetSchema) =>
        query switch
        {
            MultiStatementNode { Statements: [.., var result] } statements => LogicalPlanRewriter.RewriteChildren(
                statements,
                child => ReferenceEquals(child, result) ? Prepare(child, preserveSetSchema) : child
            ),
            SortNode sort => Prepare(sort.Input, preserveSetSchema),
            TakeNode take => new TakeNode(take.Count, Prepare(take.Input, preserveSetSchema)),
            SkipNode skip => new SkipNode(skip.Count, Prepare(skip.Input, preserveSetSchema)),
            SetOperationNode { Kind: SetOpKind.UnionAll } append => new SetOperationNode(
                SetOpKind.UnionAll,
                Prepare(append.Left, true),
                Prepare(append.Right, true),
                []
            ),
            ProjectNode { IsDistinct: false } project when HasOnlyRowFilters(project.Input) => new ProjectNode(
                preserveSetSchema
                    ? project.Fields.Select(UndemandedSetField).ToArray()
                    : [new ProjectedField("__existsValue", new Literal(1, typeof(int)), 0)],
                project.Input
            ),
            CteNode cte => new CteNode(cte.Definitions, Prepare(cte.Query, preserveSetSchema)),
            _ => query,
        };

    // An empty CASE produces its typed fallback without evaluating the projection.
    // Both arms must retain the names and types established when the set was bound.
    private static ProjectedField UndemandedSetField(ProjectedField field) =>
        field with
        {
            Expression = new CaseWhen([], null, field.Expression.ReturnType),
        };

    private static bool HasOnlyRowFilters(LogicalNode node) =>
        node is SchemaScanNode or CteRefNode or ValuesScanNode
        || node is FilterNode filter && HasOnlyRowFilters(filter.Input);
}
