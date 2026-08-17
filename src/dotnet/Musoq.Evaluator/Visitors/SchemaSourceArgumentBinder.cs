using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Visitors;

internal sealed record SchemaSourceBindingFailure(
    DiagnosticCode Code,
    string Message,
    TextSpan Span,
    IReadOnlyDictionary<string, string>? Arguments = null);

internal sealed class SchemaSourceBindingResult
{
    private SchemaSourceBindingResult(BoundSchemaInvocation? invocation, SchemaSourceBindingFailure? failure)
    {
        Invocation = invocation;
        Failure = failure;
    }

    public BoundSchemaInvocation? Invocation { get; }

    public SchemaSourceBindingFailure? Failure { get; }

    public bool IsRequired => Invocation == null && Failure == null;

    public static SchemaSourceBindingResult NotRequired() => new(null, null);

    public static SchemaSourceBindingResult Success(BoundSchemaInvocation invocation) => new(invocation, null);

    public static SchemaSourceBindingResult Error(SchemaSourceBindingFailure failure) => new(null, failure);
}

internal static class SchemaSourceArgumentBinder
{
    public static IReadOnlySet<int> GetPathSensitiveArgumentIndexes(
        ArgsListNode arguments,
        SchemaMethodInfo[] methods,
        BoundSchemaInvocation? invocation)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(methods);

        if (invocation != null)
            return GetBoundPathSensitiveArgumentIndexes(invocation);

        if (arguments.HasNamedArguments)
            return new HashSet<int>();

        var compatibleSignatures = methods
            .Select(SchemaSourceSignature.Create)
            .Where(signature => signature.Parameters.Length == arguments.Args.Length)
            .Where(signature => arguments.Args
                .Select((argument, index) => (argument, index))
                .All(item => IsCompatible(item.argument, signature.Parameters[item.index].ParameterType)))
            .ToArray();

        var pathSensitiveIndexes = new HashSet<int>();
        for (var argumentIndex = 0; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            if (compatibleSignatures.Length > 0 && compatibleSignatures.All(signature =>
                    SuspiciousOrdinaryStringEscapeDiagnostics.IsPathSensitiveStringParameter(
                        signature.Parameters[argumentIndex].Name,
                        signature.Parameters[argumentIndex].ParameterType)))
                pathSensitiveIndexes.Add(argumentIndex);
        }

