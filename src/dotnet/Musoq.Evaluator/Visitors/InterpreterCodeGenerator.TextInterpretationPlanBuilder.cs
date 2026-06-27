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

        var boundFields = new List<BoundTextField>(schema.Fields.Length);
        foreach (var field in schema.Fields)
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
                PropertyClrType = $"CaptureResult_{field.Name}?",
                IsCaptureResult = true,
                CaptureGroups = field.CaptureGroups
            };

        var propertyClrType = field.FieldType switch
        {
            TextFieldType.Repeat => $"{field.PrimaryValue ?? "object"}[]?",
            TextFieldType.Switch => "object?",
            _ => "string?"
        };

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
}
