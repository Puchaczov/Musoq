using System.Globalization;
using System.Runtime.CompilerServices;

namespace Musoq.Benchmarks;

internal static class NativeBenchmarkColumnConsumption
{
    internal static long ReadInteger(Musoq.Evaluator.Tables.Row row, int column)
    {
        var visitor = new IntegerVisitor();
        row.VisitColumn(column, ref visitor);
        return visitor.Value;
    }

    internal static int ReadTextLength(Musoq.Evaluator.Tables.Row row, int column)
    {
        var visitor = new TextLengthVisitor();
        row.VisitColumn(column, ref visitor);
        return visitor.Value;
    }

    private struct IntegerVisitor : Musoq.Schema.IColumnValueVisitor
    {
        public long Value;
        public void Visit<T>(int columnIndex, in T value)
        {
            var copy = value;
            if (typeof(T) == typeof(decimal)) Value = Convert.ToInt64(Unsafe.As<T, decimal>(ref copy));
            else if (typeof(T) == typeof(decimal?)) Value = Convert.ToInt64(Unsafe.As<T, decimal?>(ref copy).GetValueOrDefault());
            else Value = Checksum(in value);
        }
    }

    private struct TextLengthVisitor : Musoq.Schema.IColumnValueVisitor
    {
        public int Value;
        public void Visit<T>(int columnIndex, in T value) => Value = value?.ToString()?.Length ?? 0;
    }

    internal static string Format<T>(in T value)
    {
        var copy = value;
        if (value is null) return "<null>";
        if (typeof(T) == typeof(byte)) return Unsafe.As<T, byte>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(sbyte)) return Unsafe.As<T, sbyte>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(short)) return Unsafe.As<T, short>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(ushort)) return Unsafe.As<T, ushort>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(int)) return Unsafe.As<T, int>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(uint)) return Unsafe.As<T, uint>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(long)) return Unsafe.As<T, long>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(ulong)) return Unsafe.As<T, ulong>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(float)) return Unsafe.As<T, float>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(double)) return Unsafe.As<T, double>(ref copy).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(decimal)) return Unsafe.As<T, decimal>(ref copy).ToString(CultureInfo.InvariantCulture);
        return value.ToString() ?? "<null>";
    }

    internal static long Checksum<T>(in T value)
    {
        var copy = value;
        if (value is null) return 0;
        if (typeof(T) == typeof(byte)) return unchecked((long)Unsafe.As<T, byte>(ref copy));
        if (typeof(T) == typeof(byte?)) return unchecked((long)Unsafe.As<T, byte?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(sbyte)) return unchecked((long)Unsafe.As<T, sbyte>(ref copy));
        if (typeof(T) == typeof(sbyte?)) return unchecked((long)Unsafe.As<T, sbyte?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(short)) return unchecked((long)Unsafe.As<T, short>(ref copy));
        if (typeof(T) == typeof(short?)) return unchecked((long)Unsafe.As<T, short?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(ushort)) return unchecked((long)Unsafe.As<T, ushort>(ref copy));
        if (typeof(T) == typeof(ushort?)) return unchecked((long)Unsafe.As<T, ushort?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(int)) return unchecked((long)Unsafe.As<T, int>(ref copy));
        if (typeof(T) == typeof(int?)) return unchecked((long)Unsafe.As<T, int?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(uint)) return unchecked((long)Unsafe.As<T, uint>(ref copy));
        if (typeof(T) == typeof(uint?)) return unchecked((long)Unsafe.As<T, uint?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(long)) return unchecked((long)Unsafe.As<T, long>(ref copy));
        if (typeof(T) == typeof(long?)) return unchecked((long)Unsafe.As<T, long?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(ulong)) return unchecked((long)Unsafe.As<T, ulong>(ref copy));
        if (typeof(T) == typeof(ulong?)) return unchecked((long)Unsafe.As<T, ulong?>(ref copy).GetValueOrDefault());
        if (typeof(T) == typeof(bool)) return Unsafe.As<T, bool>(ref copy) ? 1 : 0;
        if (typeof(T) == typeof(string))
        {
            var checksum = 17L;
            foreach (var character in Unsafe.As<T, string>(ref copy)) checksum = unchecked(checksum * 31 + character);
            return checksum;
        }
        throw new InvalidOperationException($"Unexpected enum benchmark column type '{typeof(T)}'.");
    }
}
