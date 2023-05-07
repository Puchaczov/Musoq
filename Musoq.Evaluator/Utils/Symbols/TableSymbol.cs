using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class TableSymbol : Symbol
    {
        private readonly List<string> _orders = new();

        private readonly Dictionary<string, Tuple<ISchema, ISchemaTable>> _tables = new();

        private string _fullTableName;

        private ISchemaTable _fullTable;
        private ISchema _fullSchema;

        public TableSymbol(string alias, ISchema schema, ISchemaTable table, bool hasAlias)
        {
            _tables.Add(alias, new Tuple<ISchema, ISchemaTable>(schema, table));
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
        
        public bool IsCompound => _orders.Count > 1;

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByColumnName(string column)
        {
            (ISchema, ISchemaTable, string) score = (null, null, null);

            foreach (var table in _tables)
            {
                var col = table.Value.Item2.GetColumnByName(column);

                if (col == null)
                    throw new NotSupportedException($"Unrecognized column ({column})");

                score = (table.Value.Item1, table.Value.Item2, table.Key);
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
            if (_fullTableName == alias)
                return _fullTable.GetColumnByName(columnName);

            return _tables[alias].Item2.GetColumnByName(columnName);
        }

        public ISchemaColumn GetColumn(string columnName)
        {
            ISchemaColumn column = null;
            foreach (var table in _orders)
            {
                var tmpColumn = _tables[table].Item2.GetColumnByName(columnName);

                if (column != null)
                    throw new NotSupportedException("Multiple column with the same identifier");

                if (tmpColumn == null)
                    continue;

                column = tmpColumn;
            }

            if (column == null)
                throw new NotSupportedException("No such column.");

            return column;
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

        public int GetColumnIndex(string alias, string columnName)
        {
            var i = 0;
            var count = 0;
            while (_orders[i] != alias)
            {
                count += _tables[_orders[i]].Item2.Columns.Length;
                i++;
            }

            var columns = _tables[_orders[i]].Item2.Columns;
            var j = 0;
            for (; j < columns.Length; j++)
                if (columns[j].ColumnName == columnName)
                    break;

            return count + j + 1;
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

            symbol._tables.Add(other._fullTableName, new Tuple<ISchema, ISchemaTable>(other._fullSchema, other._fullTable));
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
                var dynamicTable = new DynamicTable(item.Value.Item2.Columns.Select(c => ConvertColumnToNullable(c)).ToArray());
                symbol._tables.Add(item.Key, new Tuple<ISchema, ISchemaTable>(item.Value.Item1, dynamicTable));
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
    }
}