using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateBinaryInterpreterClass(BinarySchemaNode schema, BinaryFieldReaderPlan? reader = null)
    {
        var plan = BuildBinaryPlan(schema);

        var builder = new StringBuilder();
        var className = plan.SchemaName;
        var typeParameters = plan.TypeParameters.Select(EscapeCSharpIdentifier).ToArray();

        var typeParamsDecl = schema.IsGeneric ? $"<{string.Join(", ", typeParameters)}>" : string.Empty;
        var fullClassName = $"{EscapeCSharpIdentifier(className)}{typeParamsDecl}";

        var genericConstraints = string.Empty;
        if (schema.IsGeneric)
        {
            var constraints = typeParameters.Select(t => $"where {t} : IBytesInterpreter<{t}>, new()").ToArray();
            genericConstraints = " " + string.Join(" ", constraints);
        }

        var allFields = plan.Fields.Select(f => f.Source).ToList();

        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"/// Generated interpreter for binary schema '{className}'."
        );
        if (schema.IsGeneric)
            builder.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"/// This is a generic schema with type parameters: {string.Join(", ", typeParameters)}."
            );
        if (!string.IsNullOrEmpty(schema.Extends))
            builder.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"/// Extends schema '{schema.Extends}'."
            );
        builder.AppendLine("/// </summary>");
        var properties = plan
            .Fields.Where(static field => field.EmitsProperty)
            .Select(field => new PayloadProperty(
                field.Name,
                reader?.Demand(field.Name) == BinaryFieldDemand.Length ? "int?" : field.PropertyClrType!
            ))
            .ToArray();
        var declaration = new InterpreterDeclaration(
            className,
            "BytesInterpreterBase",
            "IBytesInterpreterStatus",
            typeParamsDecl,
            genericConstraints
        );
        var implementation = OpenInterpreterClass(builder, declaration, properties);
        builder.Append(GenerateParentContextMembers(className, implementation));

        BeginStatusMethod(fullClassName);
        builder.Append(InterpreterStatusCodeEmitter.BeginBinary(fullClassName));
        builder.Append(StatusCheck("base.InitializeParsePositionStatus(data, offset)"));
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"        __parsedValue = new {fullClassName}();"
        );
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        _discardCounter = 0;

        var completionIndex = 0;
        foreach (var field in plan.Fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{EscapeString(field.Name)}\");");

            switch (field.Source)
            {
                case FieldDefinitionNode parsedField:
                    builder.Append(
                        Indent(
                            GenerateFieldReadCodeWithModifiers(
                                parsedField,
                                reader?.Demand(field.Name) ?? BinaryFieldDemand.Value
                            ),
                            2
                        )
                    );
                    break;
                case ComputedFieldNode computedField:
                    builder.Append(Indent(GenerateComputedFieldCode(computedField, allFields), 2));
                    break;
            }

            if (field.EmitsProperty)
            {
                if (reader?.Demand(field.Name) != BinaryFieldDemand.Discard)
                    AppendGeneratedLine(
                        builder,
                        $"        __parsedValue.{field.PropertyName} = {field.LocalVariableName};"
                    );
                AppendGeneratedLine(builder, $"        base.RecordParsedFieldSlot({completionIndex++});");
            }
        }

        builder.Append(InterpreterStatusCodeEmitter.End(fullClassName));

        builder.AppendLine("}");

        return builder.ToString();
    }

    private string GenerateFieldReadCodeWithModifiers(
        FieldDefinitionNode field,
        BinaryFieldDemand demand = BinaryFieldDemand.Value
    )
    {
        _activeSchemaFieldName = field.Name;
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var clrTypeName = GetClrTypeNameForFieldDefinition(field);

        if (field is { Name: "_", Constraint: null, ValueValidation: null, TypeAnnotation: ByteArrayTypeNode })
            demand = BinaryFieldDemand.Discard;

        if (field.WhenCondition != null)
        {
            if (
                BinarySchemaExpressionTypes.Unwrap(
                    BinarySchemaExpressionTypes.Resolve(field.WhenCondition, _expressionFields, _registry)
                ) != typeof(bool)
            )
                throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
                    $"Binary schema field '{field.Name}' when expression must evaluate to boolean.",
                    field.WhenCondition.SpanOrEmpty(),
                    Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint
                );
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

            if (demand == BinaryFieldDemand.Length)
                builder.AppendLine($"int? {localVar} = null;");
            else if (demand == BinaryFieldDemand.Value)
                builder.AppendLine(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{nullableTypeName} {localVar} = {defaultValue};"
                );
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (({condition}) == true)");
            builder.AppendLine("{");

            if (field.AtOffset != null)
            {
                var offset = NextStatusName("__musoqOffset");
                builder.Append(
                    StatusSizeDeclaration(
                        offset,
                        field.AtOffset,
                        Musoq.Schema.Interpreters.ParseErrorCode.InvalidPosition
                    )
                );
                builder.Append(StatusCheck($"base.SeekWithStatus({offset})"));
            }

            var innerReadCode =
                demand == BinaryFieldDemand.Value
                    ? GenerateFieldReadCodeInner(field, localVar)
                    : GenerateByteRegionRead(localVar, (ByteArrayTypeNode)field.TypeAnnotation, demand, declare: false);
            builder.Append(Indent(innerReadCode, 1));

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.Append(
                    StatusCheck(
                        $"base.ValidateWithStatus(({checkExpr}) == true, \"{EscapeString(field.Name)}\", \"Check constraint failed\")"
                    )
                );
            }

            var valueValidationStatement = GenerateFieldValueValidationStatement(field, localVar);
            if (!string.IsNullOrEmpty(valueValidationStatement))
                builder.AppendLine(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"    {valueValidationStatement}"
                );

            builder.AppendLine("}");
        }
        else
        {
            if (field.AtOffset != null)
            {
                var offset = NextStatusName("__musoqOffset");
                builder.Append(
                    StatusSizeDeclaration(
                        offset,
                        field.AtOffset,
                        Musoq.Schema.Interpreters.ParseErrorCode.InvalidPosition
                    )
                );
                builder.Append(StatusCheck($"base.SeekWithStatus({offset})"));
            }

            builder.AppendLine(
                demand == BinaryFieldDemand.Value
                    ? GenerateBinaryValueRead(localVar, field.Name, field.TypeAnnotation)
                    : GenerateByteRegionRead(localVar, (ByteArrayTypeNode)field.TypeAnnotation, demand, declare: true)
            );

            if (field.Constraint != null)
            {
                var checkExpr = GenerateConditionExpression(field.Constraint.Expression);
                builder.Append(
                    StatusCheck(
                        $"base.ValidateWithStatus(({checkExpr}) == true, \"{EscapeString(field.Name)}\", \"Check constraint failed\")"
                    )
                );
            }

            var valueValidationStatement = GenerateFieldValueValidationStatement(field, localVar);
            if (!string.IsNullOrEmpty(valueValidationStatement))
                builder.AppendLine(valueValidationStatement);
        }

        return builder.ToString();
    }
}
