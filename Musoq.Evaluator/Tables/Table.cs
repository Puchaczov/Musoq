using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tables
{
    public class Table : IndexedList<Key, Row>, IEnumerable<Row>
    {
        private readonly Dictionary<int, Column> _columnsByIndex;
        private readonly Dictionary<string, Column> _columnsByName;
        private readonly Dictionary<int, TableIndex[]> _indexes;

        public Table(string name, Column[] columns)
        {
            Name = name;

            _indexes = new Dictionary<int, TableIndex[]>();
            _columnsByIndex = new Dictionary<int, Column>();
            _columnsByName = new Dictionary<string, Column>();

            AddColumns(columns);
        }

        public string Name { get; }

        public IEnumerable<Column> Columns => _columnsByIndex.Values;

        public IEnumerator<Row> CurrentEnumerator { get; private set; }

        public IEnumerator<Row> GetEnumerator()
        {
            CurrentEnumerator = Rows.GetEnumerator();
            return CurrentEnumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddColumns(params Column[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                _columnsByIndex.Add(columns[i].ColumnOrder, columns[i]);
                _columnsByName.Add(columns[i].Name, columns[i]);
            }
        }

        public void AddIndex(params TableIndex[] indexes)
        {
            var hash = 0;
            foreach (var item in indexes)
                hash += item.GetHashCode();

            _indexes.Add(hash, indexes);
        }

        public bool HasIndex(params TableIndex[] indexes)
        {
            var hash = 0;
            foreach (var item in indexes)
                hash += item.GetHashCode();

            return _indexes.ContainsKey(hash);
        }

        public Column GetColumn(string name)
        {
            return _columnsByName[name];
        }

        public void Add(Row value)
        {
            var newIndex = Rows.Count;

            if (value.Count != _columnsByIndex.Count)
                throw new NotSupportedException(
                    $"({nameof(Add)}) Current row has {value.Count} values but {_columnsByIndex.Count} required.");

            for (var i = 0; i < value.Count; i++)
            {
                if (value[i] == null)
                    continue;

                var t1 = value[i].GetType();
                var t2 = _columnsByIndex[i].ColumnType;
                if (!t2.IsAssignableFrom(t1))
                    throw new NotSupportedException(
                        $"({nameof(Add)}) Mismatched types. {t2.Name} is not assignable from {t1.Name}");
            }

            Rows.Add(value);

            foreach (var index in _indexes)
            {
                var array = index.Value.Select((f, i) => _columnsByName[f.ColumnName]);
                var enumerable = array as Column[] ?? array.ToArray();
                var indexes = enumerable.Select(f => f.ColumnOrder).ToArray();

                var objects = new object[indexes.Length];

                for (var i = 0; i < indexes.Length; i++) objects[i] = value[_columnsByIndex[indexes[i]].ColumnOrder];

                var key = new Key(objects, indexes);

                if (!HasMatchKey(key, value))
                    continue;

                if (!Indexes.ContainsKey(key))
                    Indexes.Add(key, new List<int>());

                Indexes[key].Add(newIndex);
            }
        }
    }
}