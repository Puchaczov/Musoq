using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public decimal ToDecimal(string value)
        {
            return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
        }

        [BindableMethod]
        public decimal ToDecimal(string value, string culture)
        {
            return Convert.ToDecimal(value, new CultureInfo(culture));
        }

        [BindableMethod]
        public decimal? ToDecimal(long? value)
        {
            return value;
        }

        [BindableMethod]
        public decimal? ToDecimal(decimal? value)
        {
            return value;
        }

        [BindableMethod]
        public long? ToLong(string value)
        {
            return long.Parse(value);
        }

        [BindableMethod]
        public string ToString(DateTimeOffset? date)
        {
            if (!date.HasValue)
                return null;

            return date.ToString();
        }

        [BindableMethod]
        public string ToString(DateTime? date)
        {
            return date?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(decimal? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(long? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(object obj)
        {
            return obj?.ToString();
        }
    }
}
