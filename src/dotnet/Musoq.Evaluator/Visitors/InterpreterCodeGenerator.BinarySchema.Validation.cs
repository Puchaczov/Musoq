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
        return $"Validate({condition}, \"{field.Name}\", \"{EscapeString(message)}\");";
    }

    private string GenerateScalarValidationCondition(
        FieldValueValidationNode validation, string localVar, FieldDefinitionNode field)
    {
        // Casting the expected literal to the field's CLR type preserves the bit pattern so unsigned
        // fields validate magic/const/oneOf values above int.MaxValue correctly.
        var castType = GetClrTypeNameForFieldDefinition(field) is { } typeName && typeName
            is "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong"
            ? typeName
            : null;

        string Expected(Node value) => castType is null
            ? GenerateConditionExpression(value)
            : $"unchecked(({castType})({GenerateConditionExpression(value)}))";

        if (validation.Kind != FieldValueValidationKind.OneOf)
            return $"{localVar} == {Expected(validation.Values[0])}";

        var comparisons = validation.Values.Select(value => $"{localVar} == {Expected(value)}");
        return $"({string.Join(" || ", comparisons)})";
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
            _ => $"Field '{fieldName}' did not match the expected constant value."
        };
    }
}
