using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Skips elements from the beginning of the sequence.
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="skipCount">How many elements to skip</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Elements without the skipped ones</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public IEnumerable<T>? Skip<T>(IEnumerable<T>? values, int skipCount)
    {
        if (values == null)
            return null;

        return values.Skip(skipCount);
    }

    /// <summary>
    ///     Takes elements from the beginning of the sequence.
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="takeCount">How many elements to skip</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Only taken ones elements</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public IEnumerable<T>? Take<T>(IEnumerable<T>? values, int takeCount)
    {
        if (values == null)
            return null;

        return values.Take(takeCount);
    }

    /// <summary>
    ///     Skip and takes elements from the beginning of the sequence.
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="skipCount">How many elements to skip</param>
    /// <param name="takeCount">How many elements to skip</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Skipped and taken elements</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public IEnumerable<T>? SkipAndTake<T>(IEnumerable<T>? values, int skipCount, int takeCount)
    {
        if (values == null)
            return null;

        return values.Skip(skipCount).Take(takeCount);
    }

    /// <summary>
    ///     Turn array arguments of T into a single array.
    /// </summary>
    /// <param name="values">The values</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Array of specific type</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T[]? EnumerableToArray<T>(IEnumerable<T>? values)
    {
        if (values == null)
            return null;

        return values.ToArray();
    }

    /// <summary>
    ///     Turn array arguments of T into a single array.
    /// </summary>
    /// <param name="values">The values</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Array of specific type</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T[]? MergeArrays<T>(params T[][]? values)
    {
        if (values == null)
            return null;

        var totalLength = 0;
        foreach (var value in values)
            totalLength += value.Length;

        var result = new T[totalLength];
        var offset = 0;
        foreach (var value in values)
        {
            Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }

        return result;
    }

    /// <summary>
    ///     Computes longest common sequence of two given sequences
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="pattern">The pattern</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Longest common subsequence of two sequences</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public IEnumerable<T>? LongestCommonSequence<T>(IEnumerable<T>? source, IEnumerable<T>? pattern)
        where T : IEquatable<T>
    {
        if (source == null)
            return null;

        if (pattern == null)
            return null;

        var sourceArray = source.ToArray();
        var patternArray = pattern.ToArray();
        var sourceCount = sourceArray.Length;
        var patternCount = patternArray.Length;

        var sequenceLengths = new int[sourceCount][];
        for (var i = 0; i < sourceCount; i++)
            sequenceLengths[i] = new int[patternCount];

        var maxSubStringSequence = 0;

        IEnumerable<T>? subSequence = null;

        for (var i = 0; i < sourceCount; ++i)
        {
            var sourceElement = sourceArray[i];
            for (var j = 0; j < patternCount; ++j)
            {
                var patternElement = patternArray[j];

                if (sourceElement.Equals(patternElement))
                {
                    sequenceLengths[i][j] = i == 0 || j == 0 ? 1 : sequenceLengths[i - 1][j - 1] + 1;

                    if (sequenceLengths[i][j] <= maxSubStringSequence) continue;

                    maxSubStringSequence = sequenceLengths[i][j];
                    subSequence = sourceArray.Skip(i - maxSubStringSequence + 1).Take(maxSubStringSequence);
                }
            }
        }

        return subSequence ?? Array.Empty<T>();
    }
}
