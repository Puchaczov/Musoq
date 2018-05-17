using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class TableSymbol : Symbol
    {
        private readonly Dictionary<string, Tuple<ISchema, ISchemaTable>> _tables = new Dictionary<string, Tuple<ISchema, ISchemaTable>>();
        private readonly List<string> _orders = new List<string>();

        public TableSymbol(string alias, ISchema schema, ISchemaTable table, bool hasAlias)
        {
            _tables.Add(alias, new Tuple<ISchema, ISchemaTable>(schema, table));
            _orders.Add(alias);
            HasAlias = hasAlias;
        }

        private TableSymbol()
        {
            HasAlias = true;
        }

        public bool HasAlias { get; }

        public string[] CompoundTables => _orders.ToArray();

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByColumnName(string column)
        {
            (ISchema, ISchemaTable, string) score = (null, null, null);

            foreach (var table in _tables)
            {
                var col = table.Value.Item2.Columns.SingleOrDefault(c => c.ColumnName == column);

                if(col == null)
                    throw new NotSupportedException();

                score = (table.Value.Item1, table.Value.Item2, table.Key);
            }

            return score;
        }

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByAlias(string alias)
        {
            return (_tables[alias].Item1, _tables[alias].Item2, alias);
        }

        public ISchemaColumn GetColumnByAliasAndName(string alias, string columnName)
        {
            return _tables[alias].Item2.Columns.Single(c => c.ColumnName == columnName);
        }

        public ISchemaColumn GetColumn(string columnName)
        {
            ISchemaColumn column = null;
            foreach (var table in _orders)
            {
                var tmpColumn = _tables[table].Item2.Columns.SingleOrDefault(col => col.ColumnName == columnName);

                if(column != null)
                    throw new NotSupportedException("Multiple column with the same identifier");

                if(tmpColumn == null)
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
            foreach (var table in _orders)
            {
                columns.AddRange(GetColumns(table));
            }

            return columns.ToArray();
        }

        public int GetColumnIndex(string alias, string columnName)
        {
            int i = 0;
            int count = 0;
            while (_orders[i] != alias)
            {
                count += _tables[_orders[i]].Item2.Columns.Length;
                i++;
            }

            var columns = _tables[_orders[i]].Item2.Columns;
            int j = 0;
            for (; j < columns.Length; j++)
            {
                if(columns[j].ColumnName == columnName)
                    break;
            }

            return (count + j + 1);
        }

        public TableSymbol MergeSymbols(TableSymbol other)
        {
            var symbol = new TableSymbol();

            foreach (var item in _tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);
            }

            foreach (var item in other._tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);
            }

            return symbol;
        }

        public TableSymbol JoinSymbols(TableSymbol other)
        {
            var symbol = new TableSymbol();
            var name = _tables.Keys.Aggregate((a, b) => a + b);

            var tables = _tables.Values.Select(f => f.Item2);
            var table = new DynamicTable(tables.Select(f => f.Columns).Aggregate((a, b) => a.Concat(b).ToArray()));

            symbol._tables.Add(name, new Tuple<ISchema, ISchemaTable>(new TransitionSchema(name, table), table));
            symbol._tables.Add(other._orders[0], new Tuple<ISchema, ISchemaTable>(other._tables[other._orders[0]].Item1, other._tables[other._orders[0]].Item2));

            symbol._orders.Add(name);
            symbol._orders.Add(other._orders[0]);

            return symbol;
        }
    }

    public class ColumnSymbol : Symbol
    {
        public ColumnSymbol(ISchemaColumn column)
        {
            Column = column;
        }

        public ISchemaColumn Column { get; }
    }

    public class TypeSymbol : Symbol
    {
        public TypeSymbol(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }

    public class FieldsNamesSymbol : Symbol
    {
        public FieldsNamesSymbol(string[] names)
        {
            Names = names;
        }

        public string[] Names { get; }
    }


    public class RefreshMethodsSymbol : Symbol
    {
        public RefreshMethodsSymbol(IEnumerable<AccessMethodNode> refreshMethods)
        {
            RefreshMethods = refreshMethods.ToArray();
        }

        public IReadOnlyList<AccessMethodNode> RefreshMethods { get; }
    }
}
