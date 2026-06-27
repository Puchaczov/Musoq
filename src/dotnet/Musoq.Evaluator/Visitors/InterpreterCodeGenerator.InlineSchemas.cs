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
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"public sealed class {className} : BytesInterpreterBase<{className}>");
        builder.AppendLine("{");

        var fields = inlineSchema.Fields;
        foreach (var field in fields)
        {
            if (field.Name == "_") continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode }) continue;

            var clrTypeName = GetClrTypeNameForFieldInline(field);
            var isConditional = field.IsConditional ||
                                (field is ComputedFieldNode inlineComputed &&
                                 ReferencesConditionalField(inlineComputed.Expression, fields));
            var isTypeParam = IsTypeParameter(clrTypeName);

            var propertyTypeName = isConditional && !IsReferenceType(clrTypeName) && !isTypeParam
                ? $"{clrTypeName}?"
                : clrTypeName;

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} field value.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {propertyTypeName} {EscapeCSharpIdentifier(field.Name)} {{ get; init; }}");
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
        builder.AppendLine("        ParsePosition = offset;");
        builder.AppendLine("        BitOffset = 0;");
        builder.AppendLine();

        foreach (var outerRef in outerRefs)
        {
            var localVar = GetLocalVarName(outerRef);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        var {localVar} = Outer{EscapeCSharpIdentifier(outerRef)};");
        }

        if (outerRefs.Count > 0)
            builder.AppendLine();

        var fieldInitializers = new List<string>();
        foreach (var field in fields)
        {
            if (field is not FieldDefinitionNode parsedField) continue;

            var readCode = GenerateFieldReadCodeWithModifiers(parsedField);
            builder.Append(Indent(readCode, 2));

            if (parsedField.Name != "_" && parsedField.TypeAnnotation is not AlignmentNode)
                fieldInitializers.Add(
                    $"{EscapeCSharpIdentifier(parsedField.Name)} = {GetLocalVarName(parsedField.Name)}");
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        return new {className}");
        builder.AppendLine("        {");
        for (var i = 0; i < fieldInitializers.Count; i++)
        {
            var comma = i < fieldInitializers.Count - 1 ? "," : "";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"            {fieldInitializers[i]}{comma}");
        }

        builder.AppendLine("        };");
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
            ComputedFieldNode computedField => InferComputedFieldTypeNameStatic(computedField.Expression),
            _ => "object"
        };
    }

    private string GenerateInlineSchemaReadCode(string localVar, string fieldName, InlineSchemaTypeNode inlineSchema)
    {
        var inlineClassName = GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null);
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempInterpreter} = new {inlineClassName}();");
        builder.Append(GenerateOuterRefAssignments(tempInterpreter, inlineSchema));
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempInterpreter}.InterpretAt(data, ParsePosition);");
        builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"ParsePosition = {tempInterpreter}.BytesConsumed;");
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
