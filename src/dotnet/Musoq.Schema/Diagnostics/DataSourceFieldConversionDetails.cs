namespace Musoq.Schema.Diagnostics;

/// <summary>
/// Optional typed metadata on a provider's <see cref="FormatException"/>. Hosts that do not
/// recognize these <see cref="Exception.Data"/> keys still receive the original exception.
/// </summary>
public static class DataSourceFieldConversionDetails
{
    /// <summary>One-based logical data record number, stored as a <see cref="long"/>.</summary>
    public const string RecordNumberKey = "Musoq.DataSource.FieldConversion.RecordNumber";

    /// <summary>One-based physical column ordinal, stored as an <see cref="int"/>.</summary>
    public const string ColumnOrdinalKey = "Musoq.DataSource.FieldConversion.ColumnOrdinal";

    /// <summary>Expected field type, stored as a <see cref="Type"/>.</summary>
    public const string ExpectedTypeKey = "Musoq.DataSource.FieldConversion.ExpectedType";

    internal static (long RecordNumber, int ColumnOrdinal, string ExpectedType)? Find(Exception? exception)
    {
        for (var current = exception; current is not null and not AggregateException; current = current.InnerException)
        {
            if (current is not FormatException)
                continue;

            var data = current.Data;
            if (
                data[RecordNumberKey] is long recordNumber and > 0
                && data[ColumnOrdinalKey] is int columnOrdinal and > 0
                && data[ExpectedTypeKey] is Type expectedType
            )
            {
                return (recordNumber, columnOrdinal, GetSafeTypeName(expectedType));
            }
        }

        return null;
    }

    private static string GetSafeTypeName(Type expectedType)
    {
        var type = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
        if (type == typeof(Guid))
            return nameof(Guid);
        if (type == typeof(DateOnly))
            return nameof(DateOnly);
        if (type == typeof(TimeOnly))
            return nameof(TimeOnly);
        if (type == typeof(DateTimeOffset))
            return nameof(DateTimeOffset);
        if (type == typeof(TimeSpan))
            return nameof(TimeSpan);

        var typeCode = Type.GetTypeCode(type);
        return typeCode is TypeCode.Object or TypeCode.Empty or TypeCode.DBNull ? "Value" : typeCode.ToString();
    }
}
