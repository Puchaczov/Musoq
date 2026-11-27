using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Stack<bool> _groupByAllQueryFlags = new();

    internal void MarkCurrentQueryGroupByAll()
    {
        if (_groupByAllQueryFlags.Count == 0)
            return;

        _groupByAllQueryFlags.Pop();
        _groupByAllQueryFlags.Push(true);
    }

    private void GroupByAllQueryBegins()
    {
        _groupByAllQueryFlags.Push(false);
    }

    private void GroupByAllQueryEnds()
    {
        if (_groupByAllQueryFlags.Count > 0)
            _groupByAllQueryFlags.Pop();
    }
}
