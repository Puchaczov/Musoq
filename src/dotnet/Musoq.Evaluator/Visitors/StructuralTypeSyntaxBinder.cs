using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>Maps parser structural declarations to immutable Core shape descriptors.</summary>
internal static class StructuralTypeSyntaxBinder
{
    public static bool TryBind(
        StructuralTypeSyntaxNode syntax,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        return TryBindCore(syntax, out descriptor, out error);
    }

    private static bool TryBindCore(
        StructuralTypeSyntaxNode syntax,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        error = string.Empty;
        descriptor = null!;

        switch (syntax.Kind)
        {
            case StructuralTypeSyntaxKind.Scalar:
                if (!ScriptParameterTypeCatalog.TryResolveScalar(syntax.ScalarName!, out var scalar))
                {
                    error = $"Structural type '{syntax.ScalarName}' is not a supported Core scalar type.";
                    return false;
                }

                descriptor = StructuralTypeDescriptor.Scalar(
                    scalar.ClrType,
                    syntax.IsNullable || !scalar.ClrType.IsValueType);
                return true;

            case StructuralTypeSyntaxKind.Record:
                return TryBindRecord(syntax, out descriptor, out error);

            case StructuralTypeSyntaxKind.Collection:
                if (syntax.ElementType == null || !TryBindCore(syntax.ElementType, out var element, out error))
                    return false;

                descriptor = StructuralTypeDescriptor.Collection(
                    element.CoreValueType.MakeArrayType(),
                    element,
                    syntax.IsNullable);
                return true;

            default:
                error = $"Structural type kind '{syntax.Kind}' is not supported.";
                return false;
        }
    }

    private static bool TryBindRecord(
        StructuralTypeSyntaxNode syntax,
        out StructuralTypeDescriptor descriptor,
        out string error)
    {
        var fields = new List<StructuralFieldDescriptor>(syntax.Fields.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in syntax.Fields)
        {
            if (!names.Add(field.Name))
            {
                descriptor = null!;
                error = $"Structural type field '{field.Name}' is declared more than once.";
                return false;
            }

            if (!TryBindCore(field.Type, out var fieldType, out error))
            {
                descriptor = null!;
                return false;
            }

            var fieldDefault = StructuralDefaultDescriptor.Absent;
            if (field.DefaultValue != null)
            {
                if (field.DefaultValue is not ConstantValueNode and not NullNode)
                {
                    descriptor = null!;
                    error = $"Default for structural field '{field.Name}' must be a primitive constant or null.";
                    return false;
                }

                var rawValue = field.DefaultValue is NullNode
                    ? null
                    : ((ConstantValueNode)field.DefaultValue).ObjValue;
                var conversion = ScriptValueConverter.ConvertValue(
                    "Structural field",
                    field.Name,
                    fieldType.ToCanonicalSql(),
                    fieldType.CoreValueType,
                    rawValue);
                if (!conversion.Success)
                {
                    descriptor = null!;
                    error = conversion.Error;
                    return false;
                }

                fieldDefault = StructuralDefaultDescriptor.Create(
                    conversion.Value,
                    field.DefaultValue.ToString());
            }

            fields.Add(new StructuralFieldDescriptor(
                field.Name,
                fieldType,
                !field.HasDefault,
                fieldDefault));
        }

        descriptor = StructuralTypeDescriptor.Record(
            typeof(StructuralValue),
            fields,
            syntax.IsNullable);
        error = string.Empty;
        return true;
    }
}