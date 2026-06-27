using System.Linq;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private static TableSymbol CreateJoinOutputSymbol(
        JoinType joinType,
        string outputId,
        TableSymbol left,
        TableSymbol right)
    {
        var output = joinType is JoinType.LeftSemi or JoinType.LeftAntiSemi
            ? left.WithFullTableName(outputId)
            : left.MergeSymbols(right);

        return joinType switch
        {
            JoinType.OuterLeft or JoinType.AsOfLeft =>
                output.MarkAliasesAsMaybeMissing(right.CompoundTables),
            JoinType.OuterRight =>
                output.MarkAliasesAsMaybeMissing(left.CompoundTables),
            JoinType.OuterFull =>
                output.MarkAliasesAsMaybeMissing(left.CompoundTables.Concat(right.CompoundTables)),
            _ => output
        };
    }
}
