using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Classifies a failed callable lookup after the owner has already been selected.
///     This intentionally describes the lookup contract without changing the schema's
///     existing method-selection implementation.
/// </summary>
internal static class CallableResolutionDiagnostics
{
    private const int CandidateLimit = 5;

    public static CannotResolveMethodException CreateException(
        ISchema schema,
        string callableName,
        IReadOnlyList<Type> argumentTypes,
        Type? entityType,
        TextSpan span,
        IReadOnlyList<Node>? argumentNodes = null,
        Func<MethodInfo, bool>? methodFilter = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(callableName);
        ArgumentNullException.ThrowIfNull(argumentTypes);

        var failure = Classify(schema, callableName, argumentTypes, entityType, span, argumentNodes, methodFilter);
        return new CannotResolveMethodException(failure.Message, failure.Code, failure.Span, failure.Arguments);
    }

    private static CallableResolutionFailure Classify(
        ISchema schema,
        string callableName,
        IReadOnlyList<Type> argumentTypes,
        Type? entityType,
        TextSpan span,
        IReadOnlyList<Node>? argumentNodes,
        Func<MethodInfo, bool>? methodFilter)
    {
        var allMethods = schema.GetAllLibraryMethods();
        var normalizedName = MethodNameNormalizer.Normalize(callableName);
        var namedMethods = allMethods
            .Where(entry => MethodNameNormalizer.Normalize(entry.Key) == normalizedName)
            .SelectMany(static entry => entry.Value)
            .Distinct(MethodInfoIdentityComparer.Instance)
            .ToArray();

        if (namedMethods.Length == 0)
        {
            var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(callableName, allMethods.Keys);
            var message = string.IsNullOrWhiteSpace(suggestion)
                ? $"Unknown callable '{callableName}'."
                : $"Unknown callable '{callableName}'. Did you mean '{suggestion}'?";
            var arguments = Facts(callableName, argumentTypes, [], null);
            if (!string.IsNullOrWhiteSpace(suggestion))
                arguments["suggestion"] = suggestion;

            return new CallableResolutionFailure(
                DiagnosticCode.MQ3086_UnknownCallable,
                message,
                span,
                arguments);
        }

        var applicableMethods = namedMethods
            .Where(method => methodFilter == null || methodFilter(method))
            .Where(method => IsEntityCompatible(method, entityType))
            .ToArray();
        if (applicableMethods.Length == 0)
        {
            return new CallableResolutionFailure(
                DiagnosticCode.MQ3086_UnknownCallable,
                $"Unknown callable '{callableName}' for the selected callable kind.",
                span,
                Facts(callableName, argumentTypes, namedMethods));
        }

        var arityCandidates = applicableMethods
            .Select(method => new Candidate(method, GetParameters(method)))
            .Where(candidate => candidate.AcceptsArity(argumentTypes.Count))
            .ToArray();
        if (arityCandidates.Length == 0)
        {
            return new CallableResolutionFailure(
                DiagnosticCode.MQ3087_InvalidCallableArity,
                $"Callable '{callableName}' does not accept {argumentTypes.Count} argument(s); expected {FormatExpectedCounts(applicableMethods)}.",
                span,
                Facts(callableName, argumentTypes, applicableMethods, FormatExpectedCounts(applicableMethods)));
        }

        var typeCandidates = arityCandidates
            .Select(candidate => (candidate, score: candidate.GetTypeScore(argumentTypes)))
            .Where(static item => item.score != int.MaxValue)
            .OrderBy(static item => item.score)
            .ToArray();
        if (typeCandidates.Length == 0)
        {
            return new CallableResolutionFailure(
                DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                AddParameterGuidance(
                    $"No overload of callable '{callableName}' accepts argument types ({FormatTypes(argumentTypes)}).",
                    argumentNodes,
                    arityCandidates.Select(static candidate => candidate.Method)),
                span,
                Facts(callableName, argumentTypes, arityCandidates.Select(static candidate => candidate.Method)));
        }

        var bestScore = typeCandidates[0].score;
        var best = typeCandidates
            .Where(item => item.score == bestScore)
            .Where(item => GetResolutionPriority(item.candidate.Method) ==
                           GetResolutionPriority(typeCandidates[0].candidate.Method))
            .ToArray();
        if (best.Length > 1)
        {
            return new CallableResolutionFailure(
                DiagnosticCode.MQ3089_AmbiguousCallableOverload,
                $"Callable '{callableName}' is ambiguous for argument types ({FormatTypes(argumentTypes)}).",
                span,
                Facts(callableName, argumentTypes, best.Select(static item => item.candidate.Method)));
        }

        // This method is called only after the schema's normal resolver failed.
        // A compatible candidate here means the schema has a richer selection rule
        // that rejected the call for another reason; report the closest precise type
        // failure instead of falling back to the legacy generic code.
        return new CallableResolutionFailure(
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            AddParameterGuidance(
                $"No overload of callable '{callableName}' accepts argument types ({FormatTypes(argumentTypes)}).",
                argumentNodes,
                typeCandidates.Select(static item => item.candidate.Method)),
            span,
            Facts(callableName, argumentTypes, typeCandidates.Select(static item => item.candidate.Method)));
    }

