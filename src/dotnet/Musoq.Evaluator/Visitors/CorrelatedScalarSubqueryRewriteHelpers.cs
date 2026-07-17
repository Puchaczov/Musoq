using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static bool RequiresResultMaterialization(QueryNode query)
    {
        return query.GroupBy != null ||
               query.OrderBy != null ||
               query.Skip != null ||
               query.Take != null ||
               query.Window != null ||
               query.Qualify != null ||
               query.Select.IsDistinct;
    }

    private static bool RequiresCorrelatedAggregateApply(QueryNode query)
    {
        return RequiresResultMaterialization(query);
    }

    private static bool RequiresUnsupportedCombinedScalarShape(QueryNode query)
    {
        var hasSlicing = query.Skip != null || query.Take != null;
        var hasPreSliceShaping = query.GroupBy != null ||
                                 query.Window != null ||
                                 query.Qualify != null ||
                                 query.Select.IsDistinct;
        return hasSlicing && hasPreSliceShaping;
    }

    private static AccessMethodNode CreateDeferredCorrelatedScalarAggregate(Node expression)
    {
        return new AccessMethodNode(
            new FunctionToken(CorrelatedScalarSubqueryAggregateName, default),
            new ArgsListNode([expression]),
            null,
            false)
        {
            IsScalarSubqueryValueWrapper = true
        };
    }

    private static AccessMethodNode CreateCorrelatedScalarResultAccessor(Node expression, string libraryAlias)
    {
        return new AccessMethodNode(
            new FunctionToken(CorrelatedScalarSubqueryResultName, default),
            new ArgsListNode([expression]),
            null,
            false,
            alias: libraryAlias);
    }
}
