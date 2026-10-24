using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal sealed class SubqueryBindingFrame : IDisposable
{
    private readonly SemanticAnalysisState _state;
    private readonly KeyValuePair<string, SchemaFromNode>[] _sources;
    private readonly KeyValuePair<string, string>[] _tables;
    private readonly AccessMethodNode[] _refresh;
    private readonly Type[] _caseTypes;
    private readonly QueryPart _part;
    private readonly bool _insideWindowFunction;
    private readonly string _identifier;
    private readonly string _alias;
    private readonly int _quantity;
    private readonly int _sourceKey;
    private readonly int _methodDepth;

    internal SubqueryBindingFrame(SemanticAnalysisState state)
    {
        _state = state;
        var source = state.SourceBinding;
        _sources = source.AliasToSchemaFromNodeMap.ToArray();
        Identities = new Dictionary<string, (int, string)>(source.SchemaFromInfo);
        _tables = source.AliasMapToInMemoryTableMap.ToArray();
        _refresh = state.MethodResolution.RefreshMethods.ToArray();
        _caseTypes = state.Diagnostics.NullSuspiciousTypes.ToArray();
        _part = state.Query.QueryPart;
        _insideWindowFunction = state.Query.InsideWindowFunction;
        state.Query.InsideWindowFunction = false;
        _identifier = source.Identifier;
        _alias = source.QueryAlias;
        _quantity = source.UsedSchemasQuantity;
        _sourceKey = source.SchemaFromKey;
        _methodDepth = state.Traversal.MethodCount;
        VisibleAliases = source.CurrentScope.ScopeSymbolTable.GetSymbols<TableSymbol>()
            .SelectMany(static table => table.CompoundTables).ToHashSet(StringComparer.OrdinalIgnoreCase);
        source.AliasToSchemaFromNodeMap.Clear();
        source.SchemaFromInfo.Clear();
        source.AliasMapToInMemoryTableMap.Clear();
        state.MethodResolution.RefreshMethods.Clear();
        state.Diagnostics.NullSuspiciousTypes.Clear();
        source.Identifier = string.Empty;
        source.QueryAlias = string.Empty;
        source.UsedSchemasQuantity = 0;
    }

    internal IReadOnlyDictionary<string, (int SchemaFromKey, string SourceContextId)> Identities { get; }
    internal IReadOnlySet<string> VisibleAliases { get; }

    public void Dispose()
    {
        var source = _state.SourceBinding;
        Restore(source.AliasToSchemaFromNodeMap, _sources);
        Restore(source.SchemaFromInfo, Identities);
        Restore(source.AliasMapToInMemoryTableMap, _tables);
        _state.MethodResolution.RefreshMethods.Clear();
        _state.MethodResolution.RefreshMethods.AddRange(_refresh);
        _state.Diagnostics.NullSuspiciousTypes.Clear();
        _state.Diagnostics.NullSuspiciousTypes.AddRange(_caseTypes);
        _state.Query.QueryPart = _part;
        _state.Query.InsideWindowFunction = _insideWindowFunction;
        source.Identifier = _identifier;
        source.QueryAlias = _alias;
        source.UsedSchemasQuantity = _quantity;
        source.SchemaFromKey = _sourceKey;
        while (_state.Traversal.MethodCount > _methodDepth)
            _state.Traversal.PopMethod("BuildMetadataAndInferTypesVisitor", "subquery scope");
    }

    private static void Restore<TKey, TValue>(IDictionary<TKey, TValue> target,
        IEnumerable<KeyValuePair<TKey, TValue>> values) where TKey : notnull
    {
        target.Clear();
        foreach (var pair in values)
            target.Add(pair);
    }
}
