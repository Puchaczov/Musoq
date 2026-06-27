using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private static TableSymbol CreateApplyOutputSymbol(
        ApplyType applyType,
        TableSymbol left,
        TableSymbol right)
    {
        var output = left.MergeSymbols(right);

        return applyType == ApplyType.Outer
            ? output.MarkAliasesAsMaybeMissing(right.CompoundTables)
            : output;
    }
}
