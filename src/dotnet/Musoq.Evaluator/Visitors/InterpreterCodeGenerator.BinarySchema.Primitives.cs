using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GeneratePrimitiveReadCode(string localVar, PrimitiveTypeNode primitiveType)
    {
        var bytes = $"base.TakePreparedBytes(data, {primitiveType.FixedSizeBytes})";
        return StatusCheck($"base.PrepareByteInputStatus(data, {primitiveType.FixedSizeBytes})") +
               $"var {localVar} = {ReadPreparedPrimitive(primitiveType, bytes)};";
    }

    private static string ReadPreparedPrimitive(PrimitiveTypeNode primitiveType, string bytes)
    {
        if (primitiveType.TypeName == PrimitiveTypeName.Byte)
            return $"{bytes}[0]";
        if (primitiveType.TypeName == PrimitiveTypeName.SByte)
            return $"unchecked((sbyte){bytes}[0])";

        var number = primitiveType.TypeName switch
        {
            PrimitiveTypeName.Short => "Int16",
            PrimitiveTypeName.UShort => "UInt16",
            PrimitiveTypeName.Int => "Int32",
            PrimitiveTypeName.UInt => "UInt32",
            PrimitiveTypeName.Long => "Int64",
            PrimitiveTypeName.ULong => "UInt64",
            PrimitiveTypeName.Float => "Single",
            PrimitiveTypeName.Double => "Double",
            _ => throw new InvalidOperationException($"Unknown primitive type: {primitiveType.TypeName}")
        };
        var order = primitiveType.Endianness == Endianness.LittleEndian ? "LittleEndian" : "BigEndian";
        return $"global::System.Buffers.Binary.BinaryPrimitives.Read{number}{order}({bytes})";
    }

    private string GenerateByteArrayReadCode(string localVar, ByteArrayTypeNode byteArrayType)
    {
        var size = NextStatusName("__musoqReadSize");
        return StatusSizeDeclaration(size, byteArrayType.SizeExpression) + StatusCheck($"base.PrepareByteArrayStatus(data, {size})") +
               $"var {localVar} = base.ReadPreparedByteArray(data, {size});";
    }

    private string GenerateStringReadCode(string localVar, StringTypeNode stringType, string fieldName)
    {
        return GenerateStringDeclarationCode(localVar, stringType, fieldName);
    }

    private string GenerateStringDeclarationCode(string targetVar, StringTypeNode stringType, string fieldName)
    {
        var builder = new StringBuilder();
        var rawStringVar = stringType.AsTextSchemaName != null ? $"{targetVar}_raw" : targetVar;

        var size = NextStatusName("__musoqReadSize");
        var encoding = NextStatusName("__musoqReadEncoding");
        builder.Append(StatusSizeDeclaration(size, stringType.SizeExpression));
        builder.AppendLine($"var {encoding} = {GetEncodingExpression(stringType.Encoding)};");
        var preparation = stringType.Modifiers.HasFlag(StringModifier.NullTerm)
            ? "PrepareNullTerminatedStringStatus" : "PrepareStringStatus";
        builder.Append(StatusCheck($"base.{preparation}(data, {size})"));
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {rawStringVar} = {GenerateStringReadExpression(stringType, size, encoding)};");

        if (stringType.AsTextSchemaName == null)
            return builder.ToString();

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {targetVar}_textInterpreter = new {stringType.AsTextSchemaName}();");
        builder.Append(StatusRead(targetVar,
            $"base.ParseNestedStatus({targetVar}_textInterpreter, {rawStringVar}, \"{EscapeString(fieldName)}\")"));
        return builder.ToString();
    }

    private static string GenerateStringReadExpression(StringTypeNode stringType, string sizeExpr, string encodingExpr)
    {
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
            StringEncoding.Ebcdic => "System.Text.CodePagesEncodingProvider.Instance.GetEncoding(37, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback) ?? throw new System.NotSupportedException(\"IBM EBCDIC code page 037 is unavailable.\")",
            _ => "System.Text.Encoding.UTF8"
        };
    }

    private string GenerateBitsReadCode(string localVar, BitsTypeNode bitsType)
    {
        var bitCount = bitsType.BitCount;
        var castType = GetBitsClrTypeName(bitsType);
        return StatusCheck($"base.PrepareBitsStatus(data, {bitCount})") +
               $"var {localVar} = ({castType})ReadBits(data, {bitCount});";
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
