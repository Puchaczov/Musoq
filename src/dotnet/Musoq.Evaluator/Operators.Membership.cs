namespace Musoq.Evaluator;

public partial class Operators
{
    /// <summary>Applies SQL null admission to a planner-selected constant membership lookup.</summary>
    public static bool? SqlIn<T>(T value, Func<T, bool> contains, bool hasNull)
    {
        ArgumentNullException.ThrowIfNull(contains);
        if (value is null) return null;
        if (contains(value)) return true;
        return hasNull ? null : false;
    }
}
