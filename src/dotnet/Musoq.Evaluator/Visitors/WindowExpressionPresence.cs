using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class WindowExpressionPresence
{
    internal static bool Contains(Node expression)
    {
        var detector = new Detector();
        expression.Accept(new Traverser(detector));
        return detector.Found;
    }

    private sealed class Traverser(IExpressionVisitor visitor) : RawTraverseVisitor<IExpressionVisitor>(visitor);

    private sealed class Detector : NoOpExpressionVisitor
    {
        internal bool Found { get; private set; }

        public override void Visit(WindowFunctionNode node) => Found = true;
    }
}
