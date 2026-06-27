namespace Musoq.Evaluator.Utils;

using System;

public class ScopeWalker(Scope scope)
{
    private readonly ScopeWalker? _parent;
    private int _childIndex;

    private ScopeWalker(Scope scope, ScopeWalker parent)
        : this(scope)
    {
        _parent = parent;
    }

    public Scope Scope { get; } = scope;

    public ScopeWalker NextChild()
    {
        return new ScopeWalker(Scope.Child[_childIndex++], this);
    }

    public ScopeWalker Parent()
    {
        return _parent ?? throw new InvalidOperationException("Root scope walker has no parent.");
    }
}
