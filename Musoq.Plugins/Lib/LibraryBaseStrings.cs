using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public string Substr(string value, int index, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (length < 1)
                return string.Empty;

            var valueLastIndex = value.Length - 1;
            var computedLastIndex = index + (length - 1);

            if (valueLastIndex < computedLastIndex)
                length = ((value.Length - 1) - index) + 1;

            return value.Substring(index, length);
        }

        [BindableMethod]
        public string Substr(string value, int length)
        {
            return Substr(value, 0, length);
        }

        [BindableMethod]
        public string Concat(params string[] strings)
        {
            var concatedStrings = new StringBuilder();

            foreach (var value in strings)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public string Concat(params object[] objects)
        {
            var concatedStrings = new StringBuilder();

            foreach (var value in objects)
                concatedStrings.Append(value);

            return concatedStrings.ToString();
        }

        [BindableMethod]
        public bool Contains(string content, string what)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(what))
                return false;

            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
        }

        [BindableMethod]
        public int IndexOf(string value, string text)
        {
            return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
