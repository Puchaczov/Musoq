namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaClrTypeNames
{
    internal static string Map(Type type)
    {
        if (type.IsArray)
            return Map(type.GetElementType()!) + "[]";
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => "byte", TypeCode.SByte => "sbyte",
            TypeCode.Int16 => "short", TypeCode.UInt16 => "ushort",
            TypeCode.Int32 => "int", TypeCode.UInt32 => "uint",
            TypeCode.Int64 => "long", TypeCode.UInt64 => "ulong",
            TypeCode.Single => "float", TypeCode.Double => "double", TypeCode.Decimal => "decimal",
            TypeCode.Boolean => "bool", TypeCode.String => "string", _ => "object"
        };
    }
}
