using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Supports negative indexing where -1 means last element, -2 second to last, etc.
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <param name="array">The array to access</param>
        /// <param name="index">The index to access (negative indices supported)</param>
        /// <returns>Array element if valid index, default(T) if out-of-bounds or array is null</returns>
        public static T GetArrayElement<T>(T[] array, int index)
        {
            if (array == null || array.Length == 0)
                return default(T);
            
            // Handle negative indexing: -1 = last element, -2 = second to last, etc.
            if (index < 0)
            {
                // Convert negative index to positive equivalent
                // If index goes beyond the array bounds when wrapping, use modulo to wrap around
                var positiveIndex = ((index % array.Length) + array.Length) % array.Length;
                return array[positiveIndex];
            }
            
            // Handle positive indexing with bounds check
            if (index >= array.Length)
                return default(T);
            
            return array[index];
        }

        /// <summary>
        /// Safely access a string character, returning '\0' for out-of-bounds indices
        /// Supports negative indexing where -1 means last character, -2 second to last, etc.
        /// </summary>
        /// <param name="str">The string to access</param>
        /// <param name="index">The character index to access (negative indices supported)</param>
        /// <returns>Character if valid index, '\0' if out-of-bounds or string is null</returns>
        public static char GetStringCharacter(string str, int index)
        {
            if (str == null || str.Length == 0)
                return '\0';
            
            // Handle negative indexing: -1 = last character, -2 = second to last, etc.
            if (index < 0)
            {
                // Convert negative index to positive equivalent
                // If index goes beyond the string bounds when wrapping, use modulo to wrap around
                var positiveIndex = ((index % str.Length) + str.Length) % str.Length;
                return str[positiveIndex];
            }
            
            // Handle positive indexing with bounds check
            if (index >= str.Length)
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
        /// Supports negative indexing where -1 means last element, -2 second to last, etc.
        /// </summary>
        /// <typeparam name="T">List element type</typeparam>
        /// <param name="list">The list to access</param>
        /// <param name="index">The index to access (negative indices supported)</param>
        /// <returns>List element if valid index, default(T) if out-of-bounds or list is null</returns>
        public static T GetListElement<T>(IList<T> list, int index)
        {
            if (list == null || list.Count == 0)
                return default(T);
            
            // Handle negative indexing: -1 = last element, -2 = second to last, etc.
            if (index < 0)
            {
                // Convert negative index to positive equivalent
                // If index goes beyond the list bounds when wrapping, use modulo to wrap around
                var positiveIndex = ((index % list.Count) + list.Count) % list.Count;
                return list[positiveIndex];
            }
            
            // Handle positive indexing with bounds check
            if (index >= list.Count)
                return default(T);
            
            return list[index];
        }

        /// <summary>
        /// Generic safe access for any indexable type using reflection
        /// </summary>
        /// <param name="indexable">The indexable object</param>
        /// <param name="index">The index to access (int for arrays, string for dictionaries, etc.)</param>
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

                // Handle arrays with integer indices
                if (indexable.GetType().IsArray && index is int arrayIndex)
                {
                    var array = (Array)indexable;
                    if (array.Length == 0)
                        return GetDefaultValue(elementType);
                    
                    // Handle negative indexing for arrays
                    if (arrayIndex < 0)
                    {
                        var positiveIndex = ((arrayIndex % array.Length) + array.Length) % array.Length;
                        return array.GetValue(positiveIndex);
                    }
                    
                    if (arrayIndex >= array.Length)
                        return GetDefaultValue(elementType);
                    
                    return array.GetValue(arrayIndex);
                }

                // Handle dictionaries with string keys
                if (index is string stringKey)
                {
                    var dictType = indexable.GetType();
                    
                    // Check if it's a generic dictionary
                    if (dictType.IsGenericType)
                    {
                        var genericDef = dictType.GetGenericTypeDefinition();
                        if (genericDef == typeof(Dictionary<,>) || 
                            genericDef == typeof(IDictionary<,>) ||
                            dictType.GetInterfaces().Any(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                        {
                            // Use TryGetValue to safely access dictionary
                            var tryGetValueMethod = dictType.GetMethod("TryGetValue");
                            if (tryGetValueMethod != null)
                            {
                                var parameters = new object[] { stringKey, null };
                                var found = (bool)tryGetValueMethod.Invoke(indexable, parameters);
                                return found ? parameters[1] : GetDefaultValue(elementType);
                            }
                        }
                    }
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