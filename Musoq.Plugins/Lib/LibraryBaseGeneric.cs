using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Skips elements from the beginning of the sequence.
        /// </summary>
        /// <param name="values">The values</param>
        /// <param name="skipCount">How many elements to skip</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Elements without the skipped ones</returns>
        [BindableMethod]
        public IEnumerable<T>? Skip<T>(IEnumerable<T>? values, int skipCount)
        {
            if (values == null)
                return null;

            return values.Skip(skipCount);
        }
        
        /// <summary>
        /// Takes elements from the beginning of the sequence.
        /// </summary>
        /// <param name="values">The values</param>
        /// <param name="takeCount">How many elements to skip</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Only taken ones elements</returns>
        [BindableMethod]
        public IEnumerable<T>? Take<T>(IEnumerable<T>? values, int takeCount)
        {
            if (values == null)
                return null;

            return values.Take(takeCount);
        }
        
        /// <summary>
        /// Skip and takes elements from the beginning of the sequence.
        /// </summary>
        /// <param name="values">The values</param>
        /// <param name="skipCount">How many elements to skip</param>
        /// <param name="takeCount">How many elements to skip</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Skipped and taken elements</returns>
        [BindableMethod]
        public IEnumerable<T>? SkipAndTake<T>(IEnumerable<T>? values, int skipCount, int takeCount)
        {
            if (values == null)
                return null;

            return values.Skip(skipCount).Take(takeCount);
        }

        /// <summary>
        /// Turn array arguments of T into a single array.
        /// </summary>
        /// <param name="values">The values</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Array of specific type</returns>
        [BindableMethod]
        public T[]? EnumerableToArray<T>(IEnumerable<T>? values)
        {
            if (values == null)
                return null;

            return values.ToArray();
        }

        /// <summary>
        /// Turn array arguments of T into a single array.
        /// </summary>
        /// <param name="values">The values</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Array of specific type</returns>
        [BindableMethod]
        public T[]? MergeArrays<T>(params T[][]? values)
        {
            if (values == null)
                return null;

            var result = new List<T>();
            
            foreach (var value in values)
            {
                result.AddRange(value);
            }
            
            return result.ToArray();
        }

        /// <summary>
        /// Computes longest common sequence of two given sequences 
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="pattern">The pattern</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Longest common subsequence of two sequences</returns>
        [BindableMethod]
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

            var array = new int[sourceCount, patternCount];
            var maxSubStringSequence = 0;

            IEnumerable<T>? subSequence = null;

            for (var i = 0; i < sourceCount; ++i)
            {
                var sourceElement = sourceArray.ElementAt(i);
                for (var j = 0; j < patternCount; ++j)
                {
                    var patternElement = patternArray.ElementAt(j);

                    if (sourceElement.Equals(patternElement))
                    {
                        array[i, j] = i == 0 || j == 0 ? 1 : array[i - 1, j - 1] + 1;

                        if (array[i, j] <= maxSubStringSequence) continue;
                        
                        maxSubStringSequence = array[i, j];
                        subSequence = sourceArray.Skip(i - maxSubStringSequence + 1).Take(maxSubStringSequence);
                    }
                    else
                    {
                        array[i, j] = 0;
                    }
                }
            }

            return subSequence ?? Array.Empty<T>();
        }

        /// <summary>
        /// Gets the element at the specified index in a sequence
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="index">The index</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Element of a given index</returns>
        [BindableMethod]
        public T? GetElementAtOrDefault<T>(IEnumerable<T>? enumerable, int? index)
        {
            if (enumerable == null)
                return default;

            if (index == null)
                return default;

            return enumerable.ElementAtOrDefault(index.Value);
        }

        /// <summary>
        /// Gets the length of the sequence
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Length of sequence</returns>
        [BindableMethod]
        public int? Length<T>(IEnumerable<T>? enumerable)
        {
            if (enumerable == null)
                return null;

            return enumerable.Count();
        }

        /// <summary>
        /// Gets the length of the array
        /// </summary>
        /// <param name="array">The array</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Length of sequence</returns>
        [BindableMethod]
        public int? Length<T>(T[]? array)
        {
            return array?.Length;
        }
        
        /// <summary>
        /// Gets the value of an array at the specified index
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="values">The values</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Value of specified index</returns>
        [BindableMethod]
        public T? Choose<T>(int index, params T[] values)
        {
            if (values.Length <= index)
                return default;

            return values[index];
        }

        /// <summary>
        /// Chose a or b value based on the expression result
        /// </summary>
        /// <param name="expressionResult">The expression result</param>
        /// <param name="a">The A parameter</param>
        /// <param name="b">The B parameter</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Value a or b</returns>
        [BindableMethod]
        public T If<T>(bool expressionResult, T a, T b)
        {
            if (expressionResult)
                return a;

            return b;
        }
        
        /// <summary>
        /// Determine whether content matches the specified pattern
        /// </summary>
        /// <param name="regex">The regex</param>
        /// <param name="content">The content</param>
        /// <returns>True if matches, otherwise false</returns>
        [BindableMethod]
        public bool? Match(string? regex, string? content)
        {
            if (regex == null || content == null)
                return null;

            return Regex.IsMatch(content, regex);
        }
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public byte? Coalesce(params byte?[] array)
            => Coalesce<byte?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public sbyte? Coalesce(params sbyte?[] array)
            => Coalesce<sbyte?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public short? Coalesce(params short?[] array)
            => Coalesce<short?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public ushort? Coalesce(params ushort?[] array)
            => Coalesce<ushort?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public int? Coalesce(params int?[] array)
            => Coalesce<int?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public decimal? Coalesce(params uint?[] array)
            => Coalesce<uint?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public decimal? Coalesce(params long?[] array)
            => Coalesce<long?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public decimal? Coalesce(params ulong?[] array)
            => Coalesce<ulong?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public decimal? Coalesce(params decimal?[] array)
            => Coalesce<decimal?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public T? Coalesce<T>(params T[] array)
        {
            foreach (var obj in array)
            {
                if (!Equals(obj, default(T)))
                    return obj;
            }

            return default;
        }

        /// <summary>
        /// Returns distinct elements from a collection.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="values">The collection to remove duplicate elements from.</param>
        /// <returns>An IEnumerable&lt;T&gt; that contains distinct elements from the input sequence.</returns>
        [BindableMethod]
        public IEnumerable<T>? Distinct<T>(IEnumerable<T>? values)
        {
            return values?.Distinct();
        }
    }
}
