namespace Musoq.Evaluator.Helpers;

public static class NamingHelper
{
    public static string ToTransitionTable(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}TransitionTable";
    }

    public static string ToGroupingTable(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}GroupingTable";
    }

    public static string ToInfoTable(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}InferredInfoTable";
    }

    public static string ToRowsSource(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}Rows";
    }

    public static string ToRowItem(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}Row";
    }

    public static string ToScoreTable(this string name)
    {
        return $"{SyntaxHelper.ToCamelCase(name)}Score";
    }

    public static string ToTransformedRowsSource(this string name)
    {
        return name;
    }

    public static string WithRowsUsage(this string name)
    {
        return $"{name}.Rows";
    }

    public static string ToColumnName(string? alias, string name)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(name))
            return name;

        var separatorIndex = name.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            var prefix = name[..separatorIndex];
            if (string.Equals(prefix, alias, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return $"{alias}.{name}";
    }

    public static string ToSetOperatorKey(this string left, string right)
    {
        return $"{left}{right}SetKey";
    }

    public static string ToRefreshMethodsSymbolName(this string left)
    {
        return $"{left}RefreshMethods";
    }

    public static string ListOf<T>()
    {
        return $"List<{typeof(T).Name}>";
    }
}
