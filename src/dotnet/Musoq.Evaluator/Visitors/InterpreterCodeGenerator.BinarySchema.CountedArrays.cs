using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateArrayReadCode(string localVar, string fieldName, ArrayTypeNode arrayType)
    {
        var count = $"_{localVar}_count";
        var index = $"_{localVar}_i";
        var item = $"_{localVar}_elem";
        var list = $"_{localVar}_list";
        var array = $"_{localVar}_array";
        var type = GetBinaryValueClrTypeName(fieldName, arrayType.ElementType);
        var width = CountedElementWidth(arrayType.ElementType);
        var builder = new StringBuilder(StatusSizeDeclaration(count, arrayType.SizeExpression));
        var empty = $"System.Array.Empty<{type}>()";
        var listType = $"System.Collections.Generic.List<{type}>";
        if (width > 0)
        {
            // Dividing the available input avoids overflowing count * width on malformed data.
            AppendGeneratedLine(
                builder,
                $"var {array} = {count} == 0 ? {empty} : base.BitOffset == 0 && {count} <= (data.Length - base.ParsePosition) / {width} ? new {type}[{count}] : null;"
            );
            AppendGeneratedLine(builder, $"var {list} = {array} is null ? new {listType}() : null;");
        }
        else
        {
            AppendGeneratedLine(builder, $"var {list} = {count} == 0 ? null : new {listType}();");
        }

        AppendGeneratedLine(builder, $"for (int {index} = 0; {index} < {count}; {index}++)");
        builder.AppendLine("{");
        builder.Append(Indent(GenerateBinaryValueRead(item, fieldName, arrayType.ElementType), 1));
        if (width > 0)
            AppendGeneratedLine(
                builder,
                $"    if ({array} is not null) {array}[{index}] = {item}; else {list}!.Add({item});"
            );
        else
            AppendGeneratedLine(builder, $"    {list}!.Add({item});");
        builder.AppendLine("}");
        var result = width > 0 ? $"{array} ?? {list}!.ToArray()" : $"{list}?.ToArray() ?? {empty}";
        AppendGeneratedLine(builder, $"var {localVar} = {result};");
        return builder.ToString();
    }

    private int CountedElementWidth(TypeAnnotationNode element)
    {
        if (element is PrimitiveTypeNode primitive)
            return primitive.FixedSizeBytes.GetValueOrDefault();

        IReadOnlyList<SchemaFieldNode>? fields = element switch
        {
            InlineSchemaTypeNode inline => inline.Fields,
            SchemaReferenceTypeNode { IsGenericInstantiation: false } reference
                when !IsTypeParameter(reference.SchemaName)
                    && _registry.TryGetSchema(reference.SchemaName, out var registration)
                    && registration?.Node is BinarySchemaNode { IsGeneric: false, Extends: null } record =>
                record.Fields,
            _ => null,
        };
        if (fields is null)
            return 0;

        long bytes = 0;
        foreach (var field in fields)
        {
            if (
                field
                is not FieldDefinitionNode
                {
                    AtOffset: null,
                    WhenCondition: null,
                    Constraint: null,
                    ValueValidation: null,
                    TypeAnnotation: PrimitiveTypeNode part
                }
            )
                return 0;
            bytes += part.FixedSizeBytes.GetValueOrDefault();
        }

        return bytes <= int.MaxValue ? (int)bytes : 0;
    }
}
