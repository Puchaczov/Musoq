using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class DistinctToGroupByTraverseVisitor(DistinctToGroupByVisitor visitor) : CloneTraverseVisitor(visitor)
{
    public RootNode Root => visitor.Root;
}
