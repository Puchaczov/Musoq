using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>Concatenates concretely typed variadic values without an invocation array.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<T>(params ReadOnlySpan<T> values)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var value in values)
        {
            if (value is null)
                return null;
            builder.Append(value.ToString());
        }
        return builder.ToString();
    }

    /// <summary>Concatenates two concrete values without constructing an argument array.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<TFirst, TSecond>(TFirst first, TSecond second)
        => first is null || second is null ? null : string.Concat(first.ToString(), second.ToString());

    /// <summary>Concatenates three concrete values without constructing an argument array.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<TFirst, TSecond, TThird>(TFirst first, TSecond second, TThird third)
        => first is null || second is null || third is null
            ? null : string.Concat(first.ToString(), second.ToString(), third.ToString());

    /// <summary>Concatenates four concrete values without constructing an argument array.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<TFirst, TSecond, TThird, TFourth>(TFirst first, TSecond second, TThird third, TFourth fourth)
        => first is null || second is null || third is null || fourth is null
            ? null : string.Concat(first.ToString(), second.ToString(), third.ToString(), fourth.ToString());
    /// <summary>Concatenates six concrete values without a variadic invocation carrier.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(
        TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth, TSixth sixth)
    {
        if (first is null || second is null || third is null || fourth is null || fifth is null || sixth is null)
            return null;
        var builder = new System.Text.StringBuilder();
        builder.Append(first.ToString());
        builder.Append(second.ToString());
        builder.Append(third.ToString());
        builder.Append(fourth.ToString());
        builder.Append(fifth.ToString());
        builder.Append(sixth.ToString());
        return builder.ToString();
    }
}
