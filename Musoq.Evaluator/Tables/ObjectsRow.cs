using System;
using System.Linq;

namespace Musoq.Evaluator.Tables
{
    public class ObjectsRow : Row
    {
        private readonly object[] _values;

        public ObjectsRow(object[] values)
        {
            _values = values;
        }

        public ObjectsRow(object[] values, object[] contexts)
        {
            _values = values;
            Contexts = contexts;
        }

        public ObjectsRow(object[] values, object[] leftContexts, object[] rightContexts)
        {
            if (leftContexts == null && rightContexts == null)
                throw new NotSupportedException("Both contexts cannot be null");

            if (leftContexts == null)
            {
                Contexts = new object[] { null }.Concat(rightContexts).ToArray();
            }
            else if (rightContexts == null)
            {
                Contexts = leftContexts.Concat(new object[] { null }).ToArray();
            }
            else
            {
                Contexts = leftContexts.Concat(rightContexts).ToArray();
            }

            _values = values;
        }

        public override object this[int columnNumber] => _values[columnNumber];

        public override int Count => _values.Length;

        public override object[] Values => _values;

        public object[] Contexts { get; }
    }
}