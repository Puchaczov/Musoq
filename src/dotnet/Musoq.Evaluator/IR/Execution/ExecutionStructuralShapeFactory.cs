using System.Collections.Generic;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Converts Schema structural descriptors into recursive portable shapes.</summary>
internal static class ExecutionStructuralShapeFactory
{
    public static ExecutionStructuralShape Create(StructuralTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => new ExecutionStructuralShape(
                StructuralTypeKind.Scalar,
                ExecutionClrBindingFactory.FromClr(GetClrType(descriptor)),
                null,
                null,
                descriptor.IsNullable),
            StructuralTypeKind.Record => CreateRecord(descriptor),
            StructuralTypeKind.Collection => CreateCollection(descriptor),
            _ => throw new InvalidOperationException($"Unknown structural type '{descriptor.Kind}'.")
        };
    }

    public static ExecutionStructuralFieldPlan CreateFieldPlan(StructuralFieldDescriptor field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return new ExecutionStructuralFieldPlan(
            field.Name,
            ExecutionClrBindingFactory.FromClr(GetClrType(field.Type)),
            field.Required,
            field.HasDefault,
            field.Default.CanonicalText,
            Create(field.Type));
    }

    private static ExecutionStructuralShape CreateRecord(StructuralTypeDescriptor descriptor)
    {
        var fields = new List<ExecutionStructuralFieldPlan>(descriptor.Fields.Count);
        foreach (var field in descriptor.Fields)
            fields.Add(CreateFieldPlan(field));

        return new ExecutionStructuralShape(
            StructuralTypeKind.Record,
            null,
            fields,
            null,
            descriptor.IsNullable);
    }

    private static ExecutionStructuralShape CreateCollection(StructuralTypeDescriptor descriptor)
    {
        var element = descriptor.ElementType ?? throw new InvalidOperationException(
            "A structural collection descriptor must have an element type.");
        return new ExecutionStructuralShape(
            StructuralTypeKind.Collection,
            null,
            null,
            ExecutionClrBindingFactory.FromClr(GetClrType(element)),
            descriptor.IsNullable,
            Create(element));
    }

    private static Type GetClrType(StructuralTypeDescriptor descriptor)
    {
        return descriptor.ClrType ?? descriptor.ScalarType ?? descriptor.CoreValueType;
    }
}
