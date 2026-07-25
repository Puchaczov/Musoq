using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private InterpretSourceValidationResult ValidateTextInterpretSource(TextSchemaNode text)
    {
        if (!string.IsNullOrWhiteSpace(text.Extends))
        {
            if (_schemaRegistry == null ||
                !_schemaRegistry.TryGetSchema(text.Extends, out var parentRegistration) ||
                parentRegistration?.Node is not TextSchemaNode parent)
            {
                return InterpretSourceValidationResult.Unsupported(
                    $"Execution IR text interpret-source lowering cannot resolve parent schema '{text.Extends}' for schema '{text.Name}'.");
            }

            var parentResult = ValidateTextInterpretSource(parent);
            if (!parentResult.IsBuilt)
                return parentResult;
        }

        foreach (var field in text.Fields)
        {
            var fieldResult = ValidateTextField(field);
            if (!fieldResult.IsBuilt)
                return fieldResult;
        }

        return InterpretSourceValidationResult.Success();
    }

    private static InterpretSourceValidationResult ValidateTextField(TextFieldDefinitionNode field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return InterpretSourceValidationResult.Success();
    }
}
