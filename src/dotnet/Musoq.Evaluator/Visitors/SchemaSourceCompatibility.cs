using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaSourceCompatibility
{
    public static bool TryGetCost(
        Node argument,
        SchemaSourceParameter parameter,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
        Func<Node, CteRelationBinding?>? relationResolver,
        out int cost,
        out StructuralConstructionPlan? constructionPlan,
        out bool relationAmbiguous,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(parameter);
        cost = 0;
        constructionPlan = null;
        relationAmbiguous = false;
        error = string.Empty;

        if (relationResolver?.Invoke(argument) is { } relation)
            return TryGetRelationCost(argument, parameter, relation, out cost, out relationAmbiguous, out error);

        if (argument is NullNode)
            return TryGetNullCost(parameter, out cost, out error);

        if (parameter.StructuralType is { } receiver)
        {
            var supplied = structuralTypeResolver?.Invoke(argument);
            if (supplied != null)
            {
                if (!TryGetStructuralCost(supplied, receiver, out cost, out error))
                    return false;

                constructionPlan = CreateConstructionPlan(supplied, receiver);
                return true;
            }

            if (argument is RecordLiteralNode record)
                return TryBindRecordLiteral(record, receiver, out cost, out error);
            if (argument is ArrayLiteralNode array)
                return TryBindArrayLiteral(array, receiver, out cost, out error);
        }

        if (argument.ReturnType is not { } argumentType)
        {
            cost = 200;
            return true;
        }

        if (SchemaConversionClassifier.TryGetCost(argumentType, parameter.ParameterType, out cost))
            return true;

        error = $"Argument type '{argumentType.Name}' is incompatible with '{parameter.ParameterType.Name}'.";
        return false;
    }

    internal static bool TryGetRelationCost(
        Node argument,
        SchemaSourceParameter parameter,
        CteRelationBinding relation,
        out int cost,
        out bool relationAmbiguous,
        out string error)
    {
        relationAmbiguous = false;
        var scalarCompatible = TryGetScalarCost(
            argument,
            parameter,
            relation,
            out _,
            out _);
        var relationCompatible = TryGetRelationOnlyCost(relation, parameter, out _, out error);
        if (relationCompatible && scalarCompatible)
        {
            relationAmbiguous = true;
            cost = 0;
            error = $"Datasource argument '{relation.Name}' is ambiguous between a scalar column and a complete CTE relation.";
            return false;
        }

        if (!relationCompatible)
        {
            cost = SchemaConversionClassifier.Incompatible;
            return false;
        }

        cost = 0;
        relationAmbiguous = false;
        return true;
    }

    /// <summary>
    /// Checks only the complete-relation interpretation of a CTE argument. This
    /// is separate from <see cref="TryGetRelationCost"/> so the caller can
    /// compare relation and scalar applicability across the complete overload
    /// set before committing to a candidate.
    /// </summary>
    internal static bool TryGetRelationOnlyCost(
        CteRelationBinding relation,
        SchemaSourceParameter parameter,
        out int cost,
        out string error)
    {
        if (SchemaSourceArgumentBinder.TryValidateRelation(relation, parameter, out error))
        {
            cost = 0;
            return true;
        }

        cost = SchemaConversionClassifier.Incompatible;
        return false;
    }

    /// <summary>
    /// Checks the scalar interpretation of an argument. When a relation is
    /// supplied, only its explicitly resolved scalar-column type is eligible;
    /// a relation's row type must never masquerade as a scalar column.
    /// </summary>
    internal static bool TryGetScalarCost(
        Node argument,
        SchemaSourceParameter parameter,
        CteRelationBinding? relation,
        out int cost,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(parameter);

        var argumentType = relation?.ScalarType ?? (relation == null ? argument.ReturnType : null);
        if (argumentType == null)
        {
            cost = SchemaConversionClassifier.Incompatible;
            error = relation == null
                ? "The scalar argument has no resolved type."
                : $"CTE relation '{relation.Name}' has no visible scalar column interpretation.";
            return false;
        }

        // A visible scalar column is already a complete CLR value. Its
        // compatibility is checked against the receiving parameter itself,
        // including collection-valued columns. It must not be interpreted as
        // a structural literal that needs element-wise preparation.
        if (relation != null)
        {
            if (SchemaConversionClassifier.TryGetCost(argumentType, parameter.ParameterType, out cost))
            {
                error = string.Empty;
                return true;
            }

            error = $"Argument type '{argumentType.Name}' is incompatible with '{parameter.ParameterType.Name}'.";
            return false;
        }

        if (parameter.StructuralType is { } receiver)
        {
            if (receiver.Kind != StructuralTypeKind.Scalar || receiver.ScalarType == null)
            {
                cost = SchemaConversionClassifier.Incompatible;
                error = "A scalar interpretation requires a scalar receiving contract.";
                return false;
            }

            if (SchemaConversionClassifier.TryGetCost(argumentType, receiver.ScalarType, out cost))
            {
                error = string.Empty;
                return true;
            }

            error = $"Argument type '{argumentType.Name}' is incompatible with '{receiver.ToCanonicalSql()}'.";
            return false;
        }

        if (SchemaConversionClassifier.TryGetCost(argumentType, parameter.ParameterType, out cost))
        {
            error = string.Empty;
            return true;
        }

        error = $"Argument type '{argumentType.Name}' is incompatible with '{parameter.ParameterType.Name}'.";
        return false;
    }

    private static bool TryGetNullCost(
        SchemaSourceParameter parameter,
        out int cost,
        out string error)
    {
        if (!parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) != null)
        {
            cost = 0;
            error = string.Empty;
            return true;
        }

        cost = SchemaConversionClassifier.Incompatible;
        error = $"Null is incompatible with '{parameter.ParameterType.Name}'.";
        return false;
    }

    private static bool TryBindRecordLiteral(
        RecordLiteralNode record,
        StructuralTypeDescriptor receiver,
        out int cost,
        out string error)
    {
        if (receiver.Kind != StructuralTypeKind.Record)
        {
            cost = SchemaConversionClassifier.Incompatible;
            error = "A record literal requires a record receiving contract.";
            return false;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        cost = 1;
        foreach (var field in record.Fields)
        {
            if (!seen.Add(field.Name))
            {
                error = $"Structural field '{field.Name}' was supplied more than once.";
                return false;
            }

            var receiverField = receiver.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (receiverField == null)
            {
                error = $"Structural input contains unexpected field '{field.Name}'.";
                return false;
            }

            if (!TryBindNode(field.Expression, receiverField.Type, out var fieldCost, out error))
                return false;
            cost += fieldCost;
        }

        foreach (var receiverField in receiver.Fields)
        {
            if (!seen.Contains(receiverField.Name) && receiverField.Required)
            {
                error = $"Structural input is missing required field '{receiverField.Name}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryBindArrayLiteral(
        ArrayLiteralNode array,
        StructuralTypeDescriptor receiver,
        out int cost,
        out string error)
    {
        if (receiver.Kind != StructuralTypeKind.Collection || receiver.ElementType == null)
        {
            cost = SchemaConversionClassifier.Incompatible;
            error = "An array literal requires a collection receiving contract.";
            return false;
        }

        cost = 1;
        foreach (var element in array.Elements)
        {
            if (!TryBindNode(element, receiver.ElementType, out var elementCost, out error))
                return false;
            cost += elementCost;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryBindNode(
        Node expression,
        StructuralTypeDescriptor receiver,
        out int cost,
        out string error)
    {
        if (expression is NullNode)
            return TryGetNullDescriptorCost(receiver, out cost, out error);

        if (expression is RecordLiteralNode record)
            return TryBindRecordLiteral(record, receiver, out cost, out error);
        if (expression is ArrayLiteralNode array)
            return TryBindArrayLiteral(array, receiver, out cost, out error);

        if (expression.ReturnType is not { } actualType)
        {
            cost = 200;
            error = string.Empty;
            return true;
        }

        if (receiver.Kind != StructuralTypeKind.Scalar || receiver.ScalarType == null ||
            !SchemaConversionClassifier.TryGetCost(actualType, receiver.ScalarType, out cost))
        {
            error = $"Expression type '{actualType.Name}' is incompatible with '{receiver.ToCanonicalSql()}'.";
            cost = SchemaConversionClassifier.Incompatible;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryGetNullDescriptorCost(
        StructuralTypeDescriptor receiver,
        out int cost,
        out string error)
    {
        if (receiver.IsNullable)
        {
            cost = 0;
            error = string.Empty;
            return true;
        }

        cost = SchemaConversionClassifier.Incompatible;
        error = $"Null is incompatible with '{receiver.ToCanonicalSql()}'.";
        return false;
    }

    private static bool TryGetStructuralCost(
        StructuralTypeDescriptor supplied,
        StructuralTypeDescriptor receiver,
        out int cost,
        out string error)
    {
        cost = 0;
        if (supplied.Kind != receiver.Kind)
        {
            error = $"Structural input kind '{supplied.Kind}' is incompatible with receiver kind '{receiver.Kind}'.";
            return false;
        }

        if (supplied.IsNullable && !receiver.IsNullable)
        {
            error = "A nullable structural value cannot be passed to a non-nullable receiver.";
            return false;
        }

        switch (supplied.Kind)
        {
            case StructuralTypeKind.Scalar:
                if (supplied.ScalarType == null || receiver.ScalarType == null ||
                    !SchemaConversionClassifier.TryGetCost(supplied.ScalarType, receiver.ScalarType, out cost))
                {
                    error = $"Structural scalar '{supplied.ToCanonicalSql()}' is incompatible with receiver '{receiver.ToCanonicalSql()}'.";
                    return false;
                }
                error = string.Empty;
                return true;
            case StructuralTypeKind.Collection:
                return TryGetStructuralCost(supplied.ElementType!, receiver.ElementType!, out cost, out error);
            case StructuralTypeKind.Record:
                foreach (var suppliedField in supplied.Fields)
                {
                    var receiverField = receiver.Fields.FirstOrDefault(candidate =>
                        string.Equals(candidate.Name, suppliedField.Name, StringComparison.OrdinalIgnoreCase));
                    if (receiverField == null)
                    {
                        error = $"Structural input contains unexpected field '{suppliedField.Name}'.";
                        return false;
                    }

                    if (!TryGetStructuralCost(suppliedField.Type, receiverField.Type, out var fieldCost, out error))
                        return false;
                    cost += fieldCost;
                }

                foreach (var receiverField in receiver.Fields)
                {
                    if (supplied.Fields.All(field => !string.Equals(field.Name, receiverField.Name, StringComparison.OrdinalIgnoreCase)) &&
                        receiverField.Required)
                    {
                        error = $"Structural input is missing required field '{receiverField.Name}'.";
                        return false;
                    }
                }

                error = string.Empty;
                return true;
            default:
                error = "Unsupported structural kind.";
                return false;
        }
    }

    private static StructuralConstructionPlan? CreateConstructionPlan(
        StructuralTypeDescriptor supplied,
        StructuralTypeDescriptor receiver)
    {
        if (supplied.Kind == StructuralTypeKind.Record && receiver.Kind == StructuralTypeKind.Record &&
            StructuralConstructionPlanBinder.TryBind(supplied, receiver, out var plan, out _))
            return plan;

        if (supplied.Kind == StructuralTypeKind.Collection && supplied.ElementType is { Kind: StructuralTypeKind.Record } suppliedElement &&
            receiver.ElementType is { Kind: StructuralTypeKind.Record } receiverElement &&
            StructuralConstructionPlanBinder.TryBind(suppliedElement, receiverElement, out var elementPlan, out _))
            return elementPlan;

        return null;
    }
}
