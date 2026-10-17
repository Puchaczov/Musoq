using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateInlineSchemaNestedClass(string className, InlineSchemaTypeNode inlineSchema)
    {
        var builder = new StringBuilder();
        var outerRefs = CollectOuterFieldReferences(inlineSchema);

        builder.AppendLine("/// <summary>");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"/// Generated nested interpreter for inline schema '{className}'.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"public sealed class {className} : BytesInterpreterBase<{className}>, global::Musoq.Plugins.IJsonPayload");
        builder.AppendLine("{");
        builder.Append(PartialPayloadCodeEmitter.Members(className));

        var fields = inlineSchema.Fields;
        _expressionFields = fields.Concat(outerRefs.Select(name => (SchemaFieldNode)new FieldDefinitionNode(name,
            new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian)))).ToArray();
        builder.Append(GenerateParentContextMembers(className));
        builder.Append(PartialPayloadCodeEmitter.JsonMembers(fields
            .Where(static field => field.Name != "_" && field is not FieldDefinitionNode { TypeAnnotation: AlignmentNode })
            .Select(field => (field.Name, EscapeCSharpIdentifier(field.Name)))));
        foreach (var field in fields)
        {
            if (field.Name == "_") continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode }) continue;

            var clrTypeName = GetClrTypeNameForFieldInline(field);
            var isConditional = field.IsConditional ||
                                (field is ComputedFieldNode inlineComputed &&
                                 BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(inlineComputed.Expression, fields, _registry)));
            var isTypeParam = IsTypeParameter(clrTypeName);

            var propertyTypeName = isConditional && !IsReferenceType(clrTypeName) && !isTypeParam
                ? $"{clrTypeName}?"
                : clrTypeName;

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} field value.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {propertyTypeName} {EscapeCSharpIdentifier(field.Name)} {{ get; set; }}");
            builder.AppendLine();
        }

        foreach (var outerRef in outerRefs)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Outer scope reference for '{outerRef}'.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    internal int Outer{EscapeCSharpIdentifier(outerRef)} {{ get; set; }}");
            builder.AppendLine();
        }

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override string SchemaName => \"{className}\";");
        builder.AppendLine();

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override {className} InterpretAt(ReadOnlySpan<byte> data, int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        InitializeParsePosition(data, offset);");
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        var __parsedValue = new {className}();");
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        foreach (var outerRef in outerRefs)
        {
            var localVar = GetLocalVarName(outerRef);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        var {localVar} = Outer{EscapeCSharpIdentifier(outerRef)};");
        }

        if (outerRefs.Count > 0)
            builder.AppendLine();

        foreach (var field in fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{EscapeString(field.Name)}\");");
            var readCode = field switch
            {
                FieldDefinitionNode parsed => GenerateFieldReadCodeWithModifiers(parsed),
                ComputedFieldNode computed => GenerateComputedFieldCode(computed, fields.ToList()),
                _ => throw new InvalidOperationException("Unsupported inline binary field.")
            };
            builder.Append(Indent(readCode, 2));

            if (field.Name != "_" && field is not FieldDefinitionNode { TypeAnnotation: AlignmentNode })
            {
                AppendGeneratedLine(builder, $"        __parsedValue.{EscapeCSharpIdentifier(field.Name)} = {GetLocalVarName(field.Name)};");
                AppendGeneratedLine(builder, $"        RecordParsedField(\"{EscapeString(field.Name)}\");");
            }
        }

        builder.AppendLine("        return __parsedValue;");
        builder.AppendLine("    }");

        builder.AppendLine("}");

        return builder.ToString();
    }

    private string GetClrTypeNameForFieldInline(SchemaFieldNode field)
    {
        return field switch
        {
            FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode nestedInline } parsedField => GetOrRegisterInlineSchemaClassName(parsedField.Name, nestedInline, null),
            FieldDefinitionNode { TypeAnnotation: ArrayTypeNode
                {
                    ElementType: InlineSchemaTypeNode
                } arrayType
            } parsedField => GetArrayClrTypeName(parsedField.Name, arrayType),
            FieldDefinitionNode { TypeAnnotation: RepeatUntilTypeNode
                {
                    ElementType: InlineSchemaTypeNode inlineSchema
                }
            } parsedField => GetRepeatUntilClrTypeName(parsedField.Name, inlineSchema),
            FieldDefinitionNode parsedField => GetClrTypeName(parsedField.TypeAnnotation),
            ComputedFieldNode computedField => InferComputedFieldTypeName(computedField.Expression, _expressionFields.ToList()),
            _ => "object"
        };
    }

    private string GenerateInlineSchemaReadCode(string localVar, string fieldName, InlineSchemaTypeNode inlineSchema)
    {
        var inlineClassName = GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null);
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempInterpreter} = {CreateNestedInterpreter(inlineClassName)};");
        builder.Append(GenerateOuterRefAssignments(tempInterpreter, inlineSchema));
        AppendGeneratedLine(builder, $"var {localVar} = InterpretNested({tempInterpreter}, data, \"{EscapeString(fieldName)}\");");
        return builder.ToString();
    }

    private string GetOrRegisterInlineSchemaClassName(string fieldName, InlineSchemaTypeNode inlineSchema,
        string? parentPrefix)
    {
        var className = $"Inline_{fieldName}";

        var existing = _inlineSchemas.FirstOrDefault(x => x.ClassName == className);
        if (existing.Schema == null) _inlineSchemas.Add((className, inlineSchema, parentPrefix));

        return className;
    }

    private static HashSet<string> CollectOuterFieldReferences(InlineSchemaTypeNode inlineSchema)
    {
        var inlineFieldNames = new HashSet<string>(
            inlineSchema.Fields.Select(f => f.Name),
            StringComparer.OrdinalIgnoreCase);

        var outerRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in inlineSchema.Fields)
        {
            if (field is not FieldDefinitionNode parsedField) continue;
            CollectFieldRefsFromTypeAnnotation(parsedField.TypeAnnotation, inlineFieldNames, outerRefs);

            if (parsedField.WhenCondition != null)
                CollectFieldRefsFromExpression(parsedField.WhenCondition, inlineFieldNames, outerRefs);
        }

        return outerRefs;
    }

    private static void CollectFieldRefsFromTypeAnnotation(TypeAnnotationNode type, HashSet<string> localNames,
        HashSet<string> outerRefs)
    {
        switch (type)
        {
            case StringTypeNode stringType:
                CollectFieldRefsFromExpression(stringType.SizeExpression, localNames, outerRefs);
                break;
            case ByteArrayTypeNode byteArrayType:
                CollectFieldRefsFromExpression(byteArrayType.SizeExpression, localNames, outerRefs);
                break;
            case ArrayTypeNode arrayType:
                CollectFieldRefsFromExpression(arrayType.SizeExpression, localNames, outerRefs);
                break;
        }
    }

    private static void CollectFieldRefsFromExpression(Node expr, HashSet<string> localNames, HashSet<string> outerRefs)
    {
        switch (expr)
        {
            case AccessColumnNode access:
                if (!localNames.Contains(access.Name))
                    outerRefs.Add(access.Name);
                break;
            case IdentifierNode ident:
                if (!localNames.Contains(ident.Name))
                    outerRefs.Add(ident.Name);
                break;
            case BinaryNode binary:
                CollectFieldRefsFromExpression(binary.Left, localNames, outerRefs);
                CollectFieldRefsFromExpression(binary.Right, localNames, outerRefs);
                break;
        }
    }

    private string GenerateOuterRefAssignments(string interpreterVar, InlineSchemaTypeNode inlineSchema)
    {
        var outerRefs = CollectOuterFieldReferences(inlineSchema);
        if (outerRefs.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var outerRef in outerRefs)
        {
            var localVar = GetLocalVarName(outerRef);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"{interpreterVar}.Outer{EscapeCSharpIdentifier(outerRef)} = Convert.ToInt32({localVar});");
        }

        return builder.ToString();
    }
}
