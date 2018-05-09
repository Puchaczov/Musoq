using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Utils
{
    public class Scope
    {
        private readonly List<Scope> _scopes = new List<Scope>();

        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();

        public Scope(Scope parent, int selfIndex)
        {
            Parent = parent;
            SelfIndex = selfIndex;
        }

        public IReadOnlyList<Scope> Child => _scopes;

        public int SelfIndex { get; }

        public Scope Parent { get; private set; }

        public SymbolTable ScopeSymbolTable { get; } = new SymbolTable();

        public StringBuilder Script { get; } = new StringBuilder();

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

        public void RemoveScope(Scope scope)
        {
            _scopes.Remove(scope);
            scope.Parent = null;
        }

        public string this[string key]
        {
            get => _attributes.ContainsKey(key) ? _attributes[key] : Parent[key];
            set => _attributes[key] = value;
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