namespace Musoq.Evaluator.IR.Execution;

internal struct ExecutionNodeChildCursor(ExecutionNode owner)
{
    private readonly ExecutionNodeDescriptor? _descriptor = Resolve(owner);
    private int _regionIndex;
    private int _nodeIndex;
    private ExecutionChildRegion _region;

    internal ExecutionNode Current { get; private set; } = null!;

    internal bool MoveNext()
    {
        if (_descriptor is null)
            return false;
        while (_nodeIndex == _region.Count)
        {
            _region = _descriptor.GetChildRegion(owner, _regionIndex++);
            _nodeIndex = 0;
            if (!_region.IsPresent)
                return false;
        }

        Current = _region[_nodeIndex++];
        return true;
    }

    private static ExecutionNodeDescriptor? Resolve(ExecutionNode node) =>
        ExecutionNodeRegistry.TryGetDescriptor(node, out var descriptor) ? descriptor : null;
}
