using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        sets = FlattenNestedCteDefinitions(sets);
        var outer = Nodes.Pop();

        if (outer is CteExpressionNode innerCte)
        {
            var mergedSets = sets.Concat(innerCte.InnerExpression).ToArray();
            Nodes.Push(new CteExpressionNode(mergedSets, innerCte.OuterExpression));
        }
        else
        {
            Nodes.Push(new CteExpressionNode(sets, outer));
        }
    }
}
