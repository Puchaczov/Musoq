using System.Globalization;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class GeneratedSubqueryContract
{
    public const string SubqueryPrefix = "_sq_";
    public const string DerivedTablePrefix = "_dt_";
    public const string ScalarMaterializationPrefix = "_sm_";
    public const string ValueColumnSuffix = "_value";
    public const string KeyColumnSuffix = "_key";
    public const string CorrelationColumnMarker = "_corr_";

    public static string CreateSubqueryName(int index)
    {
        return $"{SubqueryPrefix}{index.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string CreateDerivedTableName(int index)
    {
        return $"{DerivedTablePrefix}{index.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string CreateScalarMaterializationName(string cteName)
    {
        return $"{ScalarMaterializationPrefix}{cteName.Replace(SubqueryPrefix, string.Empty, StringComparison.OrdinalIgnoreCase)}";
    }

    public static string CreateValueColumnName(string cteName)
    {
        return $"{cteName}{ValueColumnSuffix}";
    }

    public static string CreateKeyColumnName(string cteName)
    {
        return $"{cteName}{KeyColumnSuffix}";
    }

    public static string CreateCorrelationColumnName(string cteName, int index)
    {
        return $"{cteName}{CorrelationColumnMarker}{index.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool IsGeneratedSubqueryCteName(string cteName)
    {
        return IsSubqueryCteName(cteName) || IsDerivedTableCteName(cteName);
    }

    public static bool IsSubqueryCteName(string cteName)
    {
        return cteName.StartsWith(SubqueryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDerivedTableCteName(string cteName)
    {
        return cteName.StartsWith(DerivedTablePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValueColumnForCte(string columnName, string cteName)
    {
        return string.Equals(columnName, CreateValueColumnName(cteName), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCorrelationColumnName(string columnName)
    {
        return columnName.Contains(CorrelationColumnMarker, StringComparison.OrdinalIgnoreCase);
    }
}
