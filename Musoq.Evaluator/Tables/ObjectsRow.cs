using System;

namespace Musoq.Evaluator.Tables;

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

        // Optimized: Avoid LINQ allocations by using Array.Copy directly
        if (leftContexts == null)
        {
            var result = new object[1 + rightContexts.Length];
            result[0] = null;
            Array.Copy(rightContexts, 0, result, 1, rightContexts.Length);
            Contexts = result;
        }
        else if (rightContexts == null)
        {
            var result = new object[leftContexts.Length + 1];
            Array.Copy(leftContexts, 0, result, 0, leftContexts.Length);
            result[leftContexts.Length] = null;
            Contexts = result;
        }
        else
        {
            var result = new object[leftContexts.Length + rightContexts.Length];
            Array.Copy(leftContexts, 0, result, 0, leftContexts.Length);
            Array.Copy(rightContexts, 0, result, leftContexts.Length, rightContexts.Length);
            Contexts = result;
        }

        _values = values;
    }

    public override object this[int columnNumber] => _values[columnNumber];

    public override int Count => _values.Length;

    public override object[] Values => _values;

    public override object[] Contexts { get; }
}