    private static Dictionary<string, string> Facts(
        string callableName,
        IReadOnlyList<Type> argumentTypes,
        IEnumerable<MethodInfo> methods,
        string? expectedCounts = null)
    {
        var candidates = methods
            .Select(FormatSignature)
            .Distinct(StringComparer.Ordinal)
            .Take(CandidateLimit)
            .ToArray();
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["callable"] = callableName,
            ["actualTypes"] = FormatTypes(argumentTypes),
            ["candidateSignatures"] = string.Join("; ", candidates)
        };
        if (expectedCounts != null)
            facts["expectedCounts"] = expectedCounts;

        return facts;
    }

    private static string FormatExpectedCounts(IEnumerable<MethodInfo> methods)
    {
        var counts = methods
            .Select(method => GetParameters(method))
            .Select(parameters =>
            {
                var required = parameters.Count(parameter => !parameter.IsOptional);
                var hasParams = parameters.LastOrDefault()?.IsParams == true;
                return hasParams ? $"{required}+" : required == parameters.Length
                    ? required.ToString()
                    : $"{required}..{parameters.Length}";
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .Take(CandidateLimit);

        return string.Join(", ", counts);
    }

    private static string FormatTypes(IEnumerable<Type> types)
    {
        return string.Join(", ", types.Select(FormatType));
    }

    private static string FormatType(Type type)
    {
        if (type == typeof(NullNode.NullType))
            return "null";

        return type.Name.Replace("[]", "[]", StringComparison.Ordinal);
    }

    private static string FormatSignature(MethodInfo method)
    {
        var parameters = GetParameters(method);
        return $"{method.Name}({string.Join(", ", parameters.Select(parameter =>
            parameter.IsOptional ? $"{FormatType(parameter.ParameterType)}?" : FormatType(parameter.ParameterType)))})";
    }

    private static string AddParameterGuidance(
        string message,
        IReadOnlyList<Node>? argumentNodes,
        IEnumerable<MethodInfo> methods)
    {
        if (argumentNodes == null)
            return message;

        var parameters = argumentNodes
            .OfType<ParameterReferenceNode>()
            .Select(parameter => $"${parameter.Name} ({FormatType(parameter.ReturnType ?? typeof(object))})")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (parameters.Length == 0)
            return message;

        var overloads = methods
            .Select(FormatSignature)
            .Distinct(StringComparer.Ordinal)
            .Take(CandidateLimit)
            .ToArray();
        var overloadGuidance = overloads.Length == 0
            ? string.Empty
            : $" Expected overloads include: {string.Join(", ", overloads)}.";
        return message +
               $" Script parameter argument(s) {string.Join(", ", parameters)} use their declared types during method resolution." +
               overloadGuidance +
               " Declare the parameter with a compatible type or use an explicit conversion.";
    }

    private static Parameter[] GetParameters(MethodInfo method)
    {
        return method.GetParameters()
            .Where(static parameter => !IsInjected(parameter))
            .Select(static parameter => new Parameter(
                parameter.ParameterType,
                parameter.IsOptional && parameter.HasDefaultValue,
                parameter.GetCustomAttribute<ParamArrayAttribute>() != null))
            .ToArray();
    }

    private static bool IsInjected(ParameterInfo parameter)
    {
        return parameter.GetCustomAttributes()
            .Any(static attribute => attribute is InjectTypeAttribute ||
                                     attribute.GetType().Name == "InjectSourceAttribute");
    }

    private static bool IsEntityCompatible(MethodInfo method, Type? entityType)
    {
        if (entityType == null)
            return true;

        foreach (var parameter in method.GetParameters())
        {
            var attribute = parameter.GetCustomAttributes()
                .OfType<InjectSpecificSourceAttribute>()
                .FirstOrDefault();
            if (attribute != null && attribute.InjectType != null &&
                !entityType.IsAssignableTo(attribute.InjectType))
                return false;
        }

        return true;
    }

    private static int GetResolutionPriority(MethodInfo method)
    {
        return method.GetCustomAttribute<AggregationMethodAttribute>() == null ? 10 : 0;
    }

    private sealed record Parameter(Type ParameterType, bool IsOptional, bool IsParams)
    {
        public Type EffectiveParameterType => IsParams
            ? ParameterType.GetElementType() ?? ParameterType
            : ParameterType;
    }

    private sealed class Candidate(MethodInfo method, Parameter[] parameters)
    {
        public MethodInfo Method { get; } = method;
        private Parameter[] Parameters { get; } = parameters;

        public bool AcceptsArity(int count)
        {
            var parameterCount = Parameters.Length;
            var hasParams = Parameters.LastOrDefault()?.IsParams == true;
            var fixedCount = hasParams ? parameterCount - 1 : parameterCount;
            var required = Parameters.Take(fixedCount).Count(parameter => !parameter.IsOptional);
            if (count < required)
                return false;

            return hasParams || count <= parameterCount;
        }

        public int GetTypeScore(IReadOnlyList<Type> argumentTypes)
        {
            var hasParams = Parameters.LastOrDefault()?.IsParams == true;
            var fixedCount = hasParams ? Parameters.Length - 1 : Parameters.Length;
            var score = 0;
            for (var index = 0; index < argumentTypes.Count; index++)
            {
                var parameter = index < fixedCount
                    ? Parameters[index]
                    : Parameters[^1];
                var targetType = index < fixedCount || !hasParams
                    ? parameter.ParameterType
                    : parameter.EffectiveParameterType;
                var argumentType = argumentTypes[index];
                var conversionScore = GetConversionScore(argumentType, targetType);
                if (conversionScore == int.MaxValue)
                    return int.MaxValue;

                score += conversionScore;
            }

            return score;
        }
    }

    private static int GetConversionScore(Type argumentType, Type parameterType)
    {
        if (argumentType == typeof(NullNode.NullType))
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null ? 200 : int.MaxValue;

        var argument = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
        var parameter = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (parameter == argument)
            return 0;
        if (parameter.IsGenericParameter)
            return 300;
        if (parameter.IsAssignableFrom(argument))
            return 10 + GetInheritanceDistance(argument, parameter);
        if (IsImplicitNumericConversion(argument, parameter))
            return 100;
        if (parameter.IsArray && argument.IsArray &&
            parameter.GetElementType() is { } parameterElement &&
            argument.GetElementType() is { } argumentElement &&
            GetConversionScore(argumentElement, parameterElement) != int.MaxValue)
            return 120;

        return int.MaxValue;
    }

    private static int GetInheritanceDistance(Type argument, Type parameter)
    {
        if (argument == parameter)
            return 0;

        var distance = 1;
        var current = argument.BaseType;
        while (current != null)
        {
            if (current == parameter)
                return distance;
            distance++;
            current = current.BaseType;
        }

        return 20;
    }

    private static bool IsImplicitNumericConversion(Type from, Type to)
    {
        if (!from.IsPrimitive || !to.IsPrimitive)
            return false;

        if (from == typeof(byte))
            return to == typeof(short) || to == typeof(ushort) || to == typeof(int) || to == typeof(uint) ||
                   to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) ||
                   to == typeof(decimal);

        if (from == typeof(short))
            return to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) ||
                   to == typeof(float) || to == typeof(double) || to == typeof(decimal);

        if (from == typeof(int))
            return to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) ||
                   to == typeof(decimal);

        if (from == typeof(long))
            return to == typeof(float) || to == typeof(double) || to == typeof(decimal);

        if (from == typeof(uint))
            return to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) ||
                   to == typeof(decimal);

        if (from == typeof(ulong))
            return to == typeof(float) || to == typeof(double) || to == typeof(decimal);

        return from == typeof(float) && to == typeof(double);
    }

    private sealed record CallableResolutionFailure(
        DiagnosticCode Code,
        string Message,
        TextSpan Span,
        IReadOnlyDictionary<string, string> Arguments);

    private sealed class MethodInfoIdentityComparer : IEqualityComparer<MethodInfo>
    {
        public static MethodInfoIdentityComparer Instance { get; } = new();

        public bool Equals(MethodInfo? x, MethodInfo? y)
        {
            return x != null && y != null && x.Module == y.Module && x.MetadataToken == y.MetadataToken;
        }

        public int GetHashCode(MethodInfo obj)
        {
            return HashCode.Combine(obj.Module, obj.MetadataToken);
        }
    }
}
