using System;
using System.Diagnostics;
using System.Text;

namespace FQL.Evaluator.Tables
{
    [DebuggerDisplay("{DebugInfo()}")]
    public abstract class Row : IEquatable<Row>, IValue<Key>
    {
        public abstract object this[int columnNumber] { get; }

        public abstract int Count { get; }

        public abstract object[] Values { get; }

        public bool Equals(Row other)
        {
            if (other.Count != Count)
                return false;

            var isEqual = true;

            for (var i = 0; i < Count && isEqual; ++i)
                isEqual &= this[i].Equals(other[i]);

            return isEqual;
        }

        public bool FitsTheIndex(Key key)
        {
            return key.DoesRowMatchKey(this);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Row);
        }

        public override int GetHashCode()
        {
            var hashCode = -1000162029;

            for (int i = 0, j = Count; i < j; ++i)
                hashCode = hashCode * -1521134295 + this[i].GetHashCode();

            return hashCode;
        }

        public bool CheckWithKey(Key key)
        {
            var isMatch = true;

            for (var i = 0; i < key.Columns.Length; i++)
                isMatch &= this[key.Columns[i]].Equals(key.Values[i]);

            return isMatch;
        }

        internal string DebugInfo()
        {
            var rowText = new StringBuilder();

            for (var i = 0; i < Count - 1; i++)
            {
                rowText.Append(this[i]);
                rowText.Append(", ");
            }

            rowText.Append(this[Count - 1]);

            return rowText.ToString();
        }
    }
}