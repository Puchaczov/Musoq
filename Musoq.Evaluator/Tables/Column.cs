using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FQL.Evaluator.Tables
{
    [DebuggerDisplay("{ColumnOrder}. {Name}: {ColumnType.Name}")]
    public class Column : IEquatable<Column>
    {
        public Column(string name, Type columnType, int columnOrder)
        {
            Name = name;
            ColumnType = columnType;
            ColumnOrder = columnOrder;
        }

        public string Name { get; }
        public Type ColumnType { get; }
        public int ColumnOrder { get; }

        public bool Equals(Column other)
        {
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<Type>.Default.Equals(ColumnType, other.ColumnType) &&
                   ColumnOrder == other.ColumnOrder;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }

        public override int GetHashCode()
        {
            var hashCode = -1716540554;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(ColumnType);
            hashCode = hashCode * -1521134295 + ColumnOrder.GetHashCode();
            return hashCode;
        }
    }
}