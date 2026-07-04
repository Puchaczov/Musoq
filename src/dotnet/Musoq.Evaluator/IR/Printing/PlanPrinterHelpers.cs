using System.Text;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;
using PropertyNameAndTypePair = Musoq.Parser.Nodes.From.PropertyFromNode.PropertyNameAndTypePair;
using WindowFrameNode = Musoq.Parser.Nodes.WindowFrameNode;
using WindowFrameType = Musoq.Parser.Nodes.WindowFrameType;

namespace Musoq.Evaluator.IR.Printing;

internal static class PlanPrinterHelpers
{
    public static string Indent(int indent)
    {
        return new string(' ', indent);
    }

    public static void AppendProjectedFields(StringBuilder sb, ProjectedField[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(fields[i].Expression));
            sb.Append(" as ");
            sb.Append(fields[i].OutputName);
        }
    }

    public static void AppendNames(StringBuilder sb, string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(names[i]);
        }
    }

    public static string FormatSchemaName(string schemaName)
    {
        if (schemaName.Length > 0 && schemaName[0] == '#')
            return schemaName;

        return $"#{schemaName}";
    }

    public static void AppendAggregateBindings(StringBuilder sb, AggregateBinding[] bindings)
    {
        for (var i = 0; i < bindings.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatAggregateSummary(bindings[i].ColumnName));
        }
    }

    private static string FormatAggregateSummary(string displayName)
    {
        var builder = new StringBuilder(displayName.Length);
        var inString = false;

        for (var index = 0; index < displayName.Length; index++)
        {
            var current = displayName[index];
            if (current == '\'')
                inString = !inString;

            if (!inString && IsIdentifierStart(current))
            {
                var start = index;
                index++;
                while (index < displayName.Length && IsIdentifierPart(displayName[index]))
                    index++;

                if (index < displayName.Length && displayName[index] == '.')
                    continue;

                builder.Append(displayName, start, index - start);
                index--;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    public static void AppendOrderFields(StringBuilder sb, OrderField[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(keys[i].Expression));
            if (keys[i].Descending) sb.Append(" DESC");
            AppendNullOrdering(sb, keys[i].NullOrdering);
        }
    }

    private static void AppendNullOrdering(StringBuilder sb, NullOrdering nullOrdering)
    {
        if (nullOrdering == NullOrdering.First)
            sb.Append(" NULLS FIRST");
        else if (nullOrdering == NullOrdering.Last)
            sb.Append(" NULLS LAST");
    }

    public static void AppendExpressions(StringBuilder sb, IrExpression[] expressions)
    {
        for (var i = 0; i < expressions.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(expressions[i]));
        }
    }

    public static void AppendWindowRegistrations(StringBuilder sb, WindowRegistration[] registrations)
    {
        for (var i = 0; i < registrations.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            AppendWindowRegistration(sb, registrations[i]);
        }
    }

    public static void AppendWindowRegistration(StringBuilder sb, WindowRegistration registration)
    {
        sb.Append(registration.FunctionName);
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"(idx:{registration.WindowIndex}");

        AppendWindowExpressions(sb, "partition", registration.PartitionKeys);
        AppendWindowOrderFields(sb, registration.OrderKeys);
        AppendWindowExpressions(sb, "args", registration.ValueArguments);
        if (registration.FilterPredicate is not null)
            sb.Append("; filter: ").Append(IrExpressionPrinter.Print(registration.FilterPredicate));

        if (registration.Frame is not null)
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"; frame: {GetWindowFrameText(registration.Frame)}");

        sb.Append(')');
    }

    public static void AppendWindowExpressions(StringBuilder sb, string label, IrExpression[] expressions)
    {
        if (expressions.Length == 0)
            return;

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"; {label}: ");
        AppendExpressions(sb, expressions);
    }

    public static void AppendWindowOrderFields(StringBuilder sb, OrderField[] keys)
    {
        if (keys.Length == 0)
            return;

        sb.Append("; order: ");
        AppendOrderFields(sb, keys);
    }

    public static void AppendProperties(StringBuilder sb, PropertyNameAndTypePair[] properties)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0) sb.Append('.');
            sb.Append(properties[i].PropertyName);
        }
    }

    public static string GetWindowFrameText(WindowFrameNode frame)
    {
        var frameKind = frame.FrameType == WindowFrameType.Rows ? "rows" : "range";

        return $"{frameKind} between {frame.Start.ToString()} and {frame.End.ToString()}";
    }
}
