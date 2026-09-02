using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Utils.Symbols;

public class TableSymbol : Symbol
{
    private readonly List<string> _orders = [];

    private readonly Dictionary<string, (ISchema Schema, ISchemaTable SchemaTable)> _tables = new();
    private readonly HashSet<string> _maybeMissingAliases = new(StringComparer.OrdinalIgnoreCase);
    private ISchema? _fullSchema;

    private ISchemaTable? _fullTable;

    private string? _fullTableName;

    public TableSymbol(string alias, ISchema schema, ISchemaTable table, bool hasAlias)
    {
        _tables.Add(alias, (schema, table));
        _orders.Add(alias);
        HasAlias = hasAlias;
        SetFullBinding(alias, table);
    }

    private TableSymbol(bool hasAlias = true)
    {
        HasAlias = hasAlias;
    }

    public ISchemaTable FullTable => _fullTable ?? throw new InvalidOperationException("Table symbol is incomplete.");

    private ISchema FullSchema => _fullSchema ?? throw new InvalidOperationException("Table symbol is incomplete.");

    private string FullTableName => _fullTableName ?? throw new InvalidOperationException("Table symbol is incomplete.");

    public bool HasAlias { get; }

    public string[] CompoundTables => _orders.ToArray();

    public bool IsCompoundTable => _tables.Count > 1;

    public (ISchema? Schema, ISchemaTable? Table, string? TableName) GetTableByColumnName(
        string column,
        TextSpan? span = null)
    {
        (ISchema? Schema, ISchemaTable? Table, string? Alias) score = (null, null, null);

        foreach (var table in _tables)
        {
            var col = table.Value.SchemaTable.GetColumnsByName(column);

            if (col == null)
                throw new NotSupportedException();

            if (col.Length == 0)
                continue;

            if (col.Length > 1)
                throw CreateAmbiguousColumnException(column, _orders[0], _orders[1], span);

            if (score is not (null, null, null))
                if (score.Schema != table.Value.Schema || score.Table != table.Value.SchemaTable)
                    throw CreateAmbiguousColumnException(column, score.Alias ?? string.Empty, table.Key, span);

            score = (table.Value.Schema, table.Value.SchemaTable, table.Key);
        }

        return score;
    }

    public bool ContainsAlias(string alias)
    {
        return _fullTableName == alias || _tables.ContainsKey(alias);
    }

    public bool CanAliasBeMissing(string alias)
    {
        return _maybeMissingAliases.Contains(alias);
    }

    public TableSymbol MarkAliasesAsMaybeMissing(IEnumerable<string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);

        var symbol = WithFullTableName(FullTableName);
        foreach (var alias in aliases)
        {
            if (symbol.ContainsAlias(alias))
                symbol._maybeMissingAliases.Add(alias);
        }

