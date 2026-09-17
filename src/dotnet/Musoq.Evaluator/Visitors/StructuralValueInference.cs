using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>Infers structural shapes while keeping omitted record fields absent.</summary>
internal static class StructuralValueInference
{
    public static bool TryInfer(
        object? value,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        if (value is not StructuralValue structural || structural.Kind == StructuralTypeKind.Scalar)
        {
            descriptor = null!;
            error = "A structural value must be a record or collection.";
            return false;
        }

        return TryInferValue(structural, out descriptor, out error);
    }

    private static bool TryInferValue(
        StructuralValue value,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        switch (value.Kind)
        {
            case StructuralTypeKind.Scalar:
                return TryInferScalar(value.Scalar, out descriptor, out error);
            case StructuralTypeKind.Record:
                return TryInferRecord(value, out descriptor, out error);
            case StructuralTypeKind.Collection:
                return TryInferCollection(value, out descriptor, out error);
            default:
                descriptor = null!;
                error = $"Structural value kind '{value.Kind}' is not supported.";
                return false;
        }
    }

    private static bool TryInferScalar(
        object? value,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        if (value == null)
        {
            descriptor = null!;
            error = "A null-only structural value does not provide enough type information.";
            return false;
        }

        try
        {
            descriptor = StructuralTypeDescriptor.FromClrType(value.GetType(), !value.GetType().IsValueType);
            error = string.Empty;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            descriptor = null!;
            error = $"Value of type '{value.GetType().Name}' is not a supported structural scalar: {exception.Message}";
            return false;
        }
    }

    private static bool TryInferRecord(
        StructuralValue value,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        var fields = new List<StructuralFieldDescriptor>(value.Fields.Count);
        foreach (var field in value.Fields)
        {
            if (!TryInferValue(field.Value, out var fieldType, out error))
            {
                descriptor = null!;
                error = $"Field '{field.Key}' cannot be inferred: {error}";
                return false;
            }

            fields.Add(new StructuralFieldDescriptor(field.Key, fieldType, true));
        }

        descriptor = StructuralTypeDescriptor.Record(typeof(StructuralValue), fields);
        error = string.Empty;
        return true;
    }

    private static bool TryInferCollection(
        StructuralValue value,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        if (value.Elements.Count == 0)
        {
            descriptor = null!;
            error = "An empty structural array needs an expected element type.";
            return false;
        }

        var nonNullElements = value.Elements
            .Where(static element => element.Kind != StructuralTypeKind.Scalar || element.Scalar != null)
            .ToArray();
        if (nonNullElements.Length == 0)
        {
            descriptor = null!;
            error = "An all-null structural array needs an expected element type.";
            return false;
        }

        if (nonNullElements.All(static element => element.Kind == StructuralTypeKind.Record))
        {
            if (!TryInferRecordCollection(value.Elements, out var recordElement, out error))
            {
                descriptor = null!;
                return false;
            }

            descriptor = StructuralTypeDescriptor.Collection(
                recordElement.CoreValueType.MakeArrayType(),
                recordElement);
            return true;
        }

        var elementDescriptors = new List<StructuralTypeDescriptor>(nonNullElements.Length);
        foreach (var element in nonNullElements)
        {
            if (!TryInferValue(element, out var elementType, out error))
            {
                descriptor = null!;
                return false;
            }

            elementDescriptors.Add(elementType);
        }

        if (elementDescriptors.Any(element => element.Kind != elementDescriptors[0].Kind))
        {
            descriptor = null!;
            error = "A structural array cannot mix record, collection, and scalar elements.";
            return false;
        }

        if (!TryMerge(elementDescriptors, out var mergedElement, out error))
        {
            descriptor = null!;
            return false;
        }

        if (value.Elements.Any(static element => element.Kind == StructuralTypeKind.Scalar && element.Scalar == null))
            mergedElement = MakeNullable(mergedElement);

        descriptor = StructuralTypeDescriptor.Collection(
            mergedElement.CoreValueType.MakeArrayType(),
            mergedElement);
        return true;
    }

    private static bool TryInferRecordCollection(
        IReadOnlyList<StructuralValue> elements,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
            foreach (var field in element.Fields.Keys)
                names.Add(field);

        var fields = new List<StructuralFieldDescriptor>(names.Count);
        foreach (var name in names.OrderBy(static name => name, StringComparer.Ordinal))
        {
            var suppliedValues = new List<StructuralValue>();
            var suppliedByAll = true;
            var hasNull = false;
            foreach (var element in elements)
            {
                if (!element.Fields.TryGetValue(name, out var fieldValue))
                {
                    suppliedByAll = false;
                    continue;
                }

                suppliedValues.Add(fieldValue);
                hasNull |= fieldValue.Kind == StructuralTypeKind.Scalar && fieldValue.Scalar == null;
            }

            var nonNullValues = suppliedValues
                .Where(static field => field.Kind != StructuralTypeKind.Scalar || field.Scalar != null)
                .ToArray();
            if (nonNullValues.Length == 0)
            {
                descriptor = null!;
                error = $"Record field '{name}' is null in every supplied element and cannot be inferred.";
                return false;
            }

            var candidates = new List<StructuralTypeDescriptor>(nonNullValues.Length);
            foreach (var fieldValue in nonNullValues)
            {
                if (!TryInferValue(fieldValue, out var fieldType, out error))
                {
                    descriptor = null!;
                    error = $"Field '{name}' cannot be inferred: {error}";
                    return false;
                }

                candidates.Add(fieldType);
            }

            if (!TryMerge(candidates, out var fieldTypeResult, out error))
            {
                descriptor = null!;
                error = $"Record field '{name}' has incompatible element types: {error}";
                return false;
            }

            if (hasNull)
                fieldTypeResult = MakeNullable(fieldTypeResult);

            fields.Add(new StructuralFieldDescriptor(name, fieldTypeResult, suppliedByAll));
        }

        descriptor = StructuralTypeDescriptor.Record(typeof(StructuralValue), fields);
        error = string.Empty;
        return true;
    }

