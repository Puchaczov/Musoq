using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers
{
    /// <summary>
    /// Provides safe array access methods that return default values instead of throwing exceptions
    /// for out-of-bounds access, following SQL semantics (NULL for missing/invalid access)
    /// </summary>
    public static class SafeArrayAccess
    {
        /// <summary>
        /// Safely access an array element, returning default(T) for out-of-bounds indices
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <param name="array">The array to access</param>
        /// <param name="index">The index to access</param>
        /// <returns>Array element if valid index, default(T) if out-of-bounds or array is null</returns>
        public static T GetArrayElement<T>(T[] array, int index)
        {
            if (array == null || index < 0 || index >= array.Length)
                return default(T);
            
            return array[index];
        }

        /// <summary>
        /// Safely access a string character, returning '\0' for out-of-bounds indices
        /// </summary>
        /// <param name="str">The string to access</param>
        /// <param name="index">The character index to access</param>
        /// <returns>Character if valid index, '\0' if out-of-bounds or string is null</returns>
        public static char GetStringCharacter(string str, int index)
        {
            if (str == null || index < 0 || index >= str.Length)
                return '\0';
            
            return str[index];
        }

        /// <summary>
        /// Safely access a dictionary value, returning default(TValue) for missing keys
        /// </summary>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary value type</typeparam>
        /// <param name="dictionary">The dictionary to access</param>
        /// <param name="key">The key to look up</param>
        /// <returns>Value if key exists, default(TValue) if key missing or dictionary is null</returns>
        public static TValue GetDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null || key == null)
                return default(TValue);
            
            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        /// <summary>
        /// Safely access a list element, returning default(T) for out-of-bounds indices
        /// </summary>
        /// <typeparam name="T">List element type</typeparam>
        /// <param name="list">The list to access</param>
        /// <param name="index">The index to access</param>
        /// <returns>List element if valid index, default(T) if out-of-bounds or list is null</returns>
        public static T GetListElement<T>(IList<T> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return default(T);
            
            return list[index];
        }

        /// <summary>
        /// Generic safe access for any indexable type using reflection
        /// </summary>
        /// <param name="indexable">The indexable object</param>
        /// <param name="index">The index to access</param>
        /// <param name="elementType">The expected element type</param>
        /// <returns>Element if valid, default value if out-of-bounds or error</returns>
        public static object GetIndexedElement(object indexable, object index, Type elementType)
        {
            if (indexable == null || index == null)
                return GetDefaultValue(elementType);

            try
            {
                // Handle string character access
                if (indexable is string str && index is int intIndex)
                {
                    return GetStringCharacter(str, intIndex);
                }

                // Handle arrays
                if (indexable.GetType().IsArray && index is int arrayIndex)
                {
                    var array = (Array)indexable;
                    if (arrayIndex < 0 || arrayIndex >= array.Length)
                        return GetDefaultValue(elementType);
                    
                    return array.GetValue(arrayIndex);
                }

                // Handle generic collections with indexers
                var indexerProperty = indexable.GetType().GetProperty("Item");
                if (indexerProperty != null)
                {
                    try
                    {
                        return indexerProperty.GetValue(indexable, new[] { index });
                    }
                    catch (System.Reflection.TargetInvocationException ex)
                    {
                        // Handle inner exceptions from indexer access
                        if (ex.InnerException is ArgumentOutOfRangeException ||
                            ex.InnerException is IndexOutOfRangeException ||
                            ex.InnerException is KeyNotFoundException)
                        {
                            return GetDefaultValue(elementType);
                        }
                        throw;
                    }
                }

                return GetDefaultValue(elementType);
            }
            catch (ArgumentOutOfRangeException)
            {
                return GetDefaultValue(elementType);
            }
            catch (IndexOutOfRangeException)
            {
                return GetDefaultValue(elementType);
            }
            catch (KeyNotFoundException)
            {
                return GetDefaultValue(elementType);
            }
        }

        /// <summary>
        /// Get the default value for a type (following SQL NULL semantics)
        /// </summary>
        /// <param name="type">The type to get default value for</param>
        /// <returns>Default value: null for reference types, default(T) for value types</returns>
        private static object GetDefaultValue(Type type)
        {
            if (type == null)
                return null;

            // For nullable value types, return null
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            // For reference types, return null (SQL-like behavior)
            if (!type.IsValueType)
                return null;

            // For value types, return default(T)
            return Activator.CreateInstance(type);
        }
    }
}