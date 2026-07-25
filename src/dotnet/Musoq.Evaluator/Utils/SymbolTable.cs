using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Utils;

public class SymbolTable
{
    private readonly Dictionary<object, Symbol> _symbols = new();

    public void AddSymbol(object key, Symbol symbol)
    {
        _symbols.Add(key, symbol);
    }

    public TSymbol AddOrGetSymbol<TSymbol>(object key) where TSymbol : Symbol, new()
    {
        if (_symbols.TryGetValue(key, out var symbol) && symbol is TSymbol castedSymbol) return castedSymbol;

        var newSymbol = new TSymbol();
        _symbols.Add(key, newSymbol);
        return newSymbol;
    }

    public void AddSymbolIfNotExist(object key, Symbol symbol)
    {
        _symbols.TryAdd(key, symbol);
    }

    public Symbol GetSymbol(object key)
    {
        return _symbols[key];
    }

    public TSymbol GetSymbol<TSymbol>(object key) where TSymbol : Symbol
    {
        return (TSymbol)GetSymbol(key);
    }

    public bool TryGetSymbol<TSymbol>(object key, [NotNullWhen(true)] out TSymbol? symbol)
    {
        if (_symbols.TryGetValue(key, out var plainSymbol) && plainSymbol is TSymbol castedSymbol)
        {
            symbol = castedSymbol;
            return true;
        }

        symbol = default;
        return false;
    }

    public void MoveSymbol(object oldKey, object newKey)
    {
        _symbols.Add(newKey, GetSymbol(oldKey));
        _symbols.Remove(oldKey);
    }

    public void UpdateSymbol(object oldKey, Symbol symbol)
    {
        _symbols.Remove(oldKey);
        _symbols.Add(oldKey, symbol);
    }

    public bool SymbolIsOfType<TType>(object key) where TType : Symbol
    {
        return _symbols.TryGetValue(key, out var symbol) && symbol.GetType() == typeof(TType);
    }

    internal IReadOnlyList<ScopeSymbolSnapshot> Snapshot()
    {
        return _symbols
            .Select(static pair => new ScopeSymbolSnapshot(pair.Key, SymbolSnapshotCloner.Clone(pair.Value)))
            .ToArray();
    }

    internal void Restore(IEnumerable<ScopeSymbolSnapshot> symbols)
    {
        foreach (var symbol in symbols)
            _symbols.Add(symbol.Key, SymbolSnapshotCloner.Clone(symbol.Value));
    }
}
