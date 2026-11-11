namespace Musoq.Plugins;

/// <summary>Owned byte snapshots used by compiler-proven buffer operations.</summary>
public static class ByteBufferSnapshot
{
    /// <summary>Copies at most the proven readable prefix of a complete buffer.</summary>
    public static byte[]? Copy(byte[]? value, int maximumReadBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumReadBytes);
        return value is null ? null : value.AsSpan(0, Math.Min(value.Length, maximumReadBytes)).ToArray();
    }

    /// <summary>Copies an encoded byte range while preserving its nullable and range contracts.</summary>
    public static byte[]? Copy(byte[]? value, int? offset, int? length, int maximumReadBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumReadBytes);
        if (value is null || !offset.HasValue || !length.HasValue)
            return null;

        var start = offset.GetValueOrDefault();
        var count = length.GetValueOrDefault();
        if (start < 0 || count < 0 || start > value.Length - count)
        {
            // Retain the platform codec's exact exception, parameter and validation order.
            _ = Convert.ToBase64String(value, start, count);
        }

        return value.AsSpan(start, Math.Min(count, maximumReadBytes)).ToArray();
    }

    /// <summary>Copies a prefix, retaining the null result of an empty lossless hexadecimal composition.</summary>
    public static byte[]? CopyOrNull(byte[]? value, int maximumReadBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumReadBytes);
        if (value is null || value.Length == 0)
            return null;
        return Copy(value, maximumReadBytes);
    }
}
