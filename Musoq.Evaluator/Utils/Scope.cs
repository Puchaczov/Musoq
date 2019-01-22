using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Utils
{
    [DebuggerDisplay("Name: '{Name}', ScopeId: {Id}")]
    public class Scope
    {
        private static int _scopeId;

        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();
        private readonly List<Scope> _scopes = new List<Scope>();

        public Scope(Scope parent, int selfIndex, string name = "")
        {
            Parent = parent;
            SelfIndex = selfIndex;
            Id = _scopeId++;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyList<Scope> Child => _scopes;

        public int SelfIndex { get; }

        public Scope Parent { get; private set; }

        public Scope Root {
            get
            {
                if (Parent == null)
                    return this;

                var prev = Parent;
                var rootCandidate = Parent;
                while (rootCandidate != null)
                {
                    prev = rootCandidate;
                    rootCandidate = rootCandidate.Parent;
                }

                return prev;
            }
        }

        public Scope Query
        {
            get
            {
                if (Name == "Query")
                    return this;

                if (Parent == null)
                    return null;

                var prev = Parent;
                var rootCandidate = Parent;
                while (rootCandidate != null)
                {
                    if (rootCandidate.Name == "Query")
                        break;

                    prev = rootCandidate;
                    rootCandidate = rootCandidate.Parent;
                }

                return prev;
            }
        }

        public SymbolTable ScopeSymbolTable { get; } = new SymbolTable();

        public StringBuilder Script { get; } = new StringBuilder();

        public string this[string key]
        {
            get => _attributes.ContainsKey(key) ? _attributes[key] : Parent[key];
            set => _attributes[key] = value;
        }

        public Scope AddScope(Func<Scope, int, Scope> createScope)
        {
            var scope = createScope(this, _scopes.Count);
            _scopes.Add(scope);
            return scope;
        }

        public Scope AddScope(string name = "")
        {
            var scope = new Scope(this, _scopes.Count, name);
            _scopes.Add(scope);
            return scope;
        }

        public void RemoveScope(Scope scope)
        {
            _scopes.Remove(scope);
            scope.Parent = null;
        }

        public bool ContainsAttribute(string attributeName)
        {
            return _attributes.ContainsKey(attributeName) || Parent != null && Parent.ContainsAttribute(attributeName);
        }

        public bool IsInsideNamedScope(string name)
        {
            var scope = this;
            while (scope != null && scope.Name != name) scope = scope.Parent;
            return scope != null && scope.Name == name;
        }
    }
}