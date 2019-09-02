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
        public int Rand(int min, int max)
            => _rand.Next(min, max);

        [BindableMethod]
        public double Pow(decimal x, decimal y) => Math.Pow(Convert.ToDouble(x), Convert.ToDouble(y));

        [BindableMethod]
        public double Pow(double x, double y) => Math.Pow(x, y);

        [BindableMethod]
        public double Sqrt(decimal x) => Math.Sqrt(Convert.ToDouble(x));

        [BindableMethod]
        public double Sqrt(double x) => Math.Sqrt(x);

        [BindableMethod]
        public double PercentRank<T>(IEnumerable<T> window, T value)
            where T : IComparable<T>
        {
            var orderedWindow = window.OrderBy(w => w).ToArray();
            var index = Array.IndexOf(orderedWindow, value);

            return (index - 1) / (orderedWindow.Length - 1);
        }
    }
}
