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
        public decimal? ToDecimal(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return null;
        }

        [BindableMethod]
        public decimal? ToDecimal(string value, string culture)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo(culture), out decimal result))
                return result;

            return null;
        }

        [BindableMethod]
        public decimal? ToDecimal(long? value)
        {
            return value;
        }

        [BindableMethod]
        public long? ToLong(string value)
        {
            if (long.TryParse(value, out var number))
                return number;

            return null;
        }

        [BindableMethod]
        public string ToString(DateTimeOffset? date)
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
