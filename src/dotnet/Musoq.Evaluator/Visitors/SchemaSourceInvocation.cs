using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Metadata for one public datasource argument. The metadata is derived from the
///     table constructor exposed by <see cref="ISchema.GetRawConstructors"/>.
/// </summary>
internal sealed record SchemaSourceParameter(
    string Name,
    Type ParameterType,
    bool HasDefaultValue,
    object? DefaultValue)
{
    public bool IsRequired => !HasDefaultValue;
}

/// <summary>
///     A reflected datasource signature with source-visible parameters only.
/// </summary>
internal sealed class SchemaSourceSignature
{
    private SchemaSourceSignature(
        SchemaMethodInfo method,
        ImmutableArray<SchemaSourceParameter> parameters,
        bool canBindNamedArguments)
    {
        Method = method;
        Parameters = parameters;
        CanBindNamedArguments = canBindNamedArguments;
    }

    public SchemaMethodInfo Method { get; }

    public ImmutableArray<SchemaSourceParameter> Parameters { get; }

    public bool CanBindNamedArguments { get; }

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
                                   SchemaSourceDefaultFormatter.IsUsable(reflected.DefaultValue) &&
                                   SchemaSourceDefaultFormatter.IsCompatible(reflected.DefaultValue, argument.Type);

            parameters.Add(new SchemaSourceParameter(
                argument.Name,
                argument.Type,
                hasDefaultValue,
                hasDefaultValue ? reflected!.DefaultValue : null));
        }

        return new SchemaSourceSignature(method, parameters.MoveToImmutable(), canBindNamedArguments);
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
    object? DefaultValue)
{
    public bool UsesDefault => SourceArgumentIndex is null;
}

internal sealed class BoundSchemaInvocation
{
    public BoundSchemaInvocation(
        SchemaSourceSignature signature,
        IEnumerable<BoundSchemaArgument> arguments,
        bool usesNamedArguments)
    {
        Signature = signature;
        Arguments = arguments.ToImmutableArray();
        UsesNamedArguments = usesNamedArguments;
    }

    public SchemaSourceSignature Signature { get; }

    public ImmutableArray<BoundSchemaArgument> Arguments { get; }

    public bool UsesNamedArguments { get; }

    public bool HasDefaults => Arguments.Any(static argument => argument.UsesDefault);
}

internal static class SchemaSourceDefaultFormatter
{
    public static bool IsUsable(object? value) =>
        !Equals(value, Missing.Value) && !Equals(value, DBNull.Value);

    public static bool IsCompatible(object? value, Type parameterType)
    {
        if (value is null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        return parameterType.IsInstanceOfType(value);
    }

    public static string Format(object? value)
    {
        if (value is null)
            return "null";

        if (value is string text)
            return $"'{Escape(text)}'";

        if (value is char character)
            return $"'{Escape(character.ToString())}'";

        if (value is bool boolean)
            return boolean ? "true" : "false";

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString()!;
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "''", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal);
}
