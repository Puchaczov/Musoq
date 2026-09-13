using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.StructuralInputs;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Visitors;

/// <summary>Creates deterministic indexed construction plans for receiving contracts.</summary>
internal static class StructuralConstructionPlanBinder
{
    public static bool TryBind(
        StructuralTypeDescriptor input,
        StructuralTypeDescriptor receiver,
        out StructuralConstructionPlan plan,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(receiver);
        plan = null!;

        if (input.Kind != StructuralTypeKind.Record || receiver.Kind != StructuralTypeKind.Record)
        {
            error = "Structural construction requires record input and receiving shapes.";
            return false;
        }

        if (receiver.ClrType == null || receiver.ClrType == typeof(StructuralValue))
        {
            error = "A structural receiver must expose a concrete target type.";
            return false;
        }

        if (!TryValidateCompatibility(input, receiver, out error))
            return false;

        try
        {
            var constructor = StructuralInputMetadata.SelectConstructor(receiver.ClrType);
            var constructorDescriptor = new StructuralConstructorDescriptor(constructor, receiver.Fields);
            var indexes = receiver.Fields
                .Select(field => FindFieldIndex(input.Fields, field.Name))
                .ToArray();
            plan = new StructuralConstructionPlan(receiver.ClrType, constructorDescriptor, indexes);
            return true;
        }
        catch (InvalidOperationException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    internal static bool TryValidateCompatibility(
        StructuralTypeDescriptor input,
        StructuralTypeDescriptor receiver,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(receiver);

        if (input.Kind != receiver.Kind)
        {
            error = $"Structural input kind '{input.Kind}' is incompatible with receiver kind '{receiver.Kind}'.";
            return false;
        }

        if (input.IsNullable && !receiver.IsNullable)
        {
            error = "A nullable structural value cannot be passed to a non-nullable receiver.";
            return false;
        }

        switch (input.Kind)
        {
            case StructuralTypeKind.Record:
                return TryValidateRecord(input, receiver, out error);
            case StructuralTypeKind.Collection:
                return TryValidateCompatibility(input.ElementType!, receiver.ElementType!, out error);
            case StructuralTypeKind.Scalar:
                if (CanAssignScalar(input.ScalarType!, receiver.ScalarType!))
                {
                    error = string.Empty;
                    return true;
                }

                error = $"Structural scalar '{input.ToCanonicalSql()}' is incompatible with receiver '{receiver.ToCanonicalSql()}'.";
                return false;
            default:
                error = $"Structural kind '{input.Kind}' is not supported.";
                return false;
        }
    }
    private static bool TryValidateRecord(
        StructuralTypeDescriptor input,
        StructuralTypeDescriptor receiver,
        out string error)
    {
        foreach (var inputField in input.Fields)
        {
            var receiverField = FindField(receiver.Fields, inputField.Name);
            if (receiverField == null)
            {
                error = $"Structural input contains unexpected field '{inputField.Name}'.";
                return false;
            }

            if (!CanAssign(inputField.Type, receiverField.Type))
            {
                error = $"Structural field '{inputField.Name}' has type '{inputField.Type.ToCanonicalSql()}', but the receiver expects '{receiverField.Type.ToCanonicalSql()}'.";
                return false;
            }
        }

        foreach (var receiverField in receiver.Fields)
        {
            if (FindField(input.Fields, receiverField.Name) == null && receiverField.Required)
            {
                error = $"Structural input is missing required field '{receiverField.Name}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool CanAssign(
        StructuralTypeDescriptor input,
        StructuralTypeDescriptor receiver)
    {
        if (input.Kind != receiver.Kind)
            return false;

        if (input.IsNullable && !receiver.IsNullable)
            return false;

        return input.Kind switch
        {
            StructuralTypeKind.Scalar => CanAssignScalar(input.ScalarType!, receiver.ScalarType!),
            StructuralTypeKind.Record => CanAssignRecord(input, receiver),
            StructuralTypeKind.Collection => CanAssign(input.ElementType!, receiver.ElementType!),
            _ => false
        };
    }

    private static bool CanAssignRecord(
        StructuralTypeDescriptor input,
        StructuralTypeDescriptor receiver)
    {
        foreach (var inputField in input.Fields)
        {
            var receiverField = FindField(receiver.Fields, inputField.Name);
            if (receiverField == null || !CanAssign(inputField.Type, receiverField.Type))
                return false;
        }

        return receiver.Fields.All(receiverField =>
            FindField(input.Fields, receiverField.Name) != null || !receiverField.Required);
    }

    private static bool CanAssignScalar(Type input, Type receiver)
    {
        return SchemaConversionClassifier.TryGetCost(input, receiver, out _);
    }

    private static StructuralFieldDescriptor? FindField(
        IReadOnlyList<StructuralFieldDescriptor> fields,
        string name)
    {
        return fields.FirstOrDefault(field =>
            string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static int FindFieldIndex(
        IReadOnlyList<StructuralFieldDescriptor> fields,
        string name)
    {
        for (var index = 0; index < fields.Count; index++)
            if (string.Equals(fields[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;

        return -1;
    }
}