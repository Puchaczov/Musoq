using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class TableSymbol : Symbol
    {
        private readonly List<string> _orders = new();

        private readonly Dictionary<string, (ISchema Schema, ISchemaTable SchemaTable)> _tables = new();

        private string _fullTableName;

        private ISchemaTable _fullTable;
        private ISchema _fullSchema;

        public TableSymbol(string alias, ISchema schema, ISchemaTable table, bool hasAlias)
        {
            _tables.Add(alias, (schema, table));
            _orders.Add(alias);
            HasAlias = hasAlias;
            _fullTableName = alias;

            _fullSchema = schema;
            _fullTable = table;
        }

        private TableSymbol()
        {
            HasAlias = true;
        }

        public bool HasAlias { get; }

        public string[] CompoundTables => _orders.ToArray();

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByColumnName(string column)
        {
            (ISchema Schema, ISchemaTable Table, string Alias) score = (null, null, null);

            foreach (var table in _tables)
            {
                var col = table.Value.SchemaTable.GetColumnsByName(column);

                if (col == null)
                    throw new NotSupportedException();
                
                if (col.Length == 0)
                    continue;
                
                if (col.Length > 1)
                    throw new AmbiguousColumnException(column, _orders[0], _orders[1]);
                
                if (score is not (null, null, null))
                    if (score.Schema != table.Value.Schema || score.Table != table.Value.SchemaTable)
                        throw new AmbiguousColumnException(column, score.Alias, table.Key);

                score = (table.Value.Schema, table.Value.SchemaTable, table.Key);
            }

            return score;
        }

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByAlias(string alias)
        {
            if (_fullTableName == alias)
                return (_fullSchema, _fullTable, alias);
            return (_tables[alias].Item1, _tables[alias].Item2, alias);
        }

        public ISchemaColumn GetColumnByAliasAndName(string alias, string columnName)
        {
            var columns = _fullTableName == alias ? _fullTable.GetColumnsByName(columnName) : _tables[alias].Item2.GetColumnsByName(columnName);
            
            if (columns.Length > 1)
                throw new AmbiguousColumnException(columnName, _orders[0], _orders[1]);
            
            return columns.SingleOrDefault();
        }

        public ISchemaColumn[] GetColumns(string alias)
        {
            return _tables[alias].Item2.Columns;
        }

        public ISchemaColumn[] GetColumns()
        {
            var columns = new List<ISchemaColumn>();
            foreach (var table in _orders) columns.AddRange(GetColumns(table));

            return columns.ToArray();
        }

        public TableSymbol MergeSymbols(TableSymbol other)
        {
            var symbol = new TableSymbol();

            var compoundTableColumns = new List<ISchemaColumn>();

            foreach (var item in _tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);

                compoundTableColumns.AddRange(item.Value.Item2.Columns);
            }

            symbol._tables.Add(other._fullTableName, (other._fullSchema, other._fullTable));
            symbol._orders.Add(other._fullTableName);

            compoundTableColumns.AddRange(other._fullTable.Columns);

            symbol._fullTableName = symbol._orders.Aggregate((a, b) => a + b);
            symbol._fullTable = new DynamicTable(compoundTableColumns.ToArray());
            symbol._fullSchema = new TransitionSchema(symbol._fullTableName, symbol._fullTable);

            return symbol;
        }

        public TableSymbol MakeNullableIfPossible()
        {
            var symbol = new TableSymbol();
            var compoundTableColumns = new List<ISchemaColumn>();
            
            foreach (var column in _fullTable.Columns)
            {
                compoundTableColumns.Add(ConvertColumnToNullable(column));
            }

            foreach (var item in _tables)
            {
                var dynamicTable = new DynamicTable(item.Value.Item2.Columns.Select(ConvertColumnToNullable).ToArray());
                symbol._tables.Add(item.Key, (item.Value.Item1, dynamicTable));
                symbol._orders.Add(item.Key);
            }

            symbol._fullTableName = symbol._orders.Aggregate((a, b) => a + b);
            symbol._fullTable = new DynamicTable(compoundTableColumns.ToArray());
            symbol._fullSchema = new TransitionSchema(symbol._fullTableName, symbol._fullTable);

            return symbol;
        }

        private ISchemaColumn ConvertColumnToNullable(ISchemaColumn column)
        {
            return new SchemaColumn(column.ColumnName, column.ColumnIndex, ConvertToNullable(column.ColumnType));
        }

        private Type ConvertToNullable(Type columnType)
        {
            if (Nullable.GetUnderlyingType(columnType) == null && columnType.IsValueType)
                return typeof(Nullable<>).MakeGenericType(columnType);

            return columnType;
        }

        public TableSymbol LimitColumnsTo(IReadOnlyDictionary<string, string[]> columnLimits)
        {
            var symbol = new TableSymbol();

            var compoundTableColumns = new List<ISchemaColumn>();

            foreach (var item in _tables)
            {
                var dynamicTable = new DynamicTable(item.Value.Item2.Columns.Where(c => columnLimits.ContainsKey(item.Key) && columnLimits[item.Key].Contains(c.ColumnName)).ToArray());
                symbol._tables.Add(item.Key, (item.Value.Item1, dynamicTable));
                symbol._orders.Add(item.Key);

                compoundTableColumns.AddRange(dynamicTable.Columns);
            }

            symbol._fullTableName = symbol._orders.Aggregate((a, b) => a + b);
            symbol._fullTable = new DynamicTable(compoundTableColumns.ToArray());
            symbol._fullSchema = new TransitionSchema(symbol._fullTableName, symbol._fullTable);

            return symbol;
        }
    }
}