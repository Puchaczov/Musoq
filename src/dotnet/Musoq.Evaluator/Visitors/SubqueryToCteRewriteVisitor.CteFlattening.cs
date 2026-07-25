using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static CteInnerExpressionNode[] FlattenNestedCteDefinitions(CteInnerExpressionNode[] sets)
    {
        var flattened = new List<CteInnerExpressionNode>(sets.Length);

        foreach (var set in sets)
        {
            if (set.Value is not CteExpressionNode nested)
            {
                flattened.Add(set);
                continue;
            }

            flattened.AddRange(nested.InnerExpression);
            flattened.Add(new CteInnerExpressionNode(nested.OuterExpression, set.Name, set.Columns, set.IsRecursiveDefinition));
        }

        return flattened.ToArray();
    }
}
