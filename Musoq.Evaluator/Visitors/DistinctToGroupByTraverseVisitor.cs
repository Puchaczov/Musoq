using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class DistinctToGroupByTraverseVisitor : CloneTraverseVisitor
{
    private readonly DistinctToGroupByVisitor _distinctVisitor;

    public DistinctToGroupByTraverseVisitor(DistinctToGroupByVisitor visitor)
        : base(visitor)
    {
        _distinctVisitor = visitor;
    }

    public RootNode Root => _distinctVisitor.Root;
}