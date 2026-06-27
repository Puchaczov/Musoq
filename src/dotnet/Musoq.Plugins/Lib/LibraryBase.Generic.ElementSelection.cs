using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the element at the specified index in a sequence
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="index">The index</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Element of a given index</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? GetElementAtOrDefault<T>(IEnumerable<T>? enumerable, int? index)
    {
        if (enumerable == null)
            return default;

        if (index == null)
            return default;

        return enumerable.ElementAtOrDefault(index.Value);
    }

    /// <summary>
    ///     Gets the element at the specified index in a sequence
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="index">The index</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Element of a given index</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? GetElementAt<T>(IEnumerable<T>? enumerable, int? index)
    {
        if (enumerable == null)
            return default;

        if (index == null)
            return default;

        if (enumerable is IList<T> list && (index.Value < 0 || index.Value >= list.Count))
            throw new ArgumentOutOfRangeException(nameof(index));
        return enumerable.ElementAt(index.Value);
    }

    /// <summary>
    ///     Gets the length of the sequence
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Length of sequence</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public int? Length<T>(IEnumerable<T>? enumerable)
    {
        if (enumerable == null)
            return null;

        return enumerable.Count();
    }

    /// <summary>
    ///     Gets the length of the array
    /// </summary>
    /// <param name="array">The array</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Length of sequence</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public int? Length<T>(T[]? array)
    {
        return array?.Length;
    }

    /// <summary>
    ///     Gets the value of an array at the specified index
    /// </summary>
    /// <param name="index">The index</param>
    /// <param name="values">The values</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Value of specified index</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? Choose<T>(int index, params T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length <= index)
            return default;

        return values[index];
    }

    /// <summary>
    ///     Returns distinct elements from a collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="values">The collection to remove duplicate elements from.</param>
    /// <returns>An IEnumerable&lt;T&gt; that contains distinct elements from the input sequence.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public IEnumerable<T>? Distinct<T>(IEnumerable<T>? values)
    {
        return values?.Distinct();
    }

    /// <summary>
    ///     Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    /// <param name="values">The values</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The first element of a sequence, or a default value if the sequence contains no elements.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? FirstOrDefault<T>(IEnumerable<T>? values)
    {
        if (values == null)
            return default;

        return values.FirstOrDefault();
    }

    /// <summary>
    ///     Returns the last element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    /// <param name="values">The values</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The last element of a sequence, or a default value if the sequence contains no elements.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? LastOrDefault<T>(IEnumerable<T>? values)
    {
        if (values == null)
            return default;

        return values.LastOrDefault();
    }

    /// <summary>
    ///     Returns the element at a specified index in a sequence or a default value if the index is out of range.
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="index">The index</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The element at a specified index in a sequence or a default value if the index is out of range.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? NthOrDefault<T>(IEnumerable<T>? values, int index)
    {
        if (values == null)
            return default;

        return values.ElementAtOrDefault(index);
    }

    /// <summary>
    ///     Returns the element at a specified index from the end of a sequence or a default value if the index is out of
    ///     range.
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="index">The index</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The element at a specified index from the end of a sequence or a default value if the index is out of range.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? NthFromEndOrDefault<T>(IEnumerable<T>? values, int index)
    {
        if (values == null)
            return default;

        return values.GetType() switch
        {
            List<T> list => list.ElementAtOrDefault(list.Count - index - 1),
            T[] array => array.ElementAtOrDefault(array.Length - index - 1),
            _ => values.Reverse().ElementAtOrDefault(index)
        };
    }
}
