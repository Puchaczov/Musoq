using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class NodeCloneExtensions
{
    internal static T CopySpansFrom<T>(this T target, Node source)
        where T : Node
    {
        if (source.HasSpan)
            target.WithSpan(source.Span);
        if (!source.FullSpan.IsEmpty && source.FullSpan != source.Span)
            target.WithFullSpan(source.FullSpan);
        return target;
    }
}
