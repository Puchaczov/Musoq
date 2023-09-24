using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public IEnumerable<T> Take<T>(IEnumerable<T>? values, int takeCount)
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
        public IEnumerable<T> SkipAndTake<T>(IEnumerable<T>? values, int skipCount, int takeCount)
        {
            if (values == null)
                return null;

            return values.Skip(skipCount).Take(takeCount);
        }

        /// <summary>
        /// Turn IEnumerable into array.
        /// </summary>
        /// <param name="values">The values</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Array of specific type</returns>
        [BindableMethod]
        public T[] ToArray<T>(IEnumerable<T>? values)
        {
            if (values == null)
                return null;

            return values.ToArray();
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

            var subSequence = (IEnumerable<T>?)null;

            for (int i = 0; i < sourceCount; ++i)
            {
                var sourceElement = sourceArray.ElementAt(i);
                for (int j = 0; j < patternCount; ++j)
                {
                    var patternElement = patternArray.ElementAt(j);

                    if (sourceElement.Equals(patternElement))
                    {
                        array[i, j] = (i == 0 || j == 0) ? 1 : array[i - 1, j - 1] + 1;

                        if (array[i, j] > maxSubStringSequence)
                        {
                            maxSubStringSequence = array[i, j];
                            subSequence = sourceArray.Skip(i - maxSubStringSequence + 1).Take(maxSubStringSequence);
                        }
                    }
                    else
                    {
                        array[i, j] = 0;
                    }
                }
            }

            if (subSequence == null)
                return Array.Empty<T>();

            return subSequence;
        }

        /// <summary>
        /// Gets the element at the specified index in a sequence
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="index">The index</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Element of a given index</returns>
        [BindableMethod]
        public T? GetElementAt<T>(IEnumerable<T>? enumerable, int? index)
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
            if (array == null)
                return null;

            return array.Length;
        }
    }
}
