using Musoq.Evaluator;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Parser.Nodes;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static PreLogicalNormalizationResult? NormalizeQuery(
        RootNode queryTree,
        DiagnosticContext diagnostics)
    {
        return new PreLogicalNormalizer().TryNormalize(queryTree, diagnostics, out var result)
            ? result
            : null;
    }
}
