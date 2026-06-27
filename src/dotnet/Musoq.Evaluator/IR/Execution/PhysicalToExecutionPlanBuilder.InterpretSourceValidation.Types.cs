using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private InterpretSourceValidationResult ValidateBinaryType(
        TypeAnnotationNode type,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (type is PrimitiveTypeNode or ByteArrayTypeNode or StringTypeNode or BitsTypeNode or AlignmentNode)
            return InterpretSourceValidationResult.Success();

        if (type is SchemaReferenceTypeNode reference)
            return ValidateSchemaReferenceType(reference, fieldName, validatedSchemas, genericBindings);

        if (type is InlineSchemaTypeNode inlineSchema)
            return ValidateInlineSchemaType(inlineSchema, fieldName, validatedSchemas, genericBindings);

        if (type is ArrayTypeNode arrayType)
            return ValidateArrayType(arrayType, fieldName, validatedSchemas, genericBindings);

        if (type is RepeatUntilTypeNode repeatUntilType)
            return ValidateRepeatUntilType(repeatUntilType, fieldName, validatedSchemas, genericBindings);

        if (type is BinarySwitchTypeNode switchType)
            return ValidateBinarySwitchType(switchType, fieldName, validatedSchemas, genericBindings);

        if (type is SubstreamTypeNode substreamType)
            return ValidateSubstreamType(substreamType, fieldName, validatedSchemas, genericBindings);

        return InterpretSourceValidationResult.Unsupported(
            $"Execution IR binary interpret-source lowering currently supports primitive, bits, string, byte-array, computed scalar, inline schema, non-generic and closed-generic schema-reference, primitive-array, string-array, inline-schema-array, primitive repeat-until, bits repeat-until, string repeat-until, inline-schema repeat-until, non-generic and closed-generic schema-reference-array, and non-generic and closed-generic schema-reference repeat-until fields. Found {type.GetType().Name} on field '{fieldName}'.");
    }

    private InterpretSourceValidationResult ValidateSubstreamType(
        SubstreamTypeNode substreamType,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (substreamType.Mode == SubstreamMode.Raw)
            return InterpretSourceValidationResult.Success();

        if (substreamType.Target is SchemaReferenceTypeNode reference)
            return ValidateSchemaReferenceType(reference, fieldName, validatedSchemas, genericBindings);

        if (substreamType.Target is InlineSchemaTypeNode inlineSchema)
            return ValidateInlineSchemaType(inlineSchema, fieldName, validatedSchemas, genericBindings);

        return InterpretSourceValidationResult.Unsupported(
            $"Execution IR binary interpret-source lowering currently supports raw substream fields and schema-reference or inline-schema substream targets. Found {substreamType.Target?.GetType().Name ?? "none"} on field '{fieldName}'.");
    }

    private InterpretSourceValidationResult ValidateBinarySwitchType(
        BinarySwitchTypeNode switchType,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        foreach (var switchCase in switchType.Cases)
        {
            var branchResult = ValidateBinarySwitchBranchType(
                switchCase.BranchType,
                $"{fieldName}.{switchCase.BranchAlias}",
                validatedSchemas,
                genericBindings);

            if (!branchResult.Supported)
                return branchResult;
        }

        return InterpretSourceValidationResult.Success();
    }

    private InterpretSourceValidationResult ValidateBinarySwitchBranchType(
        TypeAnnotationNode branchType,
        string branchName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (branchType is PrimitiveTypeNode or ByteArrayTypeNode)
            return InterpretSourceValidationResult.Success();

        if (branchType is SchemaReferenceTypeNode reference)
            return ValidateSchemaReferenceType(reference, branchName, validatedSchemas, genericBindings);

        return InterpretSourceValidationResult.Unsupported(
            $"Execution IR binary interpret-source lowering currently supports primitive, byte-array, and non-generic or closed-generic schema-reference switch branches. Found {branchType.GetType().Name} on branch '{branchName}'.");
    }

    private InterpretSourceValidationResult ValidateInlineSchemaType(
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        foreach (var inlineField in inlineSchema.Fields)
        {
            var fieldResult = ValidateBinaryField(inlineField, validatedSchemas, genericBindings);
            if (!fieldResult.Supported)
            {
                return InterpretSourceValidationResult.Unsupported(
                    $"Execution IR binary interpret-source lowering does not support inline schema field '{fieldName}.{inlineField.Name}': {fieldResult.UnsupportedReason}");
            }
        }

        return InterpretSourceValidationResult.Success();
    }

    private InterpretSourceValidationResult ValidateArrayType(
        ArrayTypeNode arrayType,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (arrayType.ElementType is PrimitiveTypeNode or StringTypeNode)
            return InterpretSourceValidationResult.Success();

        if (arrayType.ElementType is SchemaReferenceTypeNode reference)
            return ValidateSchemaReferenceType(reference, fieldName, validatedSchemas, genericBindings);

        if (arrayType.ElementType is InlineSchemaTypeNode inlineSchema)
            return ValidateInlineSchemaType(inlineSchema, fieldName, validatedSchemas, genericBindings);

        return InterpretSourceValidationResult.Unsupported(
            $"Execution IR binary interpret-source lowering currently supports primitive arrays, string arrays, inline-schema arrays, and non-generic or closed-generic schema-reference arrays. Found {arrayType.ElementType.GetType().Name} on field '{fieldName}'.");
    }

    private InterpretSourceValidationResult ValidateRepeatUntilType(
        RepeatUntilTypeNode repeatUntilType,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (repeatUntilType.ElementType is PrimitiveTypeNode or StringTypeNode or BitsTypeNode)
            return InterpretSourceValidationResult.Success();

        if (repeatUntilType.ElementType is SchemaReferenceTypeNode reference)
            return ValidateSchemaReferenceType(reference, fieldName, validatedSchemas, genericBindings);

        if (repeatUntilType.ElementType is InlineSchemaTypeNode inlineSchema)
            return ValidateInlineSchemaType(inlineSchema, fieldName, validatedSchemas, genericBindings);

        return InterpretSourceValidationResult.Unsupported(
            $"Execution IR binary interpret-source lowering currently supports primitive repeat-until fields, bits repeat-until fields, string repeat-until fields, inline-schema repeat-until fields, and non-generic or closed-generic schema-reference repeat-until fields. Found {repeatUntilType.ElementType.GetType().Name} on field '{fieldName}'.");
    }

    private InterpretSourceValidationResult ValidateSchemaReferenceType(
        SchemaReferenceTypeNode reference,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        var resolvedReference = BinarySchemaGenericResolver.ResolveReference(reference, genericBindings);
        if (resolvedReference.IsGenericInstantiation)
            return ValidateGenericSchemaReference(resolvedReference, fieldName, validatedSchemas, genericBindings);

        if (_schemaRegistry == null ||
            !_schemaRegistry.TryGetSchema(resolvedReference.SchemaName, out var registration) ||
            registration?.Node is not BinarySchemaNode referencedBinary)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering cannot resolve binary schema reference '{resolvedReference.SchemaName}' on field '{fieldName}'.");
        }

        return ValidateBinaryInterpretSource(
            referencedBinary,
            validatedSchemas,
            BinarySchemaGenericResolver.CreateEmptyBindings());
    }

    private InterpretSourceValidationResult ValidateGenericSchemaReference(
        SchemaReferenceTypeNode reference,
        string fieldName,
        ISet<string> validatedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> genericBindings)
    {
        if (_schemaRegistry == null ||
            !_schemaRegistry.TryGetSchema(reference.SchemaName, out var registration) ||
            registration?.Node is not BinarySchemaNode referencedBinary)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering cannot resolve generic binary schema reference '{reference.FullTypeName}' on field '{fieldName}'.");
        }

        if (!referencedBinary.IsGeneric)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering found type arguments for non-generic schema reference '{reference.FullTypeName}' on field '{fieldName}'.");
        }

        if (referencedBinary.TypeParameters.Length != reference.TypeArguments.Length)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR binary interpret-source lowering found {reference.TypeArguments.Length} type arguments for generic schema reference '{reference.FullTypeName}' on field '{fieldName}', but schema '{referencedBinary.Name}' declares {referencedBinary.TypeParameters.Length}.");
        }

        var closedGenericBindings = BinarySchemaGenericResolver.CreateBindings(
            referencedBinary,
            reference,
            genericBindings);

        return ValidateBinaryInterpretSource(referencedBinary, validatedSchemas, closedGenericBindings);
    }
}
