using System;
using System.Collections.Concurrent;

namespace Musoq.Plugins
{
    /// <summary>
    /// Represents an environment.
    /// </summary>
    public class Environment
    {
        private static readonly ConcurrentDictionary<string, object> Objects;

        private static readonly ConcurrentDictionary<string, Func<object, object>> Converters;

        static Environment()
        {
            Objects = new ConcurrentDictionary<string, object>();
            Converters = new ConcurrentDictionary<string, Func<object, object>>();
        }

        /// <summary>
        /// Gets the value of the environment.
        /// </summary>
        /// <param name="name">Name of the value.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The value.</returns>
        public T Value<T>(string name)
        {
            if (Converters.TryGetValue(name, out var converter))
                return (T) converter(Objects[name]);

            return (T) Objects[name];
        }

        /// <summary>
        /// Sets the value of the environment.
        /// </summary>
        /// <param name="name">Name of the value.</param>
        /// <param name="value">Value to set.</param>
        /// <typeparam name="T"></typeparam>
        public void SetValue<T>(string name, T value)
        {
            if (!Objects.TryAdd(name, value))
                Objects.TryUpdate(name, value, Objects[name]);
        }

        /// <summary>
        /// Sets the value of the environment with a converter.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="func">The converter function.</param>
        /// <typeparam name="T"></typeparam>
        public void SetValueWithConverter<T>(string name, T value, Func<object, object> func)
        {
            SetValue(name, value);

            if (!Converters.TryAdd(name, func))
                Converters.TryUpdate(name, func, Converters[name]);
        }
    }
}