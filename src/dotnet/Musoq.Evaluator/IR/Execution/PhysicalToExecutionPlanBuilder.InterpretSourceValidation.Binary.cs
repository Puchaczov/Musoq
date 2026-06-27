using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private InterpretSourceValidationResult ValidateBinaryInterpretSource(BinarySchemaNode binary)
    {
        return ValidateBinaryInterpretSource(
            binary,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            BinarySchemaGenericResolver.CreateEmptyBindings());
    }

    private InterpretSourceValidationResult ValidateBinaryInterpretSource(
        BinarySchemaNode binary,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (binary.IsGeneric && genericBindings.Count == 0)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering does not support generic binary schema '{binary.Name}'.");
        }

        var schemaKey = BinarySchemaGenericResolver.CreateSchemaKey(binary, genericBindings);
        if (!validatedSchemas.Add(schemaKey))
            return InterpretSourceValidationResult.Success();

        if (!string.IsNullOrWhiteSpace(binary.Extends))
        {
            if (_schemaRegistry == null ||
                !_schemaRegistry.TryGetSchema(binary.Extends, out var parentRegistration) ||
                parentRegistration?.Node is not BinarySchemaNode parent)
            {
                return InterpretSourceValidationResult.Unsupported(
                    $"Execution IR binary interpret-source lowering cannot resolve parent schema '{binary.Extends}' for schema '{binary.Name}'.");
            }

            var parentResult = ValidateBinaryInterpretSource(
                parent,
                validatedSchemas,
                BinarySchemaGenericResolver.CreateEmptyBindings());
            if (!parentResult.Supported)
                return parentResult;
        }

        foreach (var field in binary.Fields)
        {
            var fieldResult = ValidateBinaryField(field, validatedSchemas, genericBindings);
            if (!fieldResult.Supported)
                return fieldResult;
        }

        return InterpretSourceValidationResult.Success();
    }

    private InterpretSourceValidationResult ValidateBinaryField(
        SchemaFieldNode field,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        var nameResult = ValidateBinaryFieldName(field);
        if (!nameResult.Supported)
            return nameResult;

        return field switch
        {
            FieldDefinitionNode definition => ValidateBinaryType(
                definition.TypeAnnotation,
                definition.Name,
                validatedSchemas,
                genericBindings),
            ComputedFieldNode => InterpretSourceValidationResult.Success(),
            _ => InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering does not support field node '{field.GetType().Name}' on field '{field.Name}'.")
        };
    }

    private static InterpretSourceValidationResult ValidateBinaryFieldName(SchemaFieldNode field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return InterpretSourceValidationResult.Success();
    }
}
