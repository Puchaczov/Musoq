using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        private readonly Random _rand = new();

        /// <summary>
        /// Gets the absolute value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Absolute value</returns>
        [BindableMethod]
        public decimal? Abs(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        /// <summary>
        /// Gets the absolute value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Absolute value</returns>
        [BindableMethod]
        public long? Abs(long? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        /// <summary>
        /// Gets the absolute value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Absolute value</returns>
        [BindableMethod]
        public int? Abs(int? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        /// <summary>
        /// Gets the ceiling value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Ceiling value</returns>
        [BindableMethod]
        public decimal? Ceil(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Ceiling(value.Value);
        }

        /// <summary>
        /// Gets the floor value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Floor value</returns>
        [BindableMethod]
        public decimal? Floor(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Floor(value.Value);
        }

        /// <summary>
        /// Determine whether value is greater, equal or less that zero
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Is less, equal or greater value</returns>
        [BindableMethod]
        public decimal? Sign(decimal? value)
        {
            if (!value.HasValue)
                return null;

            if (value.Value > 0)
                return 1;
            if (value.Value == 0)
                return 0;

            return -1;
        }
        
        /// <summary>
        /// Determine whether value is greater, equal or less that zero
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Is less, equal or greater value</returns>
        [BindableMethod]
        public long? Sign(long? value)
        {
            if (!value.HasValue)
                return null;

            if (value.Value > 0)
                return 1;
            if (value.Value == 0)
                return 0;

            return -1;
        }

        /// <summary>
        /// Rounds the value within given precision
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="precision">The precision</param>
        /// <returns>Is less, equal or greater value</returns>
        [BindableMethod]
        public decimal? Round(decimal? value, int precision)
        {
            if (!value.HasValue)
                return null;

            return Math.Round(value.Value, precision);
        }
        
        /// <summary>
        /// Gets the percentage of the value
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="max">The max</param>
        /// <returns>Percentage of a given value</returns>
        [BindableMethod]
        public decimal? PercentOf(decimal? value, decimal? max)
        {
            if (!value.HasValue)
                return null;

            if (!max.HasValue)
                return null;

            return value * 100 / max;
        }

        /// <summary>
        /// Gets the random integer value
        /// </summary>
        /// <returns>Random integer</returns>
        [BindableMethod]
        public int Rand()
            => _rand.Next();

        /// <summary>
        /// Gets the random integer value
        /// </summary>
        /// <param name="min">The min</param>
        /// <param name="max">The max</param>
        /// <returns>Random value between min and max</returns>
        [BindableMethod]
        public int? Rand(int? min, int? max)
        {
            if (min == null || max == null)
                return null;
            
            return _rand.Next(min.Value, max.Value);
        }

        /// <summary>
        /// Computes the pow between two values
        /// </summary>
        /// <param name="x">The x</param>
        /// <param name="y">The y</param>
        /// <returns>Power of two values</returns>
        [BindableMethod]
        public double? Pow(decimal? x, decimal? y)
        {
            if (x == null || y == null)
                return null;

            return Math.Pow(Convert.ToDouble(x.Value), Convert.ToDouble(y.Value));
        }

        /// <summary>
        /// Computes the pow between two values
        /// </summary>
        /// <param name="x">The x</param>
        /// <param name="y">The y</param>
        /// <returns>Power of two values</returns>
        [BindableMethod]
        public double? Pow(double? x, double? y)
        {
            if (x == null || y == null)
                return null;

            return Math.Pow(x.Value, y.Value);
        }

        /// <summary>
        /// Computes the sqrt of a given value
        /// </summary>
        /// <param name="x">The x</param>
        /// <returns>Sqrt of a value</returns>
        [BindableMethod]
        public double? Sqrt(decimal? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(Convert.ToDouble(x.Value));
        }

        /// <summary>
        /// Computes the sqrt of a given value
        /// </summary>
        /// <param name="x">The x</param>
        /// <returns>Sqrt of a value</returns>
        [BindableMethod]
        public double? Sqrt(double? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(x.Value);
        }

        /// <summary>
        /// Computes the sqrt of a given value
        /// </summary>
        /// <param name="x">The x</param>
        /// <returns>Sqrt of a value</returns>
        [BindableMethod]
        public double? Sqrt(long? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(x.Value);
        }

        /// <summary>
        /// Computes the percent rank of a given window
        /// </summary>
        /// <param name="window">The window</param>
        /// <param name="value">The value existing in a given window</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Percent rank of a given value</returns>
        [BindableMethod]
        public double? PercentRank<T>(IEnumerable<T>? window, T? value)
            where T : IComparable<T>
        {
            if (window == null || value == null)
                return null;

            var orderedWindow = window.OrderBy(w => w).ToArray();
            var index = Array.IndexOf(orderedWindow, value);

            return (index - 1) / (orderedWindow.Length - 1);
        }
    }
}