        return pathSensitiveIndexes;
    }

    public static SchemaSourceBindingResult Bind(ArgsListNode arguments, SchemaMethodInfo[] methods)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(methods);

        var signatures = methods
            .Select(SchemaSourceSignature.Create)
            .OrderBy(FormatSignature, StringComparer.Ordinal)
            .ToArray();
        var hasExistingFullArityMatch = !arguments.HasNamedArguments &&
                                        signatures.Any(signature =>
                                            CanAcceptArgumentCount(signature, arguments.Args.Length));
        if (hasExistingFullArityMatch)
        {
            var fullArityCandidates = signatures
                .Where(signature => CanAcceptArgumentCount(signature, arguments.Args.Length))
                .Where(signature => arguments.Args
                    .Select((argument, index) => (argument, index))
                    .All(item => IsCompatible(item.argument, signature.Parameters[item.index].ParameterType)))
                .ToArray();

            if (fullArityCandidates.Length == 0)
                return SchemaSourceBindingResult.Error(CreateCallableFailure(
                    DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                    "No datasource overload accepts the supplied argument types.",
                    arguments,
                    signatures));

            var scored = fullArityCandidates
                .Select(signature => (signature, score: GetMatchScore(arguments, signature)))
                .OrderByDescending(item => item.score)
                .ToArray();
            var bestFullArity = scored.Where(item => item.score == scored[0].score).ToArray();
            if (bestFullArity.Length > 1)
                return SchemaSourceBindingResult.NotRequired();

            return SchemaSourceBindingResult.NotRequired();
        }

        var requiresBinding = arguments.HasNamedArguments ||
                              signatures.Any(signature => arguments.Args.Length != signature.Parameters.Length);

        if (!requiresBinding)
            return SchemaSourceBindingResult.NotRequired();

        if (signatures.Length == 0 ||
            signatures.All(static signature => !signature.CanBindNamedArguments))
        {
            return arguments.HasNamedArguments
                ? SchemaSourceBindingResult.Error(new SchemaSourceBindingFailure(
                    DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata,
                    "Named datasource arguments require reflected constructor metadata.",
                    FindFirstNamedArgumentSpan(arguments)))
                : SchemaSourceBindingResult.NotRequired();
        }

        if (!arguments.HasNamedArguments && !signatures.Any(signature =>
                signature.CanBindNamedArguments && CanAcceptArgumentCount(signature, arguments.Args.Length)))
        {
            return SchemaSourceBindingResult.Error(CreateCallableFailure(
                DiagnosticCode.MQ3087_InvalidCallableArity,
                "No datasource overload accepts the supplied argument count.",
                arguments,
                signatures.Where(static signature => signature.CanBindNamedArguments)));
        }

        var candidates = new List<(BoundSchemaInvocation Invocation, int Score)>();
        SchemaSourceBindingFailure? firstFailure = null;

        foreach (var signature in signatures)
        {
            if (!signature.CanBindNamedArguments)
                continue;

            var candidate = TryBindCandidate(arguments, signature);
            if (candidate.Invocation != null)
            {
                candidates.Add((candidate.Invocation, candidate.Score));
            }
            else if (firstFailure == null && candidate.Failure != null)
            {
                firstFailure = candidate.Failure;
            }
        }

        if (candidates.Count == 0)
        {
            if (firstFailure is { Code: DiagnosticCode.MQ2034_InvalidNamedSourceArgument or
                                  DiagnosticCode.MQ3079_UnknownSourceArgument or
                                  DiagnosticCode.MQ3080_DuplicateSourceArgument or
                                  DiagnosticCode.MQ3081_MissingRequiredSourceArgument })
                return SchemaSourceBindingResult.Error(firstFailure);

            return SchemaSourceBindingResult.Error(CreateCallableFailure(
                DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                "No datasource overload accepts the supplied argument types.",
                arguments,
                signatures.Where(static signature => signature.CanBindNamedArguments)));
        }

        var bestScore = candidates.Max(static candidate => candidate.Score);
        var best = candidates.Where(candidate => candidate.Score == bestScore).ToArray();
        if (best.Length != 1)
        {
            var signaturesText = string.Join(
                "; ",
                best.Select(candidate => FormatSignature(candidate.Invocation.Signature)));
            return SchemaSourceBindingResult.Error(new SchemaSourceBindingFailure(
                DiagnosticCode.MQ3089_AmbiguousCallableOverload,
                $"Multiple datasource signatures match: {signaturesText}.",
                arguments.Span,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["callable"] = best[0].Invocation.Signature.Method.MethodName,
                    ["actualTypes"] = FormatArgumentTypes(arguments),
                    ["candidateSignatures"] = string.Join("; ", best.Select(candidate => FormatSignature(candidate.Invocation.Signature)))
                }));
        }

        return SchemaSourceBindingResult.Success(best[0].Invocation);
    }

    private static IReadOnlySet<int> GetBoundPathSensitiveArgumentIndexes(BoundSchemaInvocation invocation)
    {
        var pathSensitiveIndexes = new HashSet<int>();
        foreach (var argument in invocation.Arguments)
        {
            if (argument.SourceArgumentIndex is not { } sourceArgumentIndex)
                continue;

            var parameter = invocation.Signature.Parameters[argument.ParameterIndex];
            if (SuspiciousOrdinaryStringEscapeDiagnostics.IsPathSensitiveStringParameter(
                    parameter.Name,
                    parameter.ParameterType))
                pathSensitiveIndexes.Add(sourceArgumentIndex);
        }

        return pathSensitiveIndexes;
    }

    private static (BoundSchemaInvocation? Invocation, int Score, SchemaSourceBindingFailure? Failure)
        TryBindCandidate(ArgsListNode arguments, SchemaSourceSignature signature)
    {
        var parameterIndexes = new int?[signature.Parameters.Length];
        var positionalCount = 0;
        for (var argumentIndex = 0; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            if (arguments.ArgumentNames[argumentIndex].HasValue)
                break;

            if (positionalCount >= parameterIndexes.Length)
            {
                return (null, 0, Failure(
                    DiagnosticCode.MQ3087_InvalidCallableArity,
                    "Too many positional datasource arguments.",
                    arguments.Args[argumentIndex].Span));
            }

            parameterIndexes[positionalCount] = argumentIndex;
            positionalCount++;
        }

        for (var argumentIndex = positionalCount; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            var argumentName = arguments.ArgumentNames[argumentIndex];
            if (argumentName is not { } named)
            {
                return (null, 0, Failure(
                    DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                    "Positional datasource arguments must appear before named arguments.",
                    arguments.Args[argumentIndex].Span));
            }

            var parameterIndex = -1;
            for (var index = 0; index < signature.Parameters.Length; index++)
            {
                if (!string.Equals(signature.Parameters[index].Name, named.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                parameterIndex = index;
                break;
            }
            if (parameterIndex < 0)
            {
                return (null, 0, Failure(
                    DiagnosticCode.MQ3079_UnknownSourceArgument,
                    $"Datasource argument '{named.Name}' is not present in the source signature.",
                    named.Span));
            }

            if (parameterIndexes[parameterIndex].HasValue)
            {
                return (null, 0, Failure(
                    DiagnosticCode.MQ3080_DuplicateSourceArgument,
                    $"Datasource argument '{named.Name}' was supplied more than once.",
                    named.Span));
            }

            parameterIndexes[parameterIndex] = argumentIndex;
        }

        var boundArguments = new BoundSchemaArgument[signature.Parameters.Length];
        var score = 0;
        for (var parameterIndex = 0; parameterIndex < signature.Parameters.Length; parameterIndex++)
        {
            var sourceArgumentIndex = parameterIndexes[parameterIndex];
            if (sourceArgumentIndex is not { } sourceIndex)
            {
                var parameter = signature.Parameters[parameterIndex];
                if (!parameter.HasDefaultValue)
                {
                    return (null, 0, Failure(
                        DiagnosticCode.MQ3081_MissingRequiredSourceArgument,
                        $"Required datasource argument '{parameter.Name}' was not supplied.",
                        arguments.Span));
                }

                boundArguments[parameterIndex] = new BoundSchemaArgument(
                    parameterIndex,
                    null,
                    parameter.DefaultValue);
                continue;
            }

            var parameterMetadata = signature.Parameters[parameterIndex];
            var argument = arguments.Args[sourceIndex];
            if (!IsCompatible(argument, parameterMetadata.ParameterType))
                return (null, 0, null);

            score += GetMatchScore(argument, parameterMetadata.ParameterType);
            boundArguments[parameterIndex] = new BoundSchemaArgument(
                parameterIndex,
                sourceIndex,
                null);
        }

        return (
            new BoundSchemaInvocation(signature, boundArguments, arguments.HasNamedArguments),
            score,
            null);
    }

    private static bool IsCompatible(Node argument, Type parameterType)
    {
        if (argument is NullNode)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        var argumentType = argument.ReturnType;
        if (argumentType == null)
            return true;

        return parameterType.IsAssignableFrom(argumentType) ||
               Nullable.GetUnderlyingType(parameterType) == argumentType;
    }

    private static int GetMatchScore(Node argument, Type parameterType)
    {
        if (argument is NullNode)
            return 0;

        return argument.ReturnType == parameterType ? 2 : 1;
    }

    private static int GetMatchScore(ArgsListNode arguments, SchemaSourceSignature signature)
    {
        var score = 0;
        for (var index = 0; index < arguments.Args.Length; index++)
            score += GetMatchScore(arguments.Args[index], signature.Parameters[index].ParameterType);

        return score;
    }

    private static bool CanAcceptArgumentCount(SchemaSourceSignature signature, int count)
    {
        var required = signature.Parameters.Count(static parameter => parameter.IsRequired);
        return count >= required && count <= signature.Parameters.Length;
    }

    private static SchemaSourceBindingFailure CreateCallableFailure(
        DiagnosticCode code,
        string message,
        ArgsListNode arguments,
        IEnumerable<SchemaSourceSignature> signatures)
    {
        var signaturesArray = signatures
            .Select(FormatSignature)
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["actualTypes"] = FormatArgumentTypes(arguments),
            ["candidateSignatures"] = string.Join("; ", signaturesArray)
        };
        if (code == DiagnosticCode.MQ3087_InvalidCallableArity)
            facts["expectedCounts"] = string.Join(", ", signatures.Select(FormatExpectedCount).Distinct(StringComparer.Ordinal));

        return new SchemaSourceBindingFailure(code, message, arguments.Span, facts);
    }

    private static string FormatArgumentTypes(ArgsListNode arguments)
    {
        return string.Join(", ", arguments.Args.Select(argument => argument.ReturnType?.Name ?? "object"));
    }

    private static string FormatExpectedCount(SchemaSourceSignature signature)
    {
        var required = signature.Parameters.Count(static parameter => parameter.IsRequired);
        return required == signature.Parameters.Length
            ? required.ToString()
            : $"{required}..{signature.Parameters.Length}";
    }

    private static SchemaSourceBindingFailure Failure(DiagnosticCode code, string message, TextSpan span) =>
        new(code, message, span);

    private static TextSpan FindFirstNamedArgumentSpan(ArgsListNode arguments)
    {
        foreach (var name in arguments.ArgumentNames)
            if (name is { } named)
                return named.Span;

        return arguments.Span;
    }

    private static string FormatSignature(SchemaSourceSignature signature)
    {
        var parameters = string.Join(
            ", ",
            signature.Parameters.Select(static parameter =>
                $"{parameter.Name}: {parameter.ParameterType.FullName}"));
        return $"{signature.Method.MethodName}({parameters})";
    }
}