    private static bool TryMerge(
        IReadOnlyList<StructuralTypeDescriptor> candidates,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        if (candidates.Count == 0)
        {
            descriptor = null!;
            error = "No structural type candidates were supplied.";
            return false;
        }

        var first = candidates[0];
        if (candidates.Any(candidate => candidate.Kind != first.Kind))
        {
            descriptor = null!;
            error = "Structural values have incompatible kinds.";
            return false;
        }

        switch (first.Kind)
        {
            case StructuralTypeKind.Scalar:
                try
                {
                    var nodes = candidates
                        .Select(candidate => (Node)new InferredTypeNode(candidate.ScalarType!, candidate.IsNullable))
                        .ToArray();
                    var common = CommonColumnTypeResolver.Resolve(
                        "structural value",
                        nodes,
                        default,
                        CommonColumnTypeDiagnosticKind.Values);
                    var scalar = Nullable.GetUnderlyingType(common) ?? common;
                    var nullable = candidates.Any(static candidate => candidate.IsNullable) || !scalar.IsValueType;
                    descriptor = StructuralTypeDescriptor.Scalar(scalar, nullable);
                    error = string.Empty;
                    return true;
                }
                catch (Exception exception) when (exception is ValuesSourceException or InvalidOperationException)
                {
                    descriptor = null!;
                    error = exception.Message;
                    return false;
                }

            case StructuralTypeKind.Record:
                return TryMergeRecords(candidates, out descriptor, out error);

            case StructuralTypeKind.Collection:
                if (!TryMerge(candidates.Select(static candidate => candidate.ElementType!).ToArray(), out var element, out error))
                {
                    descriptor = null!;
                    return false;
                }

                descriptor = StructuralTypeDescriptor.Collection(
                    element.CoreValueType.MakeArrayType(),
                    element,
                    candidates.Any(static candidate => candidate.IsNullable));
                return true;

            default:
                descriptor = null!;
                error = $"Structural kind '{first.Kind}' is not supported.";
                return false;
        }
    }

    private static bool TryMergeRecords(
        IReadOnlyList<StructuralTypeDescriptor> candidates,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
            foreach (var field in candidate.Fields)
                names.Add(field.Name);

        var fields = new List<StructuralFieldDescriptor>(names.Count);
        foreach (var name in names.OrderBy(static name => name, StringComparer.Ordinal))
        {
            var present = candidates
                .Select(candidate => candidate.Fields.FirstOrDefault(field =>
                    string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)))
                .Where(static field => field != null)
                .Cast<StructuralFieldDescriptor>()
                .ToArray();
            if (!TryMerge(present.Select(static field => field.Type).ToArray(), out var fieldType, out error))
            {
                descriptor = null!;
                return false;
            }

            fields.Add(new StructuralFieldDescriptor(
                present[0].Name,
                fieldType,
                present.Length == candidates.Count && present.All(static field => field.Required)));
        }

        descriptor = StructuralTypeDescriptor.Record(typeof(StructuralValue), fields, candidates.Any(static candidate => candidate.IsNullable));
        error = string.Empty;
        return true;
    }

    private static StructuralTypeDescriptor MakeNullable(StructuralTypeDescriptor descriptor)
    {
        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => StructuralTypeDescriptor.Scalar(descriptor.ScalarType!, true),
            StructuralTypeKind.Record => StructuralTypeDescriptor.Record(typeof(StructuralValue), descriptor.Fields, true),
            StructuralTypeKind.Collection => StructuralTypeDescriptor.Collection(
                descriptor.ElementType!.CoreValueType.MakeArrayType(),
                descriptor.ElementType,
                true),
            _ => throw new InvalidOperationException($"Unknown structural kind '{descriptor.Kind}'.")
        };
    }

    private sealed class InferredTypeNode(Type type, bool nullable) : Node
    {
        public override Type ReturnType { get; } = nullable && type.IsValueType
            ? typeof(Nullable<>).MakeGenericType(type)
            : type;

        public override string Id => nameof(InferredTypeNode);

        public override void Accept(IExpressionVisitor visitor)
        {
            ArgumentNullException.ThrowIfNull(visitor);
            visitor.Visit((Node)this);
        }

        public override string ToString() => ReturnType.Name;
    }
}