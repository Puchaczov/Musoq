using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables
{
    [DebuggerDisplay("{ToString()}")]
    public class GroupKey
    {
        public readonly object[] Values;

        public GroupKey(params object[] values)
        {
            Values = values;
        }

        public bool Equals(GroupKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var equals = true;

            for (var i = 0; i < Values.Length && equals; i++) equals &= Values[i].Equals(other.Values[i]);

            return equals;
        }

        public override string ToString()
        {
            var key = new StringBuilder();

            string value;
            for (var i = 0; i < Values.Length - 1; i++)
            {
                value = Values[i] == null ? "null" : Values[i].ToString(); 
                key.Append($"{value},");
            }

            value = Values[Values.Length - 1] == null ? "null" : Values[Values.Length - 1].ToString();
            key.Append(value);

            return key.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(Values, ((GroupKey)obj).Values);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;
                for (var i = 0; i < Values.Length; ++i)
                {
                    hash += Values[i].GetHashCode();
                }

                return hash;
            }
        }

        private static bool Equals<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
        {
            if (first.Count != second.Count)
                return false;

            var areEqual = true;

            for (var i = 0; i < first.Count; i++) areEqual &= first[i].Equals(second[i]);

            return areEqual;
        }
    }
}
