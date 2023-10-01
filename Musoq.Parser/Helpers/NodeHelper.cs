using System;
using System.Collections.Generic;

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

            dict.Add((typeof(ulong), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(ulong), typeof(ulong)), typeof(ulong));
            dict.Add((typeof(ulong), typeof(uint)), typeof(ulong));
            dict.Add((typeof(ulong), typeof(ushort)), typeof(ulong));

            dict.Add((typeof(int), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(int), typeof(long)), typeof(long));
            dict.Add((typeof(int), typeof(int)), typeof(int));
            dict.Add((typeof(int), typeof(short)), typeof(int));

            dict.Add((typeof(uint), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(uint), typeof(ulong)), typeof(ulong));
            dict.Add((typeof(uint), typeof(uint)), typeof(uint));
            dict.Add((typeof(uint), typeof(ushort)), typeof(uint));

            dict.Add((typeof(short), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(short), typeof(long)), typeof(long));
            dict.Add((typeof(short), typeof(int)), typeof(int));
            dict.Add((typeof(short), typeof(short)), typeof(short));

            dict.Add((typeof(ushort), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(ushort), typeof(ulong)), typeof(ulong));
            dict.Add((typeof(ushort), typeof(uint)), typeof(uint));
            dict.Add((typeof(ushort), typeof(ushort)), typeof(ushort));

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
        
        private static Type GetUnderlyingNullable(this Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);

            var isNullableType = nullableType != null;

            return isNullableType ? nullableType : type;
        }
    }
}