using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateInlineSchemaNestedClass(string className, InlineSchemaTypeNode inlineSchema)
    {
        var builder = new StringBuilder();

        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"/// Generated nested interpreter for inline schema '{className}'."
        );
        builder.AppendLine("/// </summary>");
        var fields = inlineSchema.Fields;
        _expressionFields = fields;
        var properties = new List<PayloadProperty>();
        foreach (var field in fields)
        {
            if (field.Name == "_")
                continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                continue;

            var clrTypeName = GetClrTypeNameForFieldInline(field);
            var isConditional =
                field.IsConditional
                || (
                    field is ComputedFieldNode inlineComputed
                    && BinarySchemaExpressionTypes.IsNullable(
                        BinarySchemaExpressionTypes.Resolve(inlineComputed.Expression, fields, _registry)
                    )
                );
            var isTypeParam = IsTypeParameter(clrTypeName);

            var propertyTypeName =
                isConditional && !IsReferenceType(clrTypeName) && !isTypeParam ? $"{clrTypeName}?" : clrTypeName;

            properties.Add(new PayloadProperty(field.Name, propertyTypeName));
        }
        var declaration = new InterpreterDeclaration(className, "BytesInterpreterBase", "IBytesInterpreterStatus");
        var implementation = OpenInterpreterClass(builder, declaration, properties);
        builder.Append(GenerateParentContextMembers(className, implementation));

        BeginStatusMethod(className);
        builder.Append(InterpreterStatusCodeEmitter.BeginBinary(className));
        builder.Append(StatusCheck("base.InitializeParsePositionStatus(data, offset)"));
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"        __parsedValue = new {className}();"
        );
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        var completionIndex = 0;
        foreach (var field in fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{EscapeString(field.Name)}\");");
            var readCode = field switch
            {
                FieldDefinitionNode parsed => GenerateFieldReadCodeWithModifiers(parsed),
                ComputedFieldNode computed => GenerateComputedFieldCode(computed, fields.ToList()),
                _ => throw new InvalidOperationException("Unsupported inline binary field."),
            };
            builder.Append(Indent(readCode, 2));

            if (field.Name != "_" && field is not FieldDefinitionNode { TypeAnnotation: AlignmentNode })
            {
                AppendGeneratedLine(
                    builder,
                    $"        __parsedValue.{EscapeCSharpIdentifier(field.Name)} = {GetLocalVarName(field.Name)};"
                );
                AppendGeneratedLine(builder, $"        base.RecordParsedFieldSlot({completionIndex++});");
            }
        }

        builder.Append(InterpreterStatusCodeEmitter.End(className));

        builder.AppendLine("}");

        return builder.ToString();
    }

    private string GetClrTypeNameForFieldInline(SchemaFieldNode field) =>
        GetClrTypeNameForFieldWithContext(field, _expressionFields.ToList());

    private string GenerateInlineSchemaReadCode(string localVar, string fieldName, InlineSchemaTypeNode inlineSchema)
    {
        var inlineClassName = _registry.BinaryCompositions.Name(inlineSchema);
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"var {tempInterpreter} = {CreateNestedInterpreter(inlineClassName)};"
        );
        builder.Append(
            StatusRead(localVar, $"base.InterpretNestedStatus({tempInterpreter}, data, \"{EscapeString(fieldName)}\")")
        );
        return builder.ToString();
    }
}
