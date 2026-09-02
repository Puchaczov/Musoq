namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for binary data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class BytesInterpreterBase<TOut> : IBytesInterpreter<TOut>
{
    /// <summary>
    ///     Current parse position in the byte array during interpretation.
    /// </summary>
    protected int ParsePosition { get; set; }

    /// <summary>
    ///     Current bit offset within the current byte (0-7).
    ///     Used for bit field parsing.
    /// </summary>
    protected int BitOffset { get; set; }

    /// <inheritdoc />
    public abstract string SchemaName { get; }

    /// <inheritdoc />
    public int BytesConsumed => ParsePosition;

    /// <inheritdoc />
    public TOut Interpret(ReadOnlySpan<byte> data)
    {
        return InterpretAt(data, 0);
    }

    /// <inheritdoc />
    public TOut Interpret(byte[] data)
    {
        return Interpret(data.AsSpan());
    }

    /// <inheritdoc />
    public abstract TOut InterpretAt(ReadOnlySpan<byte> data, int offset);

    /// <inheritdoc />
    public bool TryInterpret(ReadOnlySpan<byte> data, out TOut? result)
    {
        try
        {
            result = Interpret(data);
            return true;
        }
        catch (ParseException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    ///     Interprets binary data with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="data">The binary data to interpret.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information with partial fields.</returns>
    public PartialInterpretResult<TOut> PartialInterpret(byte[] data)
    {
        return PartialInterpret(data.AsSpan());
    }

    /// <summary>
    ///     Reads a single byte and advances the parse position.
    /// </summary>
    protected byte ReadByte(ReadOnlySpan<byte> data)
    {
        if (ParsePosition >= data.Length)
            ThrowInsufficientData(1, data.Length);
        return data[ParsePosition++];
    }

    /// <summary>
    ///     Reads a signed byte and advances the parse position.
    /// </summary>
    protected sbyte ReadSByte(ReadOnlySpan<byte> data)
    {
        if (ParsePosition >= data.Length)
            ThrowInsufficientData(1, data.Length);
        return (sbyte)data[ParsePosition++];
    }

    private void ThrowInsufficientData(int count, int dataLength)
    {
        throw new ParseException(
            ParseErrorCode.InsufficientData,
            SchemaName,
            _currentFieldName,
            ParsePosition,
            $"Attempted to read {count} bytes at parse position {ParsePosition}, but only {Math.Max(0, dataLength - ParsePosition)} bytes available");
    }
}
