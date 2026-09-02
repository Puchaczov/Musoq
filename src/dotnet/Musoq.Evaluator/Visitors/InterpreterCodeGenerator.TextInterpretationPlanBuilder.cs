using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    /// <summary>
    ///     Builds an immutable bound plan for a text schema, resolving field ordering
    ///     and per-field property-shape decisions ahead of C# rendering.
    /// </summary>
    /// <param name="schema">The text schema to bind.</param>
    /// <returns>The resolved bound interpretation plan.</returns>
    public BoundTextInterpretationPlan BuildTextPlan(TextSchemaNode schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _discardCounter = 0;

        var allFields = GetAllTextSchemaFields(schema);
        var boundFields = new List<BoundTextField>(allFields.Count);
        foreach (var field in allFields)
            boundFields.Add(BindTextField(field));

        return new BoundTextInterpretationPlan
        {
            SchemaName = schema.Name,
            Extends = string.IsNullOrEmpty(schema.Extends) ? null : schema.Extends,
            Fields = boundFields
        };
    }

    private BoundTextField BindTextField(TextFieldDefinitionNode field)
    {
        var localVariableName = GetLocalVarName(field.Name);

        if (field.IsDiscard)
            return new BoundTextField
            {
                Source = field,
                Name = field.Name,
                LocalVariableName = localVariableName,
                IsDiscard = true
            };

        if (field is { FieldType: TextFieldType.Pattern, CaptureGroups.Length: > 0 })
            return new BoundTextField
            {
                Source = field,
                Name = field.Name,
                LocalVariableName = localVariableName,
                IsDiscard = false,
                PropertyName = EscapeCSharpIdentifier(field.Name),
                PropertyClrType = GetTextFieldPropertyClrType(field),
                IsCaptureResult = true,
                CaptureGroups = field.CaptureGroups
            };

        var propertyClrType = GetTextFieldPropertyClrType(field);

        return new BoundTextField
        {
            Source = field,
            Name = field.Name,
            LocalVariableName = localVariableName,
            IsDiscard = false,
            PropertyName = EscapeCSharpIdentifier(field.Name),
            PropertyClrType = propertyClrType
        };
    }

    private static string GetTextFieldPropertyClrType(TextFieldDefinitionNode field)
    {
        if (field is { FieldType: TextFieldType.Pattern, CaptureGroups.Length: > 0 })
            return $"CaptureResult_{field.Name}?";

        return field.FieldType switch
        {
            TextFieldType.Repeat => $"{field.PrimaryValue ?? "object"}[]?",
            TextFieldType.Switch => "object?",
            TextFieldType.SchemaReference => $"{field.PrimaryValue ?? "object"}?",
            _ => "string?"
        };
    }

    private List<TextFieldDefinitionNode> GetAllTextSchemaFields(TextSchemaNode textSchema)
    {
        var fields = new List<TextFieldDefinitionNode>();

        if (!string.IsNullOrWhiteSpace(textSchema.Extends) &&
            _registry.TryGetSchema(textSchema.Extends, out var registration) &&
            registration?.Node is TextSchemaNode parent)
        {
            fields.AddRange(GetAllTextSchemaFields(parent));
        }

        foreach (var field in textSchema.Fields)
        {
            if (field.IsDiscard)
            {
                fields.Add(field);
                continue;
            }

            var existingIndex = fields.FindIndex(existing =>
                !existing.IsDiscard &&
                string.Equals(existing.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                fields[existingIndex] = field;
            else
                fields.Add(field);
        }

        return fields;
    }
}
