using System.Linq;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private string ResolveSourceIdentifier(FromNode source)
    {
        return source switch
        {
            ExpressionFromNode expression => ResolveSourceIdentifier(expression.Expression),
            JoinFromNode or ApplyFromNode or JoinNode or ApplyNode => Scope[MetaAttributes.ProcessedQueryId],
            _ => Scope[source.Id],
        };
    }

    private static TableSymbol CreateJoinOutputSymbol(
        JoinType joinType,
        string outputId,
        TableSymbol left,
        TableSymbol right
    )
    {
        var output = joinType is JoinType.LeftSemi or JoinType.LeftAntiSemi
            ? left.WithFullTableName(outputId)
            : left.MergeSymbols(right);

        return joinType switch
        {
            JoinType.OuterLeft or JoinType.AsOfLeft or JoinType.LeftMark or JoinType.LeftSingle =>
                output.MarkAliasesAsMaybeMissing(right.CompoundTables),
            JoinType.OuterRight => output.MarkAliasesAsMaybeMissing(left.CompoundTables),
            JoinType.OuterFull => output.MarkAliasesAsMaybeMissing(left.CompoundTables.Concat(right.CompoundTables)),
            _ => output,
        };
    }
}
