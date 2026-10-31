using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateBinaryInterpreterClass(BinarySchemaNode schema)
    {
        var plan = BuildBinaryPlan(schema);

        var builder = new StringBuilder();
        var className = plan.SchemaName;
        var typeParameters = plan.TypeParameters;

        var typeParamsDecl = schema.IsGeneric
            ? $"<{string.Join(", ", typeParameters)}>"
            : string.Empty;
        var fullClassName = $"{className}{typeParamsDecl}";

        var genericConstraints = string.Empty;
        if (schema.IsGeneric)
        {
            var constraints = typeParameters
                .Select(t => $"where {t} : IBytesInterpreter<{t}>, new()")
                .ToArray();
            genericConstraints = " " + string.Join(" ", constraints);
        }

        var allFields = plan.Fields.Select(f => f.Source).ToList();

        builder.AppendLine("/// <summary>");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"/// Generated interpreter for binary schema '{className}'.");
        if (schema.IsGeneric)
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"/// This is a generic schema with type parameters: {string.Join(", ", typeParameters)}.");
        if (!string.IsNullOrEmpty(schema.Extends)) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"/// Extends schema '{schema.Extends}'.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("[global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"public sealed class {fullClassName} : BytesInterpreterBase<{fullClassName}>, global::Musoq.Plugins.IJsonPayload{genericConstraints}");
        builder.AppendLine("{");
        builder.Append(GenerateParentContextMembers(fullClassName));
        builder.Append(PartialPayloadCodeEmitter.Members(fullClassName));
        builder.Append(PartialPayloadCodeEmitter.JsonMembers(plan.Fields.Where(static field => field.EmitsProperty)
            .Select(static field => (field.Name, field.PropertyName ?? throw new InvalidOperationException("An emitted payload field requires a property name.")))));

        foreach (var field in plan.Fields)
        {
            if (!field.EmitsProperty) continue;

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} field value.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {field.PropertyClrType} {field.PropertyName} {{ get; set; }}");
            builder.AppendLine();
        }

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override string SchemaName => \"{className}\";");
        builder.AppendLine();

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override {fullClassName} InterpretAt(ReadOnlySpan<byte> data, int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        InitializeParsePosition(data, offset);");
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        var __parsedValue = new {fullClassName}();");
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        _discardCounter = 0;

        foreach (var field in plan.Fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{EscapeString(field.Name)}\");");

            switch (field.Source)
            {
                case FieldDefinitionNode parsedField:
                    builder.Append(Indent(GenerateFieldReadCodeWithModifiers(parsedField), 2));
                    break;
                case ComputedFieldNode computedField:
                    builder.Append(Indent(GenerateComputedFieldCode(computedField, allFields), 2));
                    break;
            }

            if (field.EmitsProperty)
            {
                AppendGeneratedLine(builder, $"        __parsedValue.{field.PropertyName} = {field.LocalVariableName};");
                AppendGeneratedLine(builder, $"        RecordParsedField(\"{EscapeString(field.Name)}\");");
            }
        }

        builder.AppendLine("        return __parsedValue;");
        builder.AppendLine("    }");

        builder.AppendLine("}");

        return builder.ToString();
    }

    private string GenerateFieldReadCodeWithModifiers(FieldDefinitionNode field)
    {
        _activeSchemaFieldName = field.Name;
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var clrTypeName = GetClrTypeNameForFieldDefinition(field);

        if (field.WhenCondition != null)
        {
            if (BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(field.WhenCondition, _expressionFields, _registry)) != typeof(bool))
                throw new Musoq.Evaluator.Exceptions.QuerySyntaxException($"Binary schema field '{field.Name}' when expression must evaluate to boolean.", field.WhenCondition.SpanOrEmpty(), Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint);
            var condition = GenerateConditionExpression(field.WhenCondition);
            var isTypeParam = IsTypeParameter(clrTypeName);
            var isReferenceType = IsReferenceType(clrTypeName);

            string nullableTypeName;
            string defaultValue;

            if (isTypeParam)
            {
                nullableTypeName = clrTypeName;
                defaultValue = "default!";
            }
            else if (isReferenceType)
            {
                nullableTypeName = clrTypeName;
                defaultValue = "null";
            }
            else
            {
                nullableTypeName = $"{clrTypeName}?";
                defaultValue = "null";
            }

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{nullableTypeName} {localVar} = {defaultValue};");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (({condition}) == true)");
            builder.AppendLine("{");

            if (field.AtOffset != null)
            {
                var offsetExpr = GenerateSizeExpression(field.AtOffset, Musoq.Schema.Interpreters.ParseErrorCode.InvalidPosition);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    SeekTo({offsetExpr});");
            }

            var innerReadCode = GenerateFieldReadCodeInner(field, localVar);
            builder.Append(Indent(innerReadCode, 1));

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    Validate(({checkExpr}) == true, \"{field.Name}\", \"Check constraint failed\");");
            }

            var valueValidationStatement = GenerateFieldValueValidationStatement(field, localVar);
            if (!string.IsNullOrEmpty(valueValidationStatement))
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {valueValidationStatement}");

            builder.AppendLine("}");
        }
        else
        {
            if (field.AtOffset != null)
            {
                var offsetExpr = GenerateSizeExpression(field.AtOffset, Musoq.Schema.Interpreters.ParseErrorCode.InvalidPosition);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"SeekTo({offsetExpr});");
            }

            builder.AppendLine(GenerateFieldReadCode(field));

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Validate(({checkExpr}) == true, \"{field.Name}\", \"Check constraint failed\");");
            }

            var valueValidationStatement = GenerateFieldValueValidationStatement(field, localVar);
            if (!string.IsNullOrEmpty(valueValidationStatement))
                builder.AppendLine(valueValidationStatement);
        }

        return builder.ToString();
    }

}
