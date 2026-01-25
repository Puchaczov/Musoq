using System.Collections.Generic;
using System.Diagnostics;

namespace Musoq.Evaluator.Utils;

[DebuggerDisplay("Name: '{Name}', ScopeId: {Id}")]
public class Scope
{
    private static int _scopeId;

    private readonly Dictionary<string, string> _attributes = new();
    private readonly List<Scope> _scopes = [];

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

    public Scope Parent { get; }

    public SymbolTable ScopeSymbolTable { get; } = new();

    public string this[string key]
    {
        get => _attributes.ContainsKey(key) ? _attributes[key] : Parent[key];
        set => _attributes[key] = value;
    }

    public Scope AddScope(string name = "")
    {
        var scope = new Scope(this, _scopes.Count, name);
        _scopes.Add(scope);
        return scope;
    }

    public bool ContainsAttribute(string attributeName)
    {
        return _attributes.ContainsKey(attributeName) || (Parent != null && Parent.ContainsAttribute(attributeName));
    }

    public bool IsInsideNamedScope(string name)
    {
        var scope = this;
        while (scope != null && scope.Name != name) scope = scope.Parent;
        return scope != null && scope.Name == name;
    }
}
