using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        private readonly Random _rand = new Random();

        [BindableMethod]
        public decimal? Abs(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public long? Abs(long? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public int? Abs(int? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Abs(value.Value);
        }

        [BindableMethod]
        public decimal? Ceil(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Ceiling(value.Value);
        }

        [BindableMethod]
        public decimal? Floor(decimal? value)
        {
            if (!value.HasValue)
                return null;

            return Math.Floor(value.Value);
        }

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

        [BindableMethod]
        public decimal? Round(decimal? value, int precision)
        {
            if (!value.HasValue)
                return null;

            return Math.Round(value.Value, precision);
        }

        [BindableMethod]
        public decimal? PercentOf(decimal? value, decimal? max)
        {
            if (!value.HasValue)
                return null;

            if (!max.HasValue)
                return null;

            return value * 100 / max;
        }

        [BindableMethod]
        public int Rand()
            => _rand.Next();

        [BindableMethod]
        public int? Rand(int? min, int? max)
        {
            if (min == null || max == null)
                return null;
            
            return _rand.Next(min.Value, max.Value);
        }

        [BindableMethod]
        public double? Pow(decimal? x, decimal? y)
        {
            if (x == null || y == null)
                return null;

            return Math.Pow(Convert.ToDouble(x.Value), Convert.ToDouble(y.Value));
        }

    [BindableMethod]
        public double? Pow(double? x, double? y)
        {
            if (x == null || y == null)
                return null;

            return Math.Pow(x.Value, y.Value);
        }

        [BindableMethod]
        public double? Sqrt(decimal? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(Convert.ToDouble(x.Value));
        }

        [BindableMethod]
        public double? Sqrt(double? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(x.Value);
        }

        [BindableMethod]
        public double? Sqrt(long? x)
        {
            if (x == null)
                return null;

            return Math.Sqrt(x.Value);
        }

        [BindableMethod]
        public double? PercentRank<T>(IEnumerable<T> window, T value)
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
