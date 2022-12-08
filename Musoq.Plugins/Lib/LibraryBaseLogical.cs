using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Gets the value of an array at the specified index
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="values">The values</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Value of specified index</returns>
        [BindableMethod]
        public T Choose<T>(int index, params T[] values)
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
        public bool? Match(string regex, string content)
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
        public decimal? Coalesce(params decimal?[] array)
            => Coalesce<decimal?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public long? Coalesce(params long?[] array)
            => Coalesce<long?>(array);
        
        /// <summary>
        /// Gets the first non-null value in a list 
        /// </summary>
        /// <param name="array">The array</param>
        /// <returns>First non-null value</returns>
        [BindableMethod]
        public T Coalesce<T>(params T[] array)
        {
            foreach (var obj in array)
            {
                if (!Equals(obj, default(T)))
                    return obj;
            }

            return default;
        }
    }
}
