using System;
using System.Collections.Generic;

namespace Musoq.Parser.Helpers;

public static class NodeHelpers
{
    static NodeHelpers()
    {
        var binaryTypes = new Dictionary<(Type, Type), Type>
        {
            { (typeof(byte), typeof(byte)), typeof(int) },
            { (typeof(byte), typeof(sbyte)), typeof(int) },
            { (typeof(byte), typeof(short)), typeof(int) },
            { (typeof(byte), typeof(ushort)), typeof(int) },
            { (typeof(byte), typeof(int)), typeof(int) },
            { (typeof(byte), typeof(uint)), typeof(uint) },
            { (typeof(byte), typeof(long)), typeof(long) },
            { (typeof(byte), typeof(ulong)), typeof(ulong) },
            { (typeof(byte), typeof(float)), typeof(float) },
            { (typeof(byte), typeof(double)), typeof(double) },
            { (typeof(byte), typeof(decimal)), typeof(decimal) },

            { (typeof(sbyte), typeof(byte)), typeof(int) },
            { (typeof(sbyte), typeof(sbyte)), typeof(int) },
            { (typeof(sbyte), typeof(short)), typeof(int) },
            { (typeof(sbyte), typeof(ushort)), typeof(int) },
            { (typeof(sbyte), typeof(int)), typeof(int) },
            { (typeof(sbyte), typeof(uint)), typeof(uint) },
            { (typeof(sbyte), typeof(long)), typeof(long) },
            { (typeof(sbyte), typeof(float)), typeof(float) },
            { (typeof(sbyte), typeof(double)), typeof(double) },
            { (typeof(sbyte), typeof(decimal)), typeof(decimal) },

            { (typeof(short), typeof(byte)), typeof(int) },
            { (typeof(short), typeof(sbyte)), typeof(int) },
            { (typeof(short), typeof(short)), typeof(short) },
            { (typeof(short), typeof(ushort)), typeof(int) },
            { (typeof(short), typeof(int)), typeof(int) },
            { (typeof(short), typeof(uint)), typeof(uint) },
            { (typeof(short), typeof(long)), typeof(long) },
            { (typeof(short), typeof(float)), typeof(float) },
            { (typeof(short), typeof(double)), typeof(double) },
            { (typeof(short), typeof(decimal)), typeof(decimal) },

            { (typeof(ushort), typeof(byte)), typeof(int) },
            { (typeof(ushort), typeof(sbyte)), typeof(int) },
            { (typeof(ushort), typeof(short)), typeof(int) },
            { (typeof(ushort), typeof(ushort)), typeof(ushort) },
            { (typeof(ushort), typeof(int)), typeof(int) },
            { (typeof(ushort), typeof(uint)), typeof(uint) },
            { (typeof(ushort), typeof(long)), typeof(long) },
            { (typeof(ushort), typeof(ulong)), typeof(ulong) },
            { (typeof(ushort), typeof(float)), typeof(float) },
            { (typeof(ushort), typeof(double)), typeof(double) },
            { (typeof(ushort), typeof(decimal)), typeof(decimal) },

            { (typeof(int), typeof(byte)), typeof(int) },
            { (typeof(int), typeof(sbyte)), typeof(int) },
            { (typeof(int), typeof(short)), typeof(int) },
            { (typeof(int), typeof(ushort)), typeof(int) },
            { (typeof(int), typeof(int)), typeof(int) },
            { (typeof(int), typeof(uint)), typeof(uint) },
            { (typeof(int), typeof(long)), typeof(long) },
            { (typeof(int), typeof(float)), typeof(float) },
            { (typeof(int), typeof(double)), typeof(double) },
            { (typeof(int), typeof(decimal)), typeof(decimal) },

            { (typeof(uint), typeof(byte)), typeof(uint) },
            { (typeof(uint), typeof(sbyte)), typeof(uint) },
            { (typeof(uint), typeof(short)), typeof(uint) },
            { (typeof(uint), typeof(ushort)), typeof(uint) },
            { (typeof(uint), typeof(int)), typeof(uint) },
            { (typeof(uint), typeof(uint)), typeof(uint) },
            { (typeof(uint), typeof(long)), typeof(ulong) },
            { (typeof(uint), typeof(ulong)), typeof(ulong) },
            { (typeof(uint), typeof(float)), typeof(float) },
            { (typeof(uint), typeof(double)), typeof(double) },
            { (typeof(uint), typeof(decimal)), typeof(decimal) },

            { (typeof(long), typeof(byte)), typeof(long) },
            { (typeof(long), typeof(sbyte)), typeof(long) },
            { (typeof(long), typeof(short)), typeof(long) },
            { (typeof(long), typeof(ushort)), typeof(long) },
            { (typeof(long), typeof(int)), typeof(long) },
            { (typeof(long), typeof(uint)), typeof(ulong) },
            { (typeof(long), typeof(long)), typeof(long) },
            { (typeof(long), typeof(float)), typeof(float) },
            { (typeof(long), typeof(double)), typeof(double) },
            { (typeof(long), typeof(decimal)), typeof(decimal) },

            { (typeof(ulong), typeof(byte)), typeof(ulong) },
            { (typeof(ulong), typeof(ushort)), typeof(ulong) },
            { (typeof(ulong), typeof(uint)), typeof(ulong) },
            { (typeof(ulong), typeof(ulong)), typeof(ulong) },
            { (typeof(ulong), typeof(float)), typeof(float) },
            { (typeof(ulong), typeof(double)), typeof(double) },
            { (typeof(ulong), typeof(decimal)), typeof(decimal) },

            { (typeof(float), typeof(byte)), typeof(float) },
            { (typeof(float), typeof(sbyte)), typeof(float) },
            { (typeof(float), typeof(short)), typeof(float) },
            { (typeof(float), typeof(ushort)), typeof(float) },
            { (typeof(float), typeof(int)), typeof(float) },
            { (typeof(float), typeof(uint)), typeof(float) },
            { (typeof(float), typeof(long)), typeof(float) },
            { (typeof(float), typeof(ulong)), typeof(float) },
            { (typeof(float), typeof(float)), typeof(float) },
            { (typeof(float), typeof(double)), typeof(double) },

            { (typeof(double), typeof(byte)), typeof(double) },
            { (typeof(double), typeof(sbyte)), typeof(double) },
            { (typeof(double), typeof(short)), typeof(double) },
            { (typeof(double), typeof(ushort)), typeof(double) },
            { (typeof(double), typeof(int)), typeof(double) },
            { (typeof(double), typeof(uint)), typeof(double) },
            { (typeof(double), typeof(long)), typeof(double) },
            { (typeof(double), typeof(ulong)), typeof(double) },
            { (typeof(double), typeof(float)), typeof(double) },
            { (typeof(double), typeof(double)), typeof(double) },

            { (typeof(decimal), typeof(byte)), typeof(decimal) },
            { (typeof(decimal), typeof(sbyte)), typeof(decimal) },
            { (typeof(decimal), typeof(short)), typeof(decimal) },
            { (typeof(decimal), typeof(ushort)), typeof(decimal) },
            { (typeof(decimal), typeof(int)), typeof(decimal) },
            { (typeof(decimal), typeof(uint)), typeof(decimal) },
            { (typeof(decimal), typeof(long)), typeof(decimal) },
            { (typeof(decimal), typeof(ulong)), typeof(decimal) },
            { (typeof(decimal), typeof(decimal)), typeof(decimal) },

            { (typeof(object), typeof(string)), typeof(object) },
            { (typeof(string), typeof(object)), typeof(object) },

            { (typeof(string), typeof(string)), typeof(string) },
            { (typeof(bool), typeof(bool)), typeof(bool) },

            { (typeof(DateTimeOffset), typeof(DateTimeOffset)), typeof(TimeSpan) },
            { (typeof(DateTimeOffset), typeof(TimeSpan)), typeof(DateTimeOffset) },
            { (typeof(DateTime), typeof(DateTime)), typeof(TimeSpan) },
            { (typeof(DateTime), typeof(TimeSpan)), typeof(DateTime) },
            { (typeof(TimeSpan), typeof(TimeSpan)), typeof(TimeSpan) }
        };

        BinaryTypes = binaryTypes;
    }

    private static IReadOnlyDictionary<(Type, Type), Type> BinaryTypes { get; }

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