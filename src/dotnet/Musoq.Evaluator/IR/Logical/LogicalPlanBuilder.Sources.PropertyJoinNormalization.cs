using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private LogicalNode CreateJoinSource(IrNodes.JoinNode join)
    {
        if (
            join is
            { OnPredicate: SubqueryExpression { Kind: SubqueryResultKind.Rows } rows, Right: IrNodes.CteRefNode schema }
        )
        {
            // The generated definition supplied the derived table's column contract during binding.
            // The captured query is its executable source and already owns filtering and shaping.
            _cteDefinitions.RemoveAll(definition => definition.Name == schema.CteName);
            var kind = join.Kind == IrNodes.JoinKind.Inner ? IrNodes.ApplyKind.Cross : IrNodes.ApplyKind.Outer;
            var sourceAlias = rows.Bindings.OfType<ColumnRef>().FirstOrDefault()?.Alias ?? string.Empty;
            var source = new IrNodes.AccessMethodSourceNode(
                sourceAlias,
                rows,
                schema.Alias,
                rows.ReturnType,
                kind,
                schema.OutputSchema
            );
            return new IrNodes.ApplyNode(kind, join.Left, source, join.WithOrdinality);
        }
        if (join is not { Kind: IrNodes.JoinKind.Inner, Right: IrNodes.PropertySourceNode, WithOrdinality: false })
            return join;

        // A property source is evaluated against its owning left row. Model that
        // dependency before planning so it cannot become an independent hash input.
        return new IrNodes.FilterNode(
            join.OnPredicate,
            new IrNodes.ApplyNode(IrNodes.ApplyKind.Cross, join.Left, join.Right)
        );
    }
}
