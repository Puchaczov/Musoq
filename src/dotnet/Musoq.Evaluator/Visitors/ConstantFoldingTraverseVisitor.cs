using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Traverse visitor for constant folding.
///     Walks the AST bottom-up (children before parent) so inner expressions fold first.
///     Delegates actual folding decisions to <see cref="ConstantFoldingVisitor" />.
/// </summary>
public sealed class ConstantFoldingTraverseVisitor : RawTraverseVisitor<ConstantFoldingVisitor>
{
    public ConstantFoldingTraverseVisitor(ConstantFoldingVisitor visitor)
        : base(visitor)
    {
    }

    /// <summary>
    ///     Gets the transformed root node after folding.
    /// </summary>
    public RootNode Root => Visitor.Root;
}
