using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.Reflection;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Metadata for one public datasource argument. The metadata is derived from the
///     table constructor exposed by <see cref="ISchema.GetRawConstructors"/>.
/// </summary>
internal sealed record SchemaSourceParameter(
    string Name,
    Type ParameterType,
    bool HasDefaultValue,
    object? DefaultValue,
    StructuralTypeDescriptor? StructuralType = null,
    StructuralInputLimits? StructuralLimits = null)
{
    public bool IsRequired => !HasDefaultValue;
}
/// <summary>
///     Metadata for a complete CTE relation used as a datasource argument. A relation
///     binding is deliberately separate from an ordinary expression so a failed
///     structural conversion can never fall back to a scalar or string argument.
/// </summary>
internal sealed record CteRelationBinding(
    string Name,
    IReadOnlyList<ISchemaColumn> Columns,
    StructuralTypeDescriptor Shape,
    Type? ScalarType = null);

/// <summary>
///     A reflected datasource signature with source-visible parameters only.
/// </summary>
internal sealed class SchemaSourceSignature
{
    private SchemaSourceSignature(
        SchemaMethodInfo method,
        ImmutableArray<SchemaSourceParameter> parameters,
        bool canBindNamedArguments,
        Type? sourceConstructionType,
        bool supportsExecutionContext)
    {
        Method = method;
        Parameters = parameters;
        CanBindNamedArguments = canBindNamedArguments;
        SourceConstructionType = sourceConstructionType;
        SupportsExecutionContext = supportsExecutionContext;
        SourceStableId = method.ConstructorInfo.SourceStableId;
        SourceConstructor = method.ConstructorInfo.OriginConstructor;
    }

    public SchemaMethodInfo Method { get; }

    public ImmutableArray<SchemaSourceParameter> Parameters { get; }

    public bool CanBindNamedArguments { get; }

    public Type? SourceConstructionType { get; }

    public bool SupportsExecutionContext { get; }

    public string? SourceStableId { get; }

    public System.Reflection.ConstructorInfo? SourceConstructor { get; }

    public bool HasStructuralParameters => Parameters.Any(static parameter => parameter.StructuralType != null);
    public static SchemaSourceSignature Create(SchemaMethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var arguments = method.ConstructorInfo.Arguments ?? [];
        var originParameters = method.ConstructorInfo.OriginConstructor?
            .GetParameters()
            .Where(static parameter => parameter.ParameterType != typeof(SourceExecutionContext))
            .ToArray();

        var canBindNamedArguments = method.ConstructorInfo.OriginConstructor != null &&
                                    arguments.Length > 0 &&
                                    arguments.All(static argument => !string.IsNullOrWhiteSpace(argument.Name));

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (canBindNamedArguments)
            foreach (var argument in arguments)
                if (!names.Add(argument.Name))
                    canBindNamedArguments = false;

        var reflectionOrderMatchesMetadata = originParameters is null || originParameters.Length == arguments.Length;
        if (reflectionOrderMatchesMetadata && originParameters != null)
        {
            for (var index = 0; index < arguments.Length; index++)
            {
                if (originParameters[index].ParameterType != arguments[index].Type ||
                    !string.Equals(originParameters[index].Name, arguments[index].Name, StringComparison.Ordinal))
                {
                    reflectionOrderMatchesMetadata = false;
                    break;
                }
            }
        }

        canBindNamedArguments &= reflectionOrderMatchesMetadata;

        var structuralParameters = method.StructuralContract?.Parameters;
        var parameters = ImmutableArray.CreateBuilder<SchemaSourceParameter>(arguments.Length);
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            var reflected = reflectionOrderMatchesMetadata && originParameters is { Length: > 0 } && index < originParameters.Length
                ? originParameters[index]
                : null;

            var hasDefaultValue = reflected is not null &&
                                   reflected.IsOptional &&
                                   reflected.HasDefaultValue &&
                                   SchemaSourceDefaultCompatibility.IsUsable(reflected.DefaultValue) &&
                                   SchemaSourceDefaultCompatibility.IsCompatible(reflected.DefaultValue, argument.Type);

            parameters.Add(new SchemaSourceParameter(
                argument.Name,
                argument.Type,
                hasDefaultValue,
                hasDefaultValue ? reflected!.DefaultValue : null,
                structuralParameters != null && index < structuralParameters.Count
                    ? structuralParameters[index].Type
                    : null,
                method.StructuralContract?.Limits));
        }

        return new SchemaSourceSignature(
            method,
            parameters.MoveToImmutable(),
            canBindNamedArguments,
            method.StructuralContract == null ? null : method.ConstructorInfo.OriginConstructor?.DeclaringType,
            method.ConstructorInfo.SupportsInterCommunicator);
    }
}

/// <summary>
///     Canonical mapping from source argument expressions to one datasource signature.
///     Slots refer to the original argument list by index so AST rewrites do not leave
///     stale expression references inside the binding.
/// </summary>
internal sealed record BoundSchemaArgument(
    int ParameterIndex,
    int? SourceArgumentIndex,
    object? DefaultValue,
    CteRelationBinding? Relation = null,
    StructuralConstructionPlan? ConstructionPlan = null)
{
    public bool UsesDefault => SourceArgumentIndex is null && Relation is null;

    public bool UsesRelation => Relation is not null;
}

internal sealed class BoundSchemaInvocation
{
    public BoundSchemaInvocation(
        SchemaSourceSignature signature,
        IEnumerable<BoundSchemaArgument> arguments,
        bool usesNamedArguments,
        int overloadIndex = 0)
    {
        Signature = signature;
        Arguments = arguments.ToImmutableArray();
        UsesNamedArguments = usesNamedArguments;
        OverloadIndex = overloadIndex;
    }

    public SchemaSourceSignature Signature { get; }

    public ImmutableArray<BoundSchemaArgument> Arguments { get; }

    public bool UsesNamedArguments { get; }

    public int OverloadIndex { get; }

    public bool HasDefaults => Arguments.Any(static argument => argument.UsesDefault);

    public bool HasRelations => Arguments.Any(static argument => argument.UsesRelation);
}

internal static class SchemaSourceDefaultCompatibility
{
    public static bool IsUsable(object? value) =>
        !Equals(value, Missing.Value) && !Equals(value, DBNull.Value);

    public static bool IsCompatible(object? value, Type parameterType)
    {
        if (value is null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        return parameterType.IsInstanceOfType(value);
    }

}
