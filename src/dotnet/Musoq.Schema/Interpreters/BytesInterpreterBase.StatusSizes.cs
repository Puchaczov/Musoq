using System.Numerics;
using System.Runtime.CompilerServices;

namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    /// <summary>Validates a bound numeric field without allocating a read delegate or throwing on invalid sizes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ParseAttempt<int> ResolveSizeValueStatus<T>(
        T? value,
        ParseErrorCode errorCode = ParseErrorCode.InvalidSize
    )
        where T : struct, INumberBase<T>
    {
        if ((typeof(T) == typeof(byte) || typeof(T) == typeof(ushort)) && value.HasValue)
            return ParseAttempt<int>.Success(int.CreateTruncating(value.GetValueOrDefault()));
        return ResolveCheckedSizeValueStatus(value, errorCode);
    }

    private ParseAttempt<int> ResolveCheckedSizeValueStatus<T>(T? value, ParseErrorCode errorCode)
        where T : struct, INumberBase<T>
    {
        if (!value.HasValue || !T.IsFinite(value.Value) || !T.IsInteger(value.Value) || T.IsNegative(value.Value))
            return ParseAttempt<int>.Failed(default, InvalidExpressionSizeFailure(errorCode));
        // Every representable integer in the supported int range is exact in double.
        // This rejects saturation and out-of-range values before CreateChecked can throw.
        if (double.CreateSaturating(value.Value) > int.MaxValue)
            return ParseAttempt<int>.Failed(default, InvalidExpressionSizeFailure(errorCode));
        return ParseAttempt<int>.Success(int.CreateChecked(value.Value));
    }

    /// <summary>Evaluates a checked numeric expression once and converts its expected size failures into data.</summary>
    protected ParseAttempt<int> ResolveSizeExpressionStatus<T>(
        Func<T?> readValue,
        ParseErrorCode errorCode = ParseErrorCode.InvalidSize
    )
        where T : struct, INumberBase<T>
    {
        try
        {
            return ResolveSizeValueStatus(readValue(), errorCode);
        }
        catch (OverflowException)
        {
            return ParseAttempt<int>.Failed(default, InvalidExpressionSizeFailure(errorCode));
        }
    }

    private ParseFailure InvalidExpressionSizeFailure(ParseErrorCode code) =>
        ParseFailure.ForBytes(
            code,
            SchemaName,
            _currentFieldName,
            ParsePosition,
            code == ParseErrorCode.InvalidPosition
                ? "Position must be a non-negative representable integer."
                : "Size must be a non-negative representable integer."
        );

    /// <summary>Checks a generated binary repeat limit without throwing an ordinary input failure.</summary>
    protected ParseFailure? CheckRepeatIterationStatus(string field, int iteration, int maximum) =>
        iteration >= maximum
            ? ParseFailure.ForBytes(
                ParseErrorCode.MaxIterationsExceeded,
                SchemaName,
                field,
                ParsePosition,
                $"Repeat field '{field}' exceeded the maximum of {maximum} iterations."
            )
            : null;

    /// <summary>Checks forward progress of a binary EOF repeat.</summary>
    protected ParseFailure? CheckRepeatProgressStatus(string field, int position, int bitOffset) =>
        ParsePosition < position || ParsePosition == position && BitOffset <= bitOffset
            ? ParseFailure.ForBytes(
                ParseErrorCode.MaxIterationsExceeded,
                SchemaName,
                field,
                ParsePosition,
                $"Repeat-until-eof field '{field}' made no progress reading an element; its byte-and-bit cursor did not advance."
            )
            : null;

    /// <summary>Checks exact substream consumption without throwing an ordinary input failure.</summary>
    protected ParseFailure? CheckSubstreamConsumptionStatus<T>(
        string field,
        int length,
        IBytesInterpreter<T> interpreter
    )
    {
        var bitOffset = interpreter is BytesInterpreterBase<T> nested ? nested.BitOffset : 0;
        return interpreter.BytesConsumed != length || bitOffset != 0
            ? ParseFailure.ForBytes(
                ParseErrorCode.ValidationFailed,
                SchemaName,
                field,
                ParsePosition,
                $"Substream '{field}' declared {length} bytes but nested parser stopped at byte {interpreter.BytesConsumed}, bit {bitOffset}."
            )
            : null;
    }
}
