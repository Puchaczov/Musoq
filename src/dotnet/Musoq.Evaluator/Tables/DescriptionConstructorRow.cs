using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionConstructorRow(RowLayout layout, string[] values) : Row
{
    private readonly RowLayout _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    private readonly string[] _values = values ?? throw new ArgumentNullException(nameof(values));
    private object[]? _boxedValues;

    public override int Count => _values.Length;

    public override object this[int columnNumber]
    {
        get
        {
            if ((uint)columnNumber >= (uint)_values.Length)
                throw new ArgumentOutOfRangeException(nameof(columnNumber), columnNumber, "Column index is outside row bounds.");

            return _values[columnNumber];
        }
    }

    public override object this[string name] => this[_layout.GetIndex(name)];

    public override bool HasColumn(string name) => _layout.HasColumn(name);

    public override object[] Values
    {
        get
        {
            if (_boxedValues != null)
                return _boxedValues;

            var values = new object[_values.Length];
            Array.Copy(_values, values, _values.Length);
            _boxedValues = values;
            return values;
        }
    }
}
