using System.Linq;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

internal static class StructuralCarrierDescriptors
{
    internal static StructuralTypeDescriptor FromShape(ExecutionStructuralShape shape) => shape.Kind switch
    {
        StructuralTypeKind.Scalar => StructuralTypeDescriptor.Scalar(
            System.Nullable.GetUnderlyingType(shape.ScalarType!.RequireClrType()) ?? shape.ScalarType!.RequireClrType(), shape.IsNullable),
        StructuralTypeKind.Record => StructuralTypeDescriptor.Record(typeof(StructuralValue),
            shape.Fields.Select(field => new StructuralFieldDescriptor(field.Name, FromShape(field.Shape!), field.Required)), shape.IsNullable),
        StructuralTypeKind.Collection => StructuralTypeDescriptor.Collection(shape.ElementType!.RequireClrType().MakeArrayType(),
            FromShape(shape.ElementShape!), shape.IsNullable),
        _ => throw new System.InvalidOperationException("A native carrier requires a closed structural shape.")
    };
}
