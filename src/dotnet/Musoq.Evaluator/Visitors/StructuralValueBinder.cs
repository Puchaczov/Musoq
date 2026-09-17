using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>Validates structural values and materializes typed collection storage.</summary>
internal static class StructuralValueBinder
{
    public static bool TryNormalize(
        object? rawValue,
        StructuralTypeDescriptor descriptor,
        string valueName,
        out object? normalizedValue,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        if (!TryNormalizeTree(rawValue, descriptor, valueName, out var tree, out error))
        {
            normalizedValue = null;
            return false;
        }

        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            normalizedValue = tree.Scalar;
            return true;
        }

        if (tree.Kind == StructuralTypeKind.Scalar && tree.Scalar == null)
        {
            normalizedValue = null;
            return true;
        }

        if (descriptor.Kind == StructuralTypeKind.Record)
        {
            normalizedValue = tree;
            return true;
        }

        try
        {
            normalizedValue = MaterializeCollection(tree, descriptor);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            normalizedValue = null;
            error = $"Structural value '{valueName}' could not be materialized: {exception.Message}";
            return false;
        }
    }

    private static bool TryNormalizeTree(
        object? rawValue,
        StructuralTypeDescriptor descriptor,
        string valueName,
        out StructuralValue tree,
        out string error)
    {
        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            var scalarValue = rawValue is StructuralValue scalarTree && scalarTree.Kind == StructuralTypeKind.Scalar
                ? scalarTree.Scalar
                : rawValue;
            var conversion = ScriptValueConverter.ConvertValue(
                "Structural value",
                valueName,
                descriptor.ToCanonicalSql(),
                descriptor.CoreValueType,
                scalarValue);
            if (!conversion.Success)
            {
                tree = null!;
                error = conversion.Error;
                return false;
            }

            tree = StructuralValue.FromScalar(conversion.Value);
            error = string.Empty;
            return true;
        }

        if (rawValue == null || rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
        {
            if (!descriptor.IsNullable)
            {
                tree = null!;
                error = $"Structural value '{valueName}' is null but type '{descriptor.ToCanonicalSql()}' is not nullable.";
                return false;
            }

            tree = StructuralValue.FromScalar(null);
            error = string.Empty;
            return true;
        }

        if (descriptor.Kind == StructuralTypeKind.Record)
            return TryNormalizeRecord(rawValue, descriptor, valueName, out tree, out error);

        return TryNormalizeCollection(rawValue, descriptor, valueName, out tree, out error);
    }

    private static bool TryNormalizeRecord(
        object rawValue,
        StructuralTypeDescriptor descriptor,
        string valueName,
        out StructuralValue tree,
        out string error)
    {

        if (rawValue is not StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            tree = null!;
            error = $"Structural value '{valueName}' must be a named record.";
            return false;
        }

        var fields = new List<KeyValuePair<string, StructuralValue>>(record.Fields.Count);
        foreach (var suppliedField in record.Fields)
        {
            var field = descriptor.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, suppliedField.Key, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                tree = null!;
                error = $"Structural value '{valueName}' contains unexpected field '{suppliedField.Key}'.";
                return false;
            }

            if (!TryNormalizeTree(
                    suppliedField.Value,
                    field.Type,
                    $"{valueName}.{field.Name}",
                    out var fieldTree,
                    out error))
            {
                tree = null!;
                return false;
            }

            fields.Add(new KeyValuePair<string, StructuralValue>(field.Name, fieldTree));
        }

        foreach (var field in descriptor.Fields)
        {
            if (record.Fields.ContainsKey(field.Name))
                continue;

            if (field.Required)
            {
                tree = null!;
                error = $"Structural value '{valueName}' is missing required field '{field.Name}'.";
                return false;
            }

            if (!field.HasDefault)
                continue;

            if (!TryNormalizeTree(
                    field.Default.Value,
                    field.Type,
                    $"{valueName}.{field.Name}",
                    out var defaultTree,
                    out error))
            {
                tree = null!;
                return false;
            }

            fields.Add(new KeyValuePair<string, StructuralValue>(field.Name, defaultTree));
        }

        tree = StructuralValue.FromRecord(fields);
        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeCollection(
        object rawValue,
        StructuralTypeDescriptor descriptor,
        string valueName,
        out StructuralValue tree,
        out string error)
    {
        if (rawValue is not StructuralValue { Kind: StructuralTypeKind.Collection } collection)
        {
            tree = null!;
            error = $"Structural value '{valueName}' must be an array.";
            return false;
        }

        var elements = new StructuralValue[collection.Elements.Count];
        for (var index = 0; index < collection.Elements.Count; index++)
        {
            if (!TryNormalizeTree(
                    collection.Elements[index],
                    descriptor.ElementType!,
                    $"{valueName}[{index}]",
                    out elements[index],
                    out error))
            {
                tree = null!;
                return false;
            }
        }

        tree = StructuralValue.FromCollection(elements);
        error = string.Empty;
        return true;
    }

    private static Array MaterializeCollection(
        StructuralValue tree,
        StructuralTypeDescriptor descriptor)
    {
        var elementType = descriptor.ElementType ?? throw new InvalidOperationException("Collection element type is missing.");
        var array = Array.CreateInstance(elementType.CoreValueType, tree.Elements.Count);
        for (var index = 0; index < tree.Elements.Count; index++)
        {
            var element = tree.Elements[index];
            object? value = elementType.Kind switch
            {
                StructuralTypeKind.Scalar => element.Scalar,
                StructuralTypeKind.Record => element.Kind == StructuralTypeKind.Scalar ? null : element,
                StructuralTypeKind.Collection => element.Kind == StructuralTypeKind.Scalar
                    ? null
                    : MaterializeCollection(element, elementType),
                _ => throw new InvalidOperationException("Unknown collection element kind.")
            };
            array.SetValue(value, index);
        }

        return array;
    }
}
