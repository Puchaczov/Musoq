using System;
using System.Globalization;

namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Scalar<T>(string kind, T value) where T : IFormattable
    {
        if (structure == null)
            return;
        var text = value.ToString(null, CultureInfo.InvariantCulture);
        structure.Append(kind).Append(':').Append(text.Length).Append(':').Append(text).Append(';');
    }

    private void Write(string? value)
    {
        if (value == null)
        {
            Null();
            return;
        }
        structure?.Append("string:").Append(value.Length).Append(':').Append(value).Append(';');
    }

    private void Write(bool value) => structure?.Append(value ? "bool:true;" : "bool:false;");
    private void Write(byte value) => Scalar("byte", value);
    private void Write(sbyte value) => Scalar("sbyte", value);
    private void Write(short value) => Scalar("short", value);
    private void Write(ushort value) => Scalar("ushort", value);
    private void Write(int value) => Scalar("int", value);
    private void Write(uint value) => Scalar("uint", value);
    private void Write(long value) => Scalar("long", value);
    private void Write(ulong value) => Scalar("ulong", value);
    private void Write(float value) => Scalar("float", value);
    private void Write(double value) => Scalar("double", value);
    private void Write(decimal value) => Scalar("decimal", value);

    private void Write(object? value)
    {
        switch (value)
        {
            case null: Null(); break;
            case string item: Write(item); break;
            case bool item: Write(item); break;
            case byte item: Write(item); break;
            case sbyte item: Write(item); break;
            case short item: Write(item); break;
            case ushort item: Write(item); break;
            case int item: Write(item); break;
            case uint item: Write(item); break;
            case long item: Write(item); break;
            case ulong item: Write(item); break;
            case float item: Write(item); break;
            case double item: Write(item); break;
            case decimal item: Write(item); break;
            default: throw new InvalidOperationException("Unreviewed scalar value shape in source AST.");
        }
    }
}
