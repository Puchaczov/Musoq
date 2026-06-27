using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Generates a new random GUID.
    /// </summary>
    /// <returns>A new GUID as string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string NewGuid()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    ///     Generates a new random GUID without dashes.
    /// </summary>
    /// <returns>A new GUID as string without dashes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string NewGuidCompact()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    ///     Converts a value from one number base to another.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="fromBase">The source base (2-36)</param>
    /// <param name="toBase">The target base (2-36)</param>
    /// <returns>The converted value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ConvertBase(string? value, int fromBase, int toBase)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (fromBase < 2 || fromBase > 36 || toBase < 2 || toBase > 36)
            return null;

        try
        {
            var number = Convert.ToInt64(value, fromBase);
            return ConvertToBase(number, toBase);
        }
        catch
        {
            return null;
        }
    }

    private static string ConvertToBase(long number, int toBase)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (number == 0) return "0";

        var negative = number < 0;
        number = Math.Abs(number);

        var result = new StringBuilder();
        while (number > 0)
        {
            result.Insert(0, chars[(int)(number % toBase)]);
            number /= toBase;
        }

        return negative ? "-" + result : result.ToString();
    }
}
