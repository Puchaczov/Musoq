using System.Numerics;

namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    /// <summary>Evaluates a checked numeric size or position without truncating or substituting null.</summary>
    protected int ResolveExpressionSize<T>(Func<T?> readValue, ParseErrorCode errorCode = ParseErrorCode.InvalidSize) where T : struct, INumberBase<T>
    {
        try
        {
            var value = readValue();
            if (value.HasValue && T.IsFinite(value.Value) && T.IsInteger(value.Value) && !T.IsNegative(value.Value))
                return int.CreateChecked(value.Value);
        }
        catch (OverflowException)
        {
            throw InvalidExpressionSize(errorCode);
        }
        throw InvalidExpressionSize(errorCode);
    }

    private ParseException InvalidExpressionSize(ParseErrorCode errorCode) => ParseException.ForBytes(
        errorCode,
        SchemaName, _currentFieldName, ParsePosition,
        errorCode == ParseErrorCode.InvalidPosition ? "Position must be a non-negative representable integer." : "Size must be a non-negative representable integer.");
}
