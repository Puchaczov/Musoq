using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Schema;

namespace Musoq.Evaluator.Utils;

/// <summary>
///     Provider-independent column metadata used by semantic handoffs.
/// </summary>
internal sealed class BoundSchemaColumn : ISchemaColumn
{
    private BoundSchemaColumn(
        string columnName,
        int columnIndex,
        Type columnType,
        string? intendedTypeName,
        IReadOnlyDictionary<string, string> readModifiers)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
        IntendedTypeName = intendedTypeName;
        ReadModifiers = readModifiers;
        IsNullable = Nullable.GetUnderlyingType(columnType) != null || !columnType.IsValueType;
    }

    public string ColumnName { get; }

    public int ColumnIndex { get; }

    public Type ColumnType { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }

    public string? IntendedTypeName { get; }

    public bool IsNullable { get; }

    public static BoundSchemaColumn Capture(ISchemaColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return new BoundSchemaColumn(
            column.ColumnName,
            column.ColumnIndex,
            column.ColumnType,
            column.IntendedTypeName,
            new ReadOnlyDictionary<string, string>(
                (column.ReadModifiers ?? new Dictionary<string, string>())
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)));
    }
}

internal sealed record BoundSourceContract(
    string SourceNodeId,
    SourceIdentity Identity,
    IReadOnlyList<BoundSchemaColumn> Columns,
    IReadOnlyList<string> RequiredMemberSignatures,
    string RequiredMethodSignature);

internal sealed record BoundTableSymbolContract(
    bool HasAlias,
    IReadOnlyList<BoundTableContract> Tables,
    IReadOnlySet<string> MaybeMissingAliases)
{
    public static BoundTableSymbolContract Capture(TableSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var tables = symbol.CompoundTables
            .Select(alias =>
            {
                if (!symbol.TryGetColumns(alias, out var columns))
                    throw new InvalidOperationException($"Table symbol alias '{alias}' has no column contract.");

                var table = symbol.GetTableByAlias(alias).Table ??
                            throw new InvalidOperationException($"Table symbol alias '{alias}' has no table contract.");

                return new BoundTableContract(
                    alias,
                    table.Metadata?.TableEntityType,
                    Array.AsReadOnly(columns.Select(BoundSchemaColumn.Capture).ToArray()));
            })
            .ToArray();

        var maybeMissingAliases = symbol.CompoundTables
            .Where(symbol.CanAliasBeMissing)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new BoundTableSymbolContract(
            symbol.HasAlias,
            Array.AsReadOnly(tables),
            new ReadOnlySet<string>(maybeMissingAliases));
    }

    public TableSymbol Restore()
    {
        TableSymbol? restored = null;

        foreach (var tableContract in Tables)
        {
            var table = new DynamicTable(tableContract.Columns.Cast<ISchemaColumn>().ToArray(), tableContract.EntityType);
            var schema = new TransitionSchema(tableContract.Alias, table);
            var current = new TableSymbol(tableContract.Alias, schema, table, HasAlias);
            restored = restored == null ? current : restored.MergeSymbols(current);
        }

        if (restored == null)
            throw new InvalidOperationException("A table symbol must contain at least one table contract.");

        return MaybeMissingAliases.Count == 0
            ? restored
            : restored.MarkAliasesAsMaybeMissing(MaybeMissingAliases);
    }
}

internal sealed record BoundTableContract(
    string Alias,
    Type? EntityType,
    IReadOnlyList<BoundSchemaColumn> Columns);

internal sealed class ReadOnlySet<T>(IEnumerable<T> values) : IReadOnlySet<T>
    where T : notnull
{
    private readonly HashSet<T> _values = new(values);

    public int Count => _values.Count;

    public bool Contains(T item) => _values.Contains(item);

    public bool IsProperSubsetOf(IEnumerable<T> other) => _values.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => _values.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => _values.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => _values.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other) => _values.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => _values.SetEquals(other);

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
