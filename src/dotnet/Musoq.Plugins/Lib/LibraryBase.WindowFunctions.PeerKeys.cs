namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static bool EqualKeys(object? a, object? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (a is IComparable ca)
            return ca.CompareTo(b) == 0;

        return Equals(a, b);
    }
}
