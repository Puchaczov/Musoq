using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static void AppendShapes(StringBuilder builder, IReadOnlyList<RowShape> shapes)
    {
        builder.AppendLine("  Shapes");

        foreach (var shape in shapes)
            AppendShape(builder, shape, 4);
    }

    private static void AppendShape(StringBuilder builder, RowShape shape, int indentation)
    {
        var prefix = new string(' ', indentation);

        switch (shape)
        {
            case SourceEntityShape source:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SourceEntity [{source.Alias}: {FormatType(source.EntityType)}]");
                break;
            case GeneratedRowShape generated:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}Generated [{generated.TypeName}]");
                break;
            case GeneratedRecordShape generated:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}GeneratedRecord [{generated.TypeName}]");
                break;
            case HashPayloadShape hashPayload:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}HashPayload [{hashPayload.TypeName}]");
                break;
            case AggregateGroupShape aggregateGroup:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AggregateGroup [{aggregateGroup.TypeName}; keys: {aggregateGroup.Keys.Count.ToString(CultureInfo.InvariantCulture)}; typed aggs: {aggregateGroup.Accumulators.Count.ToString(CultureInfo.InvariantCulture)}]");
                break;
            case TableRowShape tableRow:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TableRow [{tableRow.Alias}]");
                break;
            case ExpandoAdapterShape expando:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ExpandoAdapter [{expando.Alias}: {expando.TypeName}]");
                break;
            default:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}UnknownShape [{shape.GetType().Name}]");
                break;
        }

        foreach (var field in shape.Fields)
            builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}  {field.Name}: {FormatType(field.Type)} <- {FormatAccess(field.AccessStrategy)}");
    }

    private static string FormatAccess(FieldAccessStrategy strategy)
    {
        return strategy switch
        {
            ClrPropertyAccess property => $"property {property.PropertyName}",
            DirectScalarValueAccess => "direct scalar value",
            ApplyOrdinalityAccess ordinality => $"apply ordinality {ordinality.VariableName}",
            GeneratedFieldAccess field => $"field {field.FieldName}",
            GeneratedRowContextAccess generatedContext => $"generated row context {generatedContext.TypeName}[{generatedContext.Index.ToString(CultureInfo.InvariantCulture)}]",
            GeneratedRowTypeAccess generatedRow => $"generated row {generatedRow.TypeName}.{generatedRow.FieldName}",
            GeneratedRowNestedAccess generatedNested => $"generated row {generatedNested.TypeName}.{generatedNested.FieldName}.{generatedNested.PropertyPath}",
            ExpandoDictionaryAccess expando => $"expando key \"{expando.Key}\"",
            PositionalAccess positional => $"position {positional.Index}",
            ContextAccess context => $"context {context.Index}",
            ReflectedMemberAccess reflected => $"reflected member {reflected.PropertyPath}",
            NestedClrPropertyAccess nestedProperty => $"nested property {nestedProperty.PropertyPath}",
            NestedPositionalAccess nestedPositional => $"position {nestedPositional.Index}.{nestedPositional.PropertyPath}",
            RuntimeDynamicMemberAccess runtimeDynamic => $"runtime dynamic member \"{runtimeDynamic.MemberName}\"",
            RuntimeDynamicMemberPathAccess runtimePath =>
                $"runtime dynamic path {runtimePath.RootFieldName}.{string.Join('.', runtimePath.Segments.Select(segment => segment.MemberName))}",
            _ => strategy.GetType().Name
        };
    }
}