        return symbol;
    }

    public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByAlias(string alias)
    {
        if (FullTableName == alias)
            return (FullSchema, FullTable, alias);
        return (_tables[alias].Item1, _tables[alias].Item2, alias);
    }

    public ISchemaColumn? GetColumnByAliasAndName(string alias, string columnName, TextSpan? span = null)
    {
        var columns = _fullTableName == alias
            ? FullTable.GetColumnsByName(columnName)
            : _tables[alias].Item2.GetColumnsByName(columnName);

        if (columns.Length > 1)
            throw CreateAmbiguousColumnException(columnName, _orders[0], _orders[1], span);

        return columns.SingleOrDefault();
    }

    public ISchemaColumn[] GetColumns(string alias)
    {
        return _tables[alias].Item2.Columns;
    }

    public bool AliasContainsColumn(string alias, string columnName)
    {
        if (!_tables.TryGetValue(alias, out var table))
            return false;

        return table.Item2.GetColumnsByName(columnName).Length > 0;
    }

    public bool TryGetColumns(string alias, [NotNullWhen(true)] out ISchemaColumn[]? columns)
    {
        if (_tables.TryGetValue(alias, out var table))
        {
            columns = table.Item2.Columns;
            return true;
        }

        columns = null;
        return false;
    }

    public ISchemaColumn[] GetColumns()
    {
        var columns = new List<ISchemaColumn>();
        foreach (var table in _orders) columns.AddRange(GetColumns(table));

        return columns.ToArray();
    }

    public TableSymbol MergeSymbols(TableSymbol other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var symbol = new TableSymbol();

        var compoundTableColumns = new List<ISchemaColumn>();

        AddTableBindings(this, symbol, compoundTableColumns, _orders);
        AddTableBindings(other, symbol, compoundTableColumns, other._orders);

        symbol.SetFullBinding(
            string.Concat(symbol._orders),
            new DynamicTable(compoundTableColumns.ToArray(),
                caseSensitive: IsCaseSensitiveTable(FullTable) || IsCaseSensitiveTable(other.FullTable)));
        CopyMaybeMissingAliasesTo(symbol);
        other.CopyMaybeMissingAliasesTo(symbol);

        return symbol;
    }

    public TableSymbol WithAdditionalColumn(string alias, ISchemaColumn column)
    {
        ArgumentNullException.ThrowIfNull(alias);
        ArgumentNullException.ThrowIfNull(column);

        if (!_tables.ContainsKey(alias))
            throw new InvalidOperationException($"Table alias '{alias}' was not found.");

        if (AliasContainsColumn(alias, column.ColumnName))
            throw new InvalidOperationException($"Table alias '{alias}' already exposes column '{column.ColumnName}'.");

        var symbol = new TableSymbol(HasAlias);
        var compoundTableColumns = new List<ISchemaColumn>();

        Type? singleEntityType = null;
        foreach (var tableName in _orders)
        {
            var table = _tables[tableName];
            var entityType = table.SchemaTable.Metadata?.TableEntityType;
            if (_tables.Count == 1)
                singleEntityType = entityType;

            ISchemaTable schemaTable = table.SchemaTable;
            if (string.Equals(tableName, alias, StringComparison.OrdinalIgnoreCase))
            {
                schemaTable = new DynamicTable(
                    [..table.SchemaTable.Columns, column],
                    entityType,
                    IsCaseSensitiveTable(table.SchemaTable));
            }

            symbol._tables.Add(tableName, (table.Schema, schemaTable));
            symbol._orders.Add(tableName);
            compoundTableColumns.AddRange(schemaTable.Columns);
        }

        symbol.SetFullBinding(
            string.Concat(symbol._orders),
            new DynamicTable(
                compoundTableColumns.ToArray(),
                singleEntityType,
                IsCaseSensitiveTable(FullTable)));
        CopyMaybeMissingAliasesTo(symbol);

        return symbol;
    }

    public TableSymbol WithFullTableName(string fullTableName)
    {
        var symbol = new TableSymbol(HasAlias);

        foreach (var tableName in _orders)
        {
            symbol._tables.Add(tableName, _tables[tableName]);
            symbol._orders.Add(tableName);
        }

        symbol.SetFullBinding(fullTableName, FullTable);
        CopyMaybeMissingAliasesTo(symbol);

        return symbol;
    }

    public TableSymbol MakeNullableIfPossible()
    {
        var symbol = new TableSymbol(HasAlias);
        var compoundTableColumns = new List<ISchemaColumn>();

        foreach (var column in FullTable.Columns) compoundTableColumns.Add(ConvertColumnToNullable(column));

        Type? singleEntityType = null;
        foreach (var item in _tables)
        {
            var entityType = item.Value.SchemaTable.Metadata?.TableEntityType;
            if (_tables.Count == 1)
                singleEntityType = entityType;
            var dynamicTable = new DynamicTable(
                item.Value.Item2.Columns.Select(ConvertColumnToNullable).ToArray(),
                entityType,
                IsCaseSensitiveTable(item.Value.Item2));
            symbol._tables.Add(item.Key, (item.Value.Item1, dynamicTable));
            symbol._orders.Add(item.Key);
        }

        symbol.SetFullBinding(
            string.Concat(symbol._orders),
            new DynamicTable(
                compoundTableColumns.ToArray(),
                singleEntityType,
                IsCaseSensitiveTable(FullTable)));
        CopyMaybeMissingAliasesTo(symbol);

        return symbol;
    }

    private ISchemaColumn ConvertColumnToNullable(ISchemaColumn column)
    {
        return new SchemaColumn(
            column.ColumnName,
            column.ColumnIndex,
            ConvertToNullable(column.ColumnType),
            column.IntendedTypeName,
            column.ReadModifiers,
            column.Stability);
    }

    private static Type ConvertToNullable(Type columnType)
    {
        if (Nullable.GetUnderlyingType(columnType) == null && columnType.IsValueType)
            return typeof(Nullable<>).MakeGenericType(columnType);

        return columnType;
    }

    public TableSymbol LimitColumnsTo(IReadOnlyDictionary<string, string[]> columnLimits)
    {
        var symbol = new TableSymbol(HasAlias);

        var compoundTableColumns = new List<ISchemaColumn>();

        Type? singleEntityType = null;
        foreach (var item in _tables)
        {
            var entityType = item.Value.SchemaTable.Metadata?.TableEntityType;
            if (_tables.Count == 1)
                singleEntityType = entityType;
            var columns = columnLimits.TryGetValue(item.Key, out var allowedColumns)
                ? item.Value.Item2.Columns.Where(c => allowedColumns.Contains(c.ColumnName)).ToArray()
                : [];
            var dynamicTable = new DynamicTable(
                columns,
                entityType,
                IsCaseSensitiveTable(item.Value.Item2));
            symbol._tables.Add(item.Key, (item.Value.Item1, dynamicTable));
            symbol._orders.Add(item.Key);

            compoundTableColumns.AddRange(dynamicTable.Columns);
        }

        symbol.SetFullBinding(
            string.Concat(symbol._orders),
            new DynamicTable(
                compoundTableColumns.ToArray(),
                singleEntityType,
                IsCaseSensitiveTable(FullTable)));
        CopyMaybeMissingAliasesTo(symbol);

        return symbol;
    }

    private void CopyMaybeMissingAliasesTo(TableSymbol symbol)
    {
        foreach (var alias in _maybeMissingAliases)
        {
            if (symbol.ContainsAlias(alias))
                symbol._maybeMissingAliases.Add(alias);
        }
    }

    private static bool IsCaseSensitiveTable(ISchemaTable table) =>
        table is DynamicTable { IsCaseSensitive: true };

    private void SetFullBinding(string fullTableName, ISchemaTable fullTable)
    {
        _fullTableName = fullTableName;
        _fullTable = fullTable;
        _fullSchema = _tables.Count == 1 &&
                      _orders.Count == 1 &&
                      string.Equals(_orders[0], fullTableName, StringComparison.Ordinal)
            ? _tables[_orders[0]].Schema
            : new TransitionSchema(fullTableName, fullTable);
    }

    private static void AddTableBindings(
        TableSymbol source,
        TableSymbol destination,
        ICollection<ISchemaColumn> compoundTableColumns,
        IReadOnlyList<string> aliases)
    {
        for (var index = 0; index < aliases.Count; index++)
        {
            var alias = aliases[index];
            var binding = source._tables[alias];
            destination._tables.Add(alias, binding);
            destination._orders.Add(alias);
            foreach (var column in binding.SchemaTable.Columns)
                compoundTableColumns.Add(column);
        }
    }

    private static AmbiguousColumnException CreateAmbiguousColumnException(
        string column,
        string alias1,
        string alias2,
        TextSpan? span)
    {
        return span is { } value && !value.IsEmpty
            ? new AmbiguousColumnException(column, alias1, alias2, value)
            : new AmbiguousColumnException(column, alias1, alias2);
    }
}
