using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for binary data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class BytesInterpreterBase<TOut>
{
    protected short ReadInt16Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(ParsePosition, 2));
        ParsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected short ReadInt16Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadInt16BigEndian(data.Slice(ParsePosition, 2));
        ParsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ushort ReadUInt16Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(ParsePosition, 2));
        ParsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ushort ReadUInt16Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(ParsePosition, 2));
        ParsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit signed integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int ReadInt32Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int ReadInt32Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadInt32BigEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ReadUInt32Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ReadUInt32Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit signed integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected long ReadInt64Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected long ReadInt64Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadInt64BigEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ulong ReadUInt64Le(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ulong ReadUInt64Be(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit float in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float ReadSingleLe(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit float in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float ReadSingleBe(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadSingleBigEndian(data.Slice(ParsePosition, 4));
        ParsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit double in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected double ReadDoubleLe(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit double in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected double ReadDoubleBe(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadDoubleBigEndian(data.Slice(ParsePosition, 8));
        ParsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a byte array of the specified length.
    /// </summary>
    /// <exception cref="ParseException">Thrown when length is negative.</exception>
}
