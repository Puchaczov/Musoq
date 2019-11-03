using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Schema.Helpers;

namespace Musoq.Parser.Helpers
{
    public static class NodeHelper
    {
        static NodeHelper()
        {
            var dict = new Dictionary<(Type, Type), Type>();
            BinaryTypes = dict;

            dict.Add((typeof(decimal), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(long)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(int)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(short)), typeof(decimal));

            dict.Add((typeof(long), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(long), typeof(long)), typeof(long));
            dict.Add((typeof(long), typeof(int)), typeof(long));
            dict.Add((typeof(long), typeof(short)), typeof(long));

            dict.Add((typeof(int), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(int), typeof(long)), typeof(long));
            dict.Add((typeof(int), typeof(int)), typeof(long));
            dict.Add((typeof(int), typeof(short)), typeof(long));

            dict.Add((typeof(short), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(short), typeof(long)), typeof(long));
            dict.Add((typeof(short), typeof(int)), typeof(long));
            dict.Add((typeof(short), typeof(short)), typeof(long));

            dict.Add((typeof(string), typeof(string)), typeof(string));

            dict.Add((typeof(bool), typeof(bool)), typeof(bool));

            dict.Add((typeof(DateTimeOffset), typeof(DateTimeOffset)), typeof(DateTimeOffset));
            dict.Add((typeof(DateTimeOffset), typeof(TimeSpan)), typeof(DateTimeOffset));

            dict.Add((typeof(DateTime), typeof(DateTime)), typeof(DateTime));
            dict.Add((typeof(DateTime), typeof(TimeSpan)), typeof(DateTime));

            dict.Add((typeof(object), typeof(object)), typeof(object));

            dict.Add((typeof(TimeSpan), typeof(TimeSpan)), typeof(TimeSpan));
        }

        public static IReadOnlyDictionary<(Type, Type), Type> BinaryTypes { get; }

        public static Type GetReturnTypeMap(Type left, Type right)
        {
            return BinaryTypes[(left.GetUnderlyingNullable(), right.GetUnderlyingNullable())];
        }
    }
}