using System;

namespace Musoq.Targets.Abstractions;

/// <summary>
/// Compares portable symbol descriptors by their complete semantic definition.
/// </summary>
internal static class ExecutionPortableSymbolDefinitionComparer
{
    public static bool AreEquivalent(
        ExecutionPortableTypeDescriptor? left,
        ExecutionPortableTypeDescriptor? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null ||
            left.Kind != right.Kind ||
            !string.Equals(left.StableName, right.StableName, StringComparison.Ordinal) ||
            !string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) ||
            left.Portability != right.Portability ||
            !string.Equals(left.PortabilityReason, right.PortabilityReason, StringComparison.Ordinal) ||
            left.ArrayRank != right.ArrayRank ||
            !AreEquivalent(left.Container, right.Container) ||
            !AreEquivalent(left.Arguments, right.Arguments) ||
            !AreEquivalent(left.Fields, right.Fields))
        {
            return false;
        }

        return true;
    }

    public static bool AreEquivalent(
        ExecutionPortableCallableDescriptor? left,
        ExecutionPortableCallableDescriptor? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null ||
            left.Kind != right.Kind ||
            !string.Equals(left.StableName, right.StableName, StringComparison.Ordinal) ||
            !string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) ||
            left.Portability != right.Portability ||
            !string.Equals(left.PortabilityReason, right.PortabilityReason, StringComparison.Ordinal) ||
            !string.Equals(left.MethodName, right.MethodName, StringComparison.Ordinal) ||
            left.IsStatic != right.IsStatic ||
            left.GenericArity != right.GenericArity ||
            left.InvocationMode != right.InvocationMode ||
            left.IntrinsicKind != right.IntrinsicKind ||
            left.IsStable != right.IsStable ||
            !AreEquivalent(left.DeclaringType, right.DeclaringType) ||
            !AreEquivalent(left.ReturnType, right.ReturnType) ||
            !AreEquivalent(left.ParameterTypes, right.ParameterTypes))
        {
            return false;
        }

        return true;
    }

    private static bool AreEquivalent(
        ExecutionPortableContainerContract? left,
        ExecutionPortableContainerContract? right)
    {
        return left == null
            ? right == null
            : right != null &&
              left.Kind == right.Kind &&
              left.IsOrdered == right.IsOrdered &&
              left.IsMutable == right.IsMutable &&
              left.RequiresKeyEquality == right.RequiresKeyEquality &&
              left.RequiresKeyHashing == right.RequiresKeyHashing &&
              left.BindingKind == right.BindingKind;
    }

    private static bool AreEquivalent(
        System.Collections.Generic.IReadOnlyList<ExecutionPortableTypeDescriptor> left,
        System.Collections.Generic.IReadOnlyList<ExecutionPortableTypeDescriptor> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            if (!AreEquivalent(left[index], right[index]))
                return false;
        }

        return true;
    }

    private static bool AreEquivalent(
        System.Collections.Generic.IReadOnlyList<ExecutionPortableRowFieldDescriptor> left,
        System.Collections.Generic.IReadOnlyList<ExecutionPortableRowFieldDescriptor> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            var leftField = left[index];
            var rightField = right[index];
            if (!string.Equals(leftField.Name, rightField.Name, StringComparison.Ordinal) ||
                !string.Equals(leftField.Nullability, rightField.Nullability, StringComparison.Ordinal) ||
                !AreEquivalent(leftField.Type, rightField.Type))
            {
                return false;
            }
        }

        return true;
    }

}
