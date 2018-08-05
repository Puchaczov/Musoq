using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public class CloneTraverseVisitor : RawTraverseVisitor<IExpressionVisitor>
    {
        public CloneTraverseVisitor(IExpressionVisitor visitor) 
            : base(visitor)
        {
        }
    }
}