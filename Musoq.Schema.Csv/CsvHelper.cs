using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Musoq.Schema.Csv
{
    public static class CsvHelper
    {
        public const string AutoColumnName = "Column{0}";

        private static readonly Regex NonAlphaNumericCharacters = new Regex("[^a-zA-Z0-9 -]");

        public static string MakeHeaderNameValidColumnName(string header)
        {
            if (header.Length == 0)
                return string.Empty;

            header = header.Replace(' ', '_');

            var newString = new StringBuilder();

            newString.Append(header[0]);
            var lastChar = header[0];

            for (int i = 1; i < header.Length; i++)
            {
                var currentChar = header[i];
                if (lastChar == '_' && char.IsLower(currentChar))
                    newString.Append(char.ToUpper(currentChar));
                else
                    newString.Append(currentChar);

                lastChar = currentChar;
            }

            header = NonAlphaNumericCharacters.Replace(newString.ToString(), string.Empty);

            return header;
        }
    }
}
