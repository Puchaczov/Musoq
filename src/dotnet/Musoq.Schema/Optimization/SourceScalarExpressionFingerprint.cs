using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;

namespace Musoq.Schema.Optimization;

/// <summary>Canonical typed identities, independent of culture and delimiter-bearing values.</summary>
public static class SourceScalarExpressionFingerprint
{
    public static string Compute(SourceScalarExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (!SourceScalarExpressionGraph.IsValid(expression))
            throw new ArgumentException("Malformed or unsupported portable scalar expression.", nameof(expression));
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
            WriteNode(writer, expression);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteNode(Utf8JsonWriter writer, SourceScalarExpression expression)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(expression.ResultType.AssemblyQualifiedName);
        writer.WriteStringValue(expression.Stability.ToString());
        switch (expression)
        {
            case SourceScalarLiteral literal:
                writer.WriteStringValue("literal");
                writer.WriteStringValue(Literal(literal.Value));
                break;
            case SourceScalarColumn column:
                WriteColumn(writer, column);
                break;
            case SourceScalarUnary unary:
                writer.WriteStringValue("unary");
                writer.WriteStringValue(unary.Operator.ToString());
                writer.WriteStringValue(unary.ExpressionStability.ToString());
                WriteNode(writer, unary.Operand);
                break;
            case SourceScalarBinary binary:
                writer.WriteStringValue("binary");
                writer.WriteStringValue(binary.Operator.ToString());
                writer.WriteStringValue(binary.ExpressionStability.ToString());
                if (binary.UsesSqlNullSemantics)
                    writer.WriteStringValue("sql-null-semantics");
                WriteNode(writer, binary.Left);
                WriteNode(writer, binary.Right);
                break;
            case SourceScalarCast cast:
                writer.WriteStringValue("cast");
                writer.WriteStringValue(cast.ExpressionStability.ToString());
                WriteNode(writer, cast.Operand);
                break;
            case SourceScalarNullCheck check:
                writer.WriteStringValue("null");
                writer.WriteBooleanValue(check.Negated);
                writer.WriteStringValue(check.ExpressionStability.ToString());
                WriteNode(writer, check.Operand);
                break;
            case SourceScalarCoalesce coalesce:
                writer.WriteStringValue("coalesce");
                writer.WriteStringValue(coalesce.ExpressionStability.ToString());
                foreach (var child in coalesce.Expressions)
                    WriteNode(writer, child);
                break;
            default:
                throw new ArgumentException("Unsupported portable scalar expression.", nameof(expression));
        }
        writer.WriteEndArray();
    }

    private static void WriteColumn(Utf8JsonWriter writer, SourceScalarColumn column)
    {
        writer.WriteStringValue("column");
        writer.WriteStringValue(column.Column.Name);
        writer.WriteStringValue(column.ColumnStability.ToString());
        writer.WriteStringValue(column.Column.ValueTypeName);
        writer.WriteStartArray();
        foreach (var modifier in column.Column.ReadModifiers.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WriteStringValue(modifier.Key);
            writer.WriteStringValue(modifier.Value);
        }
        writer.WriteEndArray();
    }

    private static string Encode(params string[] values) => JsonSerializer.Serialize(values);

    private static string Literal(object? value) => value switch
    {
        null => "null",
        string text => JsonSerializer.Serialize(text),
        char character => ((int)character).ToString(CultureInfo.InvariantCulture),
        float number => BitConverter.SingleToInt32Bits(number).ToString("X8", CultureInfo.InvariantCulture),
        double number => BitConverter.DoubleToInt64Bits(number).ToString("X16", CultureInfo.InvariantCulture),
        decimal number => string.Join(",", decimal.GetBits(number).Select(part => part.ToString("X8", CultureInfo.InvariantCulture))),
        DateTime date => Encode(date.Ticks.ToString(CultureInfo.InvariantCulture), date.Kind.ToString()),
        DateTimeOffset date => Encode(date.Ticks.ToString(CultureInfo.InvariantCulture), date.Offset.Ticks.ToString(CultureInfo.InvariantCulture)),
        TimeSpan duration => duration.Ticks.ToString(CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? throw new ArgumentException("A scalar literal produced no canonical value.", nameof(value)),
        bool boolean => boolean ? "true" : "false",
        _ => throw new ArgumentException("Unsupported portable scalar literal.", nameof(value))
    };
}
