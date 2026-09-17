using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.Visitors;
using Musoq.Schema.Reflection;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Helpers;

internal static class StructuralArgumentDescriptionFactory
{
    public static IReadOnlyList<StructuralArgumentDescription> Create(
        SchemaMethodInfo method,
        int overload)
    {
        ArgumentNullException.ThrowIfNull(method);
        return Create(SchemaSourceSignature.Create(method), overload);
    }

    public static IReadOnlyList<StructuralArgumentDescription> Create(
        SchemaSourceSignature signature,
        int overload)
    {
        ArgumentNullException.ThrowIfNull(signature);

        var rows = new List<StructuralArgumentDescription>();
        var limits = signature.Method.StructuralContract?.Limits ?? StructuralInputLimits.Default;

        foreach (var parameter in signature.Parameters)
        {
            var type = parameter.StructuralType ?? CreateDescriptor(parameter.ParameterType);
            AddType(
                rows,
                overload,
                parameter.Name,
                type,
                parameter.IsRequired,
                parameter.HasDefaultValue,
                parameter.HasDefaultValue ? StructuralSqlLiteralFormatter.Format(parameter.DefaultValue) : null,
                isRoot: true,
                limits);
        }

        return rows;
    }

    public static string CanonicalSignature(SchemaMethodInfo method)
    {
        var signature = SchemaSourceSignature.Create(method);
        return string.Join(
            "|",
            signature.Parameters.Select(parameter =>
                $"{parameter.Name}:{parameter.StructuralType?.ToCanonicalSql() ?? CreateDescriptor(parameter.ParameterType).ToCanonicalSql()}" +
                (parameter.HasDefaultValue
                    ? $"={StructuralSqlLiteralFormatter.Format(parameter.DefaultValue)}"
                    : string.Empty)));
    }

    private static void AddType(
        ICollection<StructuralArgumentDescription> rows,
        int overload,
        string path,
        StructuralTypeDescriptor type,
        bool? required,
        bool hasDefault,
        string? @default,
        bool isRoot,
        StructuralInputLimits limits)
    {
        var structuredRoot = isRoot && type.Kind != StructuralTypeKind.Scalar;
        rows.Add(new StructuralArgumentDescription(
            overload,
            path,
            type.Kind.ToString(),
            type.ToCanonicalSql(),
            required,
            type.IsNullable,
            hasDefault,
            @default,
            structuredRoot ? limits.MaxDepth : null,
            structuredRoot ? limits.MaxNodes : null,
            structuredRoot ? limits.MaxStringBytes : null));

        switch (type.Kind)
        {
            case StructuralTypeKind.Record:
                foreach (var field in type.Fields)
                {
                    AddType(
                        rows,
                        overload,
                        $"{path}.{field.Name}",
                        field.Type,
                        field.Required,
                        field.HasDefault,
                        field.HasDefault ? field.Default.CanonicalText : null,
                        isRoot: false,
                        limits);
                }

                break;
            case StructuralTypeKind.Collection:
                AddType(
                    rows,
                    overload,
                    $"{path}[]",
                    type.ElementType ?? throw new InvalidOperationException("Collection descriptor has no element type."),
                    required: null,
                    hasDefault: false,
                    @default: null,
                    isRoot: false,
                    limits);
                break;
        }
    }

    private static StructuralTypeDescriptor CreateDescriptor(Type type)
    {
        try
        {
            return StructuralTypeDescriptor.FromClrType(type, !type.IsValueType);
        }
        catch (InvalidOperationException)
        {
            return StructuralTypeDescriptor.Scalar(type, !type.IsValueType);
        }
    }
}
