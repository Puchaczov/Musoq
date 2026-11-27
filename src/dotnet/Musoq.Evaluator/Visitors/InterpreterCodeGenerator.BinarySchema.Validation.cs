using System.Globalization;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateFieldValueValidationStatement(FieldDefinitionNode field, string localVar)
    {
        var validation = field.ValueValidation;
        if (validation is null)
            return string.Empty;

        var condition = validation.IsByteList
            ? GenerateByteListValidationCondition(validation, localVar)
            : GenerateScalarValidationCondition(validation, localVar, field);

        var message = GenerateValidationFailureMessage(validation, field.Name);
        return StatusCheck(
            $"base.ValidateWithStatus({condition}, \"{EscapeString(field.Name)}\", \"{EscapeString(message)}\")"
        );
    }

    private string GenerateScalarValidationCondition(
        FieldValueValidationNode validation,
        string localVar,
        FieldDefinitionNode field
    )
    {
        var type = field.TypeAnnotation.ClrType;

        if (validation.Kind != FieldValueValidationKind.OneOf)
            return GenerateScalarLiteralEquality(type, localVar, validation.Values[0]);

        return $"({string.Join(" || ", validation.Values.Select(value => GenerateScalarLiteralEquality(type, localVar, value)))})";
    }

    private string GenerateScalarLiteralEquality(Type type, string localVar, Node value)
    {
        if (type == typeof(string) || type == typeof(bool) || type == typeof(decimal))
            return $"{localVar} == {GenerateConditionExpression(value)}";

        if (type == typeof(float) || type == typeof(double))
        {
            var expected = GenerateConditionExpression(value);
            if (BinarySchemaExpressionTypes.Resolve(value, [], _registry) != typeof(decimal))
                return $"{localVar} == {expected}";

            // The first unrepresentable positive decimal is 2^96, exactly representable in either float format.
            var floating = BinarySchemaClrTypeNames.Map(type);
            return $"({localVar} > ({floating})decimal.MinValue && {localVar} < ({floating})decimal.MaxValue && (decimal){localVar} == {expected})";
        }

        decimal number;
        if (type == typeof(ulong) && BinarySchemaScalarLiteral.TryGetUnsignedInteger(value, out var unsigned))
            number = unsigned;
        else if (!BinarySchemaScalarLiteral.TryGetNumber(value, out number))
            return "false";

        if (!BinarySchemaScalarLiteral.FitsIntegralType(type, number))
            return "false";

        return $"{localVar} == {number.ToString("0", CultureInfo.InvariantCulture)}";
    }

    private string GenerateByteListValidationCondition(FieldValueValidationNode validation, string localVar)
    {
        var bytes = validation.Values.Select(GenerateConditionExpression);
        return $"BytesEqual({localVar}, new byte[] {{ {string.Join(", ", bytes)} }})";
    }

    private static string GenerateValidationFailureMessage(FieldValueValidationNode validation, string fieldName)
    {
        return validation.Kind switch
        {
            FieldValueValidationKind.Magic => $"Field '{fieldName}' did not match the expected magic value.",
            FieldValueValidationKind.OneOf => $"Field '{fieldName}' value is not one of the allowed values.",
            _ => $"Field '{fieldName}' did not match the expected constant value.",
        };
    }
}
