using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"public sealed class {fullClassName} : BytesInterpreterBase<{fullClassName}>{genericConstraints}");
        builder.AppendLine("{");

        foreach (var field in plan.Fields)
        {
            if (!field.EmitsProperty) continue;

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} field value.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {field.PropertyClrType} {field.PropertyName} {{ get; init; }}");
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
        builder.AppendLine();

        _discardCounter = 0;

        var fieldInitializers = new List<string>();
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
                AppendGeneratedLine(builder, $"        RecordParsedField(\"{EscapeString(field.Name)}\", {field.LocalVariableName});");
                fieldInitializers.Add($"{field.PropertyName} = {field.LocalVariableName}");
            }
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        return new {fullClassName}");
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

    private string GenerateFieldReadCodeWithModifiers(FieldDefinitionNode field)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var clrTypeName = GetClrTypeNameForFieldDefinition(field);

        if (field.WhenCondition != null)
        {
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
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if ({condition})");
            builder.AppendLine("{");

            if (field.AtOffset != null)
            {
                var offsetExpr = GenerateConditionExpression(field.AtOffset);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    SeekTo((int)({offsetExpr}));");
            }

            var innerReadCode = GenerateFieldReadCodeInner(field, localVar);
            builder.Append(Indent(innerReadCode, 1));

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    Validate({checkExpr}, \"{field.Name}\", \"Check constraint failed\");");
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
                var offsetExpr = GenerateConditionExpression(field.AtOffset);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"SeekTo((int)({offsetExpr}));");
            }

            builder.AppendLine(GenerateFieldReadCode(field));

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Validate({checkExpr}, \"{field.Name}\", \"Check constraint failed\");");
            }

            var valueValidationStatement = GenerateFieldValueValidationStatement(field, localVar);
            if (!string.IsNullOrEmpty(valueValidationStatement))
                builder.AppendLine(valueValidationStatement);
        }

        return builder.ToString();
    }

}
