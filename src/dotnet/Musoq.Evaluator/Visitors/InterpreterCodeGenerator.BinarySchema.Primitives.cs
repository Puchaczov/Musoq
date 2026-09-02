using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GeneratePrimitiveReadCode(string localVar, PrimitiveTypeNode primitiveType)
    {
        var readMethod = GetPrimitiveReadMethod(primitiveType);
        return $"var {localVar} = {readMethod}(data);";
    }

    private static string GetPrimitiveReadMethod(PrimitiveTypeNode primitiveType)
    {
        return primitiveType.TypeName switch
        {
            PrimitiveTypeName.Byte => "ReadByte",
            PrimitiveTypeName.SByte => "ReadSByte",
            PrimitiveTypeName.Short => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadInt16Le"
                : "ReadInt16Be",
            PrimitiveTypeName.UShort => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadUInt16Le"
                : "ReadUInt16Be",
            PrimitiveTypeName.Int => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadInt32Le"
                : "ReadInt32Be",
            PrimitiveTypeName.UInt => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadUInt32Le"
                : "ReadUInt32Be",
            PrimitiveTypeName.Long => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadInt64Le"
                : "ReadInt64Be",
            PrimitiveTypeName.ULong => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadUInt64Le"
                : "ReadUInt64Be",
            PrimitiveTypeName.Float => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadSingleLe"
                : "ReadSingleBe",
            PrimitiveTypeName.Double => primitiveType.Endianness == Endianness.LittleEndian
                ? "ReadDoubleLe"
                : "ReadDoubleBe",
            _ => throw new InvalidOperationException($"Unknown primitive type: {primitiveType.TypeName}")
        };
    }

    private string GenerateByteArrayReadCode(string localVar, ByteArrayTypeNode byteArrayType)
    {
        var sizeExpr = GenerateSizeExpression(byteArrayType.SizeExpression);
        return $"var {localVar} = ReadBytes(data, {sizeExpr});";
    }

    private string GenerateStringReadCode(string localVar, StringTypeNode stringType, string fieldName)
    {
        return GenerateStringDeclarationCode(localVar, stringType, fieldName);
    }

    private string GenerateStringDeclarationCode(string targetVar, StringTypeNode stringType, string fieldName)
    {
        var builder = new StringBuilder();
        var rawStringVar = stringType.AsTextSchemaName != null ? $"{targetVar}_raw" : targetVar;

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {rawStringVar} = {GenerateStringReadExpression(stringType)};");

        if (stringType.AsTextSchemaName == null)
            return builder.ToString();

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {targetVar}_textInterpreter = new {stringType.AsTextSchemaName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"var {targetVar} = ParseNested({targetVar}_textInterpreter, {rawStringVar}, \"{EscapeString(fieldName)}\");");
        return builder.ToString();
    }

    private string GenerateStringAssignmentCode(string targetVar, StringTypeNode stringType, string fieldName)
    {
        var builder = new StringBuilder();
        var rawStringVar = stringType.AsTextSchemaName != null ? $"{targetVar}_raw" : targetVar;

        if (stringType.AsTextSchemaName != null)
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {rawStringVar} = {GenerateStringReadExpression(stringType)};");
        else
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{targetVar} = {GenerateStringReadExpression(stringType)};");

        if (stringType.AsTextSchemaName == null)
            return builder.ToString();

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {targetVar}_textInterpreter = new {stringType.AsTextSchemaName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"{targetVar} = ParseNested({targetVar}_textInterpreter, {rawStringVar}, \"{EscapeString(fieldName)}\");");
        return builder.ToString();
    }

    private string GenerateStringReadExpression(StringTypeNode stringType)
    {
        var sizeExpr = GenerateSizeExpression(stringType.SizeExpression);
        var encodingExpr = GetEncodingExpression(stringType.Encoding);
        var expression = stringType.Modifiers.HasFlag(StringModifier.NullTerm)
            ? $"ReadNullTerminatedString(data, {sizeExpr}, {encodingExpr})"
            : $"ReadString(data, {sizeExpr}, {encodingExpr})";

        if (stringType.Modifiers.HasFlag(StringModifier.Trim))
            return $"{expression}.Trim()";

        if (stringType.Modifiers.HasFlag(StringModifier.LTrim))
            return $"{expression}.TrimStart()";

        if (stringType.Modifiers.HasFlag(StringModifier.RTrim))
            return $"{expression}.TrimEnd()";

        return expression;
    }

    private static string FormatHexLiteral(HexIntegerNode hexNode)
    {
        var value = Convert.ToInt64(hexNode.ObjValue ?? 0L, System.Globalization.CultureInfo.InvariantCulture);

        if (value is > int.MaxValue and <= uint.MaxValue)
            return $"unchecked((int){value}L)";
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GetEncodingExpression(StringEncoding encoding)
    {
        return encoding switch
        {
            StringEncoding.Utf8 => "System.Text.Encoding.UTF8",
            StringEncoding.Utf16Le => "System.Text.Encoding.Unicode",
            StringEncoding.Utf16Be => "System.Text.Encoding.BigEndianUnicode",
            StringEncoding.Ascii => "System.Text.Encoding.ASCII",
            StringEncoding.Latin1 => "System.Text.Encoding.Latin1",
            StringEncoding.Ebcdic => "System.Text.Encoding.GetEncoding(37)",
            _ => "System.Text.Encoding.UTF8"
        };
    }

    private static string GenerateBitsReadCode(string localVar, BitsTypeNode bitsType)
    {
        var bitCount = bitsType.BitCount;
        var castType = GetBitsClrTypeName(bitsType);
        return $"var {localVar} = ({castType})ReadBits(data, {bitCount});";
    }

    private static string GetBitsClrTypeName(BitsTypeNode bitsType)
    {
        return bitsType.BitCount switch
        {
            <= 8 => "byte",
            <= 16 => "ushort",
            <= 32 => "uint",
            _ => "ulong"
        };
    }

}
