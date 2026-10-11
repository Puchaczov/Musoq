using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>Selects one concrete value without allocating an invocation array.</summary>
    [BindableMethod]
    public T? Choose<T>(int index, params ReadOnlySpan<T> values)
        => values.Length <= index ? default : values[index];

    /// <summary>Merges typed arrays without an outer invocation array.</summary>
    [BindableMethod]
    public T[] MergeArrays<T>(params ReadOnlySpan<T[]> values)
    {
        var length = 0;
        foreach (var value in values)
            length = checked(length + value.Length);
        var result = new T[length];
        var offset = 0;
        foreach (var value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }
        return result;
    }

    /// <summary>Adds non-null timespans with concrete span arguments.</summary>
    [BindableMethod]
    public TimeSpan? AddTimeSpans(params ReadOnlySpan<TimeSpan?> values)
    {
        TimeSpan? result = null;
        foreach (var value in values)
            if (value.HasValue)
                result = result.HasValue ? result.Value + value.Value : value;
        return result;
    }

    /// <summary>Subtracts non-null timespans from the first non-null input.</summary>
    [BindableMethod]
    public TimeSpan? SubtractTimeSpans(params ReadOnlySpan<TimeSpan?> values)
    {
        TimeSpan? result = null;
        foreach (var value in values)
            if (value.HasValue)
                result = result.HasValue ? result.Value - value.Value : value;
        return result;
    }

    /// <summary>Joins non-null strings using native variadic storage.</summary>
    [BindableMethod]
    public string? StringsJoin(string? separator, params ReadOnlySpan<string?> values)
    {
        if (separator is null)
            return null;
        var builder = new StringBuilder();
        var hasValue = false;
        foreach (var value in values)
        {
            if (value is null)
                continue;
            if (hasValue)
                builder.Append(separator);
            builder.Append(value);
            hasValue = true;
        }
        return builder.ToString();
    }

    /// <summary>Concatenates strings and characters with concrete span arguments.</summary>
    [BindableMethod]
    public string? Concat(string? firstString, params ReadOnlySpan<char> characters)
    {
        if (firstString is null)
            return null;
        var builder = new StringBuilder(firstString);
        builder.Append(characters);
        return builder.ToString();
    }

    /// <summary>Concatenates characters without invoking an argument array.</summary>
    [BindableMethod]
    public string Concat(params ReadOnlySpan<char> characters) => new(characters);

    /// <summary>Concatenates strings without invoking an argument array.</summary>
    [BindableMethod]
    public string? Concat(params ReadOnlySpan<string?> values)
    {
        var builder = new StringBuilder();
        foreach (var value in values)
        {
            if (value is null)
                return null;
            builder.Append(value);
        }
        return builder.ToString();
    }
    /// <summary>Splits strings while scanning concrete separators without an invocation array.</summary>
    [BindableMethod]
    public string[] Split(string? value, params ReadOnlySpan<string> separators)
    {
        if (value is null)
            return [];
        if (separators.IsEmpty)
            return value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Collections.Generic.List<string>();
        var start = 0;
        while (start < value.Length)
        {
            var match = -1;
            var length = 0;
            foreach (var separator in separators)
            {
                if (string.IsNullOrEmpty(separator))
                    continue;
                var position = value.IndexOf(separator, start, StringComparison.Ordinal);
                if (position >= 0 && (match < 0 || position < match))
                {
                    match = position;
                    length = separator.Length;
                }
            }
            if (match < 0)
            {
                result.Add(value[start..]);
                break;
            }
            if (match > start)
                result.Add(value[start..match]);
            start = match + length;
        }
        return result.ToArray();
    }

    /// <summary>Selects the first non-default typed value without boxing numeric inputs.</summary>
    [BindableMethod]
    public T? Coalesce<T>(params ReadOnlySpan<T> values)
    {
        foreach (var value in values)
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, default!))
                return value;
        return default;
    }

    /// <summary>Selects the first non-null byte value using concrete span storage.</summary>
    [BindableMethod]
    public byte? Coalesce(params ReadOnlySpan<byte?> values) => Coalesce<byte?>(values);

    /// <summary>Selects the first non-null sbyte value using concrete span storage.</summary>
    [BindableMethod]
    public sbyte? Coalesce(params ReadOnlySpan<sbyte?> values) => Coalesce<sbyte?>(values);

    /// <summary>Selects the first non-null short value using concrete span storage.</summary>
    [BindableMethod]
    public short? Coalesce(params ReadOnlySpan<short?> values) => Coalesce<short?>(values);

    /// <summary>Selects the first non-null ushort value using concrete span storage.</summary>
    [BindableMethod]
    public ushort? Coalesce(params ReadOnlySpan<ushort?> values) => Coalesce<ushort?>(values);

    /// <summary>Selects the first non-null int value using concrete span storage.</summary>
    [BindableMethod]
    public int? Coalesce(params ReadOnlySpan<int?> values) => Coalesce<int?>(values);

    /// <summary>Selects the first non-null uint value using concrete span storage.</summary>
    [BindableMethod]
    public decimal? Coalesce(params ReadOnlySpan<uint?> values) => Coalesce<uint?>(values);

    /// <summary>Selects the first non-null long value using concrete span storage.</summary>
    [BindableMethod]
    public decimal? Coalesce(params ReadOnlySpan<long?> values) => Coalesce<long?>(values);

    /// <summary>Selects the first non-null ulong value using concrete span storage.</summary>
    [BindableMethod]
    public decimal? Coalesce(params ReadOnlySpan<ulong?> values) => Coalesce<ulong?>(values);

    /// <summary>Selects the first non-null decimal value using concrete span storage.</summary>
    [BindableMethod]
    public decimal? Coalesce(params ReadOnlySpan<decimal?> values) => Coalesce<decimal?>(values);

}
