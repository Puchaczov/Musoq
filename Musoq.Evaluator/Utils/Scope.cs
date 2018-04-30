using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Utils
{
    public class Scope
    {
        private readonly List<Scope> _scopes = new List<Scope>();

        public Scope(Scope parent, int selfIndex)
        {
            Parent = parent;
            SelfIndex = selfIndex;
        }

        public IReadOnlyList<Scope> Child => _scopes;

        public int SelfIndex { get; }

        public Scope Parent { get; }

        public SymbolTable ScopeSymbolTable { get; } = new SymbolTable();

        public Scope AddScope(Func<Scope, int, Scope> createScope)
        {
            var scope = createScope(this, _scopes.Count);
            _scopes.Add(scope);
            return scope;
        }

        public Scope AddScope()
        {
            var scope = new Scope(this, _scopes.Count);
            _scopes.Add(scope);
            return scope;
        }
    }

    public class ScopeWalker
    {
        private readonly Scope _scope;
        private int _childIndex;
        private readonly ScopeWalker _parent = null;

        public Scope Scope => _scope;


        public ScopeWalker(Scope scope)
        {
            _scope = scope;
        }

        private ScopeWalker(Scope scope, ScopeWalker parent)
            : this(scope)
        {
            _parent = parent;
        }

        public ScopeWalker NextChild()
        {
            return new ScopeWalker(_scope.Child[_childIndex++], this);
        }

        public ScopeWalker PrevChild()
        {
            return new ScopeWalker(_scope.Child[_childIndex--], this);
        }

        public ScopeWalker Parent()
        {
            return _parent;
        }
    }
}