using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public byte? Coalesce(params byte?[] array)
    {
        return Coalesce<byte?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public sbyte? Coalesce(params sbyte?[] array)
    {
        return Coalesce<sbyte?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public short? Coalesce(params short?[] array)
    {
        return Coalesce<short?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public ushort? Coalesce(params ushort?[] array)
    {
        return Coalesce<ushort?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public int? Coalesce(params int?[] array)
    {
        return Coalesce<int?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public decimal? Coalesce(params uint?[] array)
    {
        return Coalesce<uint?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public decimal? Coalesce(params long?[] array)
    {
        return Coalesce<long?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public decimal? Coalesce(params ulong?[] array)
    {
        return Coalesce<ulong?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public decimal? Coalesce(params decimal?[] array)
    {
        return Coalesce<decimal?>(array);
    }

    /// <summary>
    ///     Gets the first non-null value in a list
    /// </summary>
    /// <param name="array">The array</param>
    /// <returns>First non-null value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? Coalesce<T>(params T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        foreach (var obj in array)
            if (!Equals(obj, default(T)))
                return obj;

        return default;
    }
}
