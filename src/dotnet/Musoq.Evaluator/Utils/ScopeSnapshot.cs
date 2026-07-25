using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Utils;

/// <summary>
/// Immutable, materializable copy of the semantic scope tree. The mutable
/// <see cref="Scope"/> object is deliberately kept out of phase artifacts;
/// consumers receive a fresh compatibility scope for each operation.
/// </summary>
internal sealed class ScopeSnapshot
{
    private readonly ScopeNodeSnapshot _root;

    private ScopeSnapshot(ScopeNodeSnapshot root)
    {
        _root = root;
    }

    public static ScopeSnapshot Capture(Scope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ScopeSnapshot(CaptureNode(scope));
    }

    public Scope CreateScope()
    {
        return _root.Materialize(null);
    }

    private static ScopeNodeSnapshot CaptureNode(Scope scope)
    {
        return new ScopeNodeSnapshot(
            scope.SelfIndex,
            scope.Name,
            scope.SnapshotAttributes(),
            scope.ScopeSymbolTable.Snapshot(),
            scope.Child.Select(CaptureNode).ToArray());
    }

    private sealed class ScopeNodeSnapshot
    {
        public ScopeNodeSnapshot(
            int selfIndex,
            string name,
            IReadOnlyDictionary<string, string> attributes,
            IReadOnlyList<ScopeSymbolSnapshot> symbols,
            IReadOnlyList<ScopeNodeSnapshot> children)
        {
            SelfIndex = selfIndex;
            Name = name;
            Attributes = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(attributes, StringComparer.Ordinal));
            Symbols = new ReadOnlyCollection<ScopeSymbolSnapshot>(symbols.ToArray());
            Children = new ReadOnlyCollection<ScopeNodeSnapshot>(children.ToArray());
        }

        private int SelfIndex { get; }

        private string Name { get; }

        private IReadOnlyDictionary<string, string> Attributes { get; }

        private IReadOnlyList<ScopeSymbolSnapshot> Symbols { get; }

        private IReadOnlyList<ScopeNodeSnapshot> Children { get; }

        public Scope Materialize(Scope? parent)
        {
            var scope = new Scope(parent, SelfIndex, Name);
            scope.RestoreAttributes(Attributes);
            scope.ScopeSymbolTable.Restore(Symbols);

            foreach (var child in Children)
                scope.AddRestoredChild(child.Materialize(scope));

            return scope;
        }
    }
}

internal sealed record ScopeSymbolSnapshot(object Key, Symbol Value);

internal static class SymbolSnapshotCloner
{
    public static Symbol Clone(Symbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return symbol switch
        {
            TableSymbol table => table.WithFullTableName(string.Concat(table.CompoundTables)),
            AliasesSymbol aliases => aliases.Clone(),
            AliasesPositionsSymbol positions => positions.Clone(),
            FieldsNamesSymbol fields => fields.Clone(),
            IndexBasedContextsPositionsSymbol contexts => contexts.Clone(),
            RefreshMethodsSymbol refreshMethods => refreshMethods.Clone(),
            _ => symbol
        };
    }
}
