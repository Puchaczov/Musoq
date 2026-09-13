using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Reflection;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

internal sealed record SchemaSourceBindingFailure(
    DiagnosticCode Code,
    string Message,
    TextSpan Span,
    IReadOnlyDictionary<string, string>? Arguments = null,
    IReadOnlyList<DiagnosticAction>? SuggestedFixes = null);

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
        BoundSchemaInvocation? invocation,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver = null,
        Func<Node, CteRelationBinding?>? relationResolver = null)
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
                .All(item => IsCompatible(item.argument, signature.Parameters[item.index], structuralTypeResolver, relationResolver)))
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

    public static SchemaSourceBindingResult Bind(
        ArgsListNode arguments,
        SchemaMethodInfo[] methods,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver = null,
        Func<Node, CteRelationBinding?>? relationResolver = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(methods);

        if (methods.Any(static method => method.StructuralContract != null))
            return BindTyped(arguments, methods, structuralTypeResolver, relationResolver);

        var signatures = methods
            .Select(SchemaSourceSignature.Create)
            .OrderBy(FormatSignature, StringComparer.Ordinal)
            .ToArray();
        if (arguments.Args.Any(static argument =>
                argument is RecordLiteralNode or ArrayLiteralNode))
        {
            return SchemaSourceBindingResult.Error(CreateCallableFailure(
                DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                "Structural datasource arguments require explicit typed source registration.",
                arguments,
                signatures));
        }

        var hasExistingFullArityMatch = !arguments.HasNamedArguments &&
                                        signatures.Any(signature =>
                                            CanAcceptArgumentCount(signature, arguments.Args.Length));
        if (hasExistingFullArityMatch)
        {
            var fullArityCandidates = signatures
                .Where(signature => CanAcceptArgumentCount(signature, arguments.Args.Length))
                .Where(signature => arguments.Args
                    .Select((argument, index) => (argument, index))
                    .All(item => IsCompatible(item.argument, signature.Parameters[item.index], structuralTypeResolver, relationResolver)))
                .ToArray();

            if (fullArityCandidates.Length == 0)
                return SchemaSourceBindingResult.Error(CreateCallableFailure(
                    DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                    "No datasource overload accepts the supplied argument types.",
                    arguments,
                    signatures));

            var scored = fullArityCandidates
                .Select(signature => (signature, score: GetMatchScore(arguments, signature, structuralTypeResolver, relationResolver)))
                .OrderByDescending(item => item.score)
                .ToArray();
            var bestFullArity = scored.Where(item => item.score == scored[0].score).ToArray();
            if (bestFullArity.Length > 1)
                return SchemaSourceBindingResult.NotRequired();

            var selectedSignature = bestFullArity[0].signature;
            if (selectedSignature.HasStructuralParameters)
            {
                var structuralCandidate = TryBindCandidate(arguments, selectedSignature, [selectedSignature], structuralTypeResolver, relationResolver);
                if (structuralCandidate.Invocation != null)
                    return SchemaSourceBindingResult.Success(structuralCandidate.Invocation);
            }
            if (selectedSignature.CanBindNamedArguments &&
                arguments.Args.Length < selectedSignature.Parameters.Length)
            {
                var selectedCandidate = TryBindCandidate(
                    arguments,
                    selectedSignature,
                    [selectedSignature],
                    structuralTypeResolver,
                    relationResolver);
                if (selectedCandidate.Invocation != null)
                    return SchemaSourceBindingResult.Success(selectedCandidate.Invocation);

                if (selectedCandidate.Failure != null)
                    return SchemaSourceBindingResult.Error(selectedCandidate.Failure);
            }

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
                ? SchemaSourceBindingResult.Error(CreateMetadataRequirementFailure(arguments))
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

        var bindableSignatures = signatures
            .Where(static signature => signature.CanBindNamedArguments)
            .ToArray();
        var candidates = new List<(BoundSchemaInvocation Invocation, int Score)>();
        SchemaSourceBindingFailure? firstFailure = null;

        foreach (var signature in bindableSignatures)
        {
            var candidate = TryBindCandidate(arguments, signature, bindableSignatures, structuralTypeResolver, relationResolver);
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

    private static SchemaSourceBindingResult BindTyped(
        ArgsListNode arguments,
        SchemaMethodInfo[] methods,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
        Func<Node, CteRelationBinding?>? relationResolver)
    {
        var signatures = methods
            .Select(SchemaSourceSignature.Create)
            .OrderBy(FormatSignature, StringComparer.Ordinal)
            .ThenBy(static signature => signature.SourceStableId, StringComparer.Ordinal)
            .ToArray();
        if (signatures.Length == 0)
            return SchemaSourceBindingResult.Error(CreateCallableFailure(
                DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                "No typed datasource overload is registered.",
                arguments,
                signatures));

        var interpretationFailure = ResolveRelationInterpretations(
            arguments,
            signatures,
            relationResolver);
        if (interpretationFailure != null)
            return SchemaSourceBindingResult.Error(interpretationFailure);

        var candidates = new List<(BoundSchemaInvocation Invocation, int Cost)>();
        SchemaSourceBindingFailure? firstFailure = null;
        SchemaSourceBindingFailure? relationAmbiguity = null;
        for (var signatureIndex = 0; signatureIndex < signatures.Length; signatureIndex++)
        {
            var signature = signatures[signatureIndex];
            var candidate = TryBindTypedCandidate(
                arguments,
                signature,
                signatures,
                structuralTypeResolver,
                relationResolver,
                signatureIndex);
            if (candidate.Invocation != null)
            {
                candidates.Add((candidate.Invocation, candidate.Cost));
                continue;
            }

            if (candidate.Failure?.Code == DiagnosticCode.MQ3116_AmbiguousRelationArgument)
                relationAmbiguity ??= candidate.Failure;
            firstFailure = PreferFailure(firstFailure, candidate.Failure);
        }

        if (relationAmbiguity != null)
            return SchemaSourceBindingResult.Error(relationAmbiguity);

        if (candidates.Count == 0)
        {
            if (firstFailure != null)
                return SchemaSourceBindingResult.Error(firstFailure);

            return SchemaSourceBindingResult.Error(CreateCallableFailure(
                DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                "No datasource overload accepts the supplied argument types.",
                arguments,
                signatures));
        }

        var bestCost = candidates.Min(static candidate => candidate.Cost);
        var best = candidates.Where(candidate => candidate.Cost == bestCost).ToArray();
        if (best.Length > 1)
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
                    ["candidateSignatures"] = signaturesText
                }));
        }

        return SchemaSourceBindingResult.Success(best[0].Invocation);
    }

    private static SchemaSourceBindingFailure? ResolveRelationInterpretations(
        ArgsListNode arguments,
        IReadOnlyList<SchemaSourceSignature> signatures,
        Func<Node, CteRelationBinding?>? relationResolver)
    {
        if (relationResolver == null)
            return null;

        for (var argumentIndex = 0; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            var argument = arguments.Args[argumentIndex];
            var relation = relationResolver(argument);
            if (relation == null)
                continue;

            var scalarSignatures = new List<string>();
            var relationSignatures = new List<string>();
            SchemaSourceBindingFailure? relationFailure = null;

            foreach (var signature in signatures)
            {
                if (!TryMapArgumentParameters(
                        arguments,
                        signature,
                        signatures,
                        out var parameterIndexes,
                        out _)
                    || FindMappedParameterIndex(parameterIndexes, argumentIndex) is not { } parameterIndex)
                    continue;

                var parameter = signature.Parameters[parameterIndex];
                if (SchemaSourceCompatibility.TryGetScalarCost(
                        argument,
                        parameter,
                        relation,
                        out _,
                        out _))
                {
                    scalarSignatures.Add(FormatSignature(signature));
                }

                if (SchemaSourceCompatibility.TryGetRelationOnlyCost(
                        relation,
                        parameter,
                        out _,
                        out var relationError))
                {
                    relationSignatures.Add(FormatSignature(signature));
                }
                else
                {
                    relationFailure = PreferFailure(
                        relationFailure,
                        CreateRelationFailure(
                            argument,
                            relation,
                            parameter,
                            signature,
                            relationError));
                }
            }

            if (scalarSignatures.Count > 0 && relationSignatures.Count > 0)
            {
                var facts = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["argument"] = argument is IdentifierNode identifier ? identifier.Name : relation.Name,
                    ["cte"] = relation.Name,
                    ["scalarCandidates"] = string.Join("; ", scalarSignatures.Distinct(StringComparer.Ordinal)),
                    ["relationCandidates"] = string.Join("; ", relationSignatures.Distinct(StringComparer.Ordinal))
                };
                return new SchemaSourceBindingFailure(
                    DiagnosticCode.MQ3116_AmbiguousRelationArgument,
                    $"Datasource argument '{relation.Name}' is ambiguous between a scalar column and a complete CTE relation.",
                    argument.Span,
                    facts);
            }

            // A visible CTE is a committed relation interpretation. If no
            // typed overload can consume it, preserve that structural failure
            // even when a scalar interpretation happens to be convertible.
            if (relationSignatures.Count == 0 && relationFailure != null)
                return relationFailure;
        }

        return null;
    }

    private static SchemaSourceBindingFailure CreateRelationFailure(
        Node argument,
        CteRelationBinding relation,
        SchemaSourceParameter parameter,
        SchemaSourceSignature signature,
        string error)
    {
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argument"] = parameter.Name,
            ["cte"] = relation.Name,
            ["candidateSignature"] = FormatSignature(signature),
            ["reasonKind"] = "structural",
            ["path"] = parameter.Name
        };
        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            error,
            argument.Span,
            facts);
    }

    private static (BoundSchemaInvocation? Invocation, int Cost, SchemaSourceBindingFailure? Failure)
        TryBindTypedCandidate(
            ArgsListNode arguments,
            SchemaSourceSignature signature,
            IReadOnlyList<SchemaSourceSignature> signatures,
            Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
            Func<Node, CteRelationBinding?>? relationResolver,
            int overloadIndex)
    {
        if (!TryMapArgumentParameters(
                arguments,
                signature,
                signatures,
                out var parameterIndexes,
                out var mappingFailure))
        {
            return (null, SchemaConversionClassifier.Incompatible, mappingFailure);
        }

        var boundArguments = new BoundSchemaArgument[signature.Parameters.Length];
        var cost = 0;
        for (var parameterIndex = 0; parameterIndex < signature.Parameters.Length; parameterIndex++)
        {
            var parameter = signature.Parameters[parameterIndex];
            if (parameterIndexes[parameterIndex] is not { } sourceIndex)
            {
                if (!parameter.HasDefaultValue)
                    return (null, SchemaConversionClassifier.Incompatible, CreateMissingArgumentFailure(parameter, arguments, signatures));

                boundArguments[parameterIndex] = new BoundSchemaArgument(parameterIndex, null, parameter.DefaultValue);
                continue;
            }

            var argument = arguments.Args[sourceIndex];
            if (!SchemaSourceCompatibility.TryGetCost(
                    argument,
                    parameter,
                    structuralTypeResolver,
                    relationResolver,
                    out var argumentCost,
                    out var constructionPlan,
                    out var relationAmbiguous,
                    out var error))
            {
                if (relationAmbiguous)
                {
                    return (null, SchemaConversionClassifier.Incompatible, new SchemaSourceBindingFailure(
                        DiagnosticCode.MQ3116_AmbiguousRelationArgument,
                        error,
                        argument.Span,
                        new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["argument"] = parameter.Name,
                            ["cte"] = relationResolver?.Invoke(argument)?.Name ?? parameter.Name
                        }));
                }

                return (null, SchemaConversionClassifier.Incompatible, CreateConversionFailure(
                    argument,
                    parameter,
                    signature,
                    error));
            }

            cost += argumentCost;
            var relation = relationResolver?.Invoke(argument);
            boundArguments[parameterIndex] = new BoundSchemaArgument(
                parameterIndex,
                sourceIndex,
                null,
                relation,
                constructionPlan);
        }

        return (new BoundSchemaInvocation(signature, boundArguments, arguments.HasNamedArguments, overloadIndex), cost, null);
    }

    private static bool TryMapArgumentParameters(
        ArgsListNode arguments,
        SchemaSourceSignature signature,
        IReadOnlyList<SchemaSourceSignature> signatures,
        out int?[] parameterIndexes,
        out SchemaSourceBindingFailure? failure)
    {
        parameterIndexes = new int?[signature.Parameters.Length];
        failure = null;
        if (!CanAcceptArgumentCount(signature, arguments.Args.Length))
            return false;

        var positionalCount = 0;
        for (var argumentIndex = 0; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            if (arguments.ArgumentNames[argumentIndex].HasValue)
                break;
            if (positionalCount >= parameterIndexes.Length)
            {
                failure = Failure(
                    DiagnosticCode.MQ3087_InvalidCallableArity,
                    "Too many positional datasource arguments.",
                    arguments.Args[argumentIndex].Span);
                return false;
            }

            parameterIndexes[positionalCount++] = argumentIndex;
        }

        for (var argumentIndex = positionalCount; argumentIndex < arguments.Args.Length; argumentIndex++)
        {
            if (arguments.ArgumentNames[argumentIndex] is not { } named)
            {
                failure = Failure(
                    DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                    "Positional datasource arguments must appear before named arguments.",
                    arguments.Args[argumentIndex].Span);
                return false;
            }

            var parameterIndex = FindParameterIndex(signature, named.Name);
            if (parameterIndex < 0)
            {
                failure = CreateUnknownArgumentFailure(named, signatures);
                return false;
            }

            if (parameterIndexes[parameterIndex].HasValue)
            {
                failure = CreateDuplicateArgumentFailure(
                    named,
                    signature.Parameters[parameterIndex].Name,
                    signatures);
                return false;
            }

            parameterIndexes[parameterIndex] = argumentIndex;
        }

        return true;
    }

    private static int FindParameterIndex(SchemaSourceSignature signature, string name)
    {
        for (var index = 0; index < signature.Parameters.Length; index++)
        {
            if (string.Equals(signature.Parameters[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }

    private static int? FindMappedParameterIndex(int?[] parameterIndexes, int sourceArgumentIndex)
    {
        for (var parameterIndex = 0; parameterIndex < parameterIndexes.Length; parameterIndex++)
        {
            if (parameterIndexes[parameterIndex] == sourceArgumentIndex)
                return parameterIndex;
        }

        return null;
    }

    private static SchemaSourceBindingFailure CreateConversionFailure(
        Node argument,
        SchemaSourceParameter parameter,
        SchemaSourceSignature signature,
        string error)
    {
        var structural = error.Contains("structural", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("field", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("collection", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("record", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("CTE", StringComparison.OrdinalIgnoreCase);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argument"] = parameter.Name,
            ["candidateSignature"] = FormatSignature(signature),
            ["reasonKind"] = structural ? "structural" : "scalar"
        };
        if (structural)
            facts["path"] = parameter.Name;

        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            error,
            argument.Span,
            facts);
    }

    private static SchemaSourceBindingFailure? PreferFailure(
        SchemaSourceBindingFailure? current,
        SchemaSourceBindingFailure? candidate)
    {
        if (candidate == null)
            return current;
        if (current == null)
            return candidate;

        var currentPriority = GetFailurePriority(current);
        var candidatePriority = GetFailurePriority(candidate);
        return candidatePriority > currentPriority ? candidate : current;
    }

    private static int GetFailurePriority(SchemaSourceBindingFailure failure)
    {
        if (failure.Code is DiagnosticCode.MQ2034_InvalidNamedSourceArgument or
            DiagnosticCode.MQ3079_UnknownSourceArgument or
            DiagnosticCode.MQ3080_DuplicateSourceArgument or
            DiagnosticCode.MQ3081_MissingRequiredSourceArgument or
            DiagnosticCode.MQ3087_InvalidCallableArity)
            return 4000;
        if (failure.Code == DiagnosticCode.MQ3116_AmbiguousRelationArgument)
            return 3000;
        if (failure.Arguments?.TryGetValue("reasonKind", out var reasonKind) == true &&
            string.Equals(reasonKind, "structural", StringComparison.Ordinal))
            return 2000;
        if (failure.Code == DiagnosticCode.MQ3088_NoMatchingCallableOverload)
            return 1000;
        return 0;
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
        TryBindCandidate(
            ArgsListNode arguments,
            SchemaSourceSignature signature,
            IReadOnlyList<SchemaSourceSignature> bindableSignatures,
            Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
            Func<Node, CteRelationBinding?>? relationResolver)
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
                return (null, 0, CreateUnknownArgumentFailure(named, bindableSignatures));
            }

            if (parameterIndexes[parameterIndex].HasValue)
            {
                return (null, 0, CreateDuplicateArgumentFailure(
                    named,
                    signature.Parameters[parameterIndex].Name,
                    bindableSignatures));
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
                    return (null, 0, CreateMissingArgumentFailure(
                        parameter,
                        arguments,
                        bindableSignatures));
                }

                boundArguments[parameterIndex] = new BoundSchemaArgument(
                    parameterIndex,
                    null,
                    parameter.DefaultValue);
                continue;
            }

            var parameterMetadata = signature.Parameters[parameterIndex];
            var argument = arguments.Args[sourceIndex];
            var relation = relationResolver?.Invoke(argument);
            if (relation != null)
            {
                if (!TryValidateRelation(relation, parameterMetadata, out _))
                    return (null, 0, null);

                score += 4;
                boundArguments[parameterIndex] = new BoundSchemaArgument(
                    parameterIndex,
                    sourceIndex,
                    null,
                    relation);
                continue;
            }

            if (!IsCompatible(argument, parameterMetadata, structuralTypeResolver, relationResolver))
                return (null, 0, null);

            score += GetMatchScore(argument, parameterMetadata, relationResolver);
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

    internal static bool TryValidateRelation(
        CteRelationBinding relation,
        SchemaSourceParameter parameter,
        out string error)
    {
        error = string.Empty;
        var receiver = parameter.StructuralType;
        if (receiver == null || receiver.Kind != StructuralTypeKind.Collection ||
            receiver.ElementType == null || relation.Columns.Count == 0)
        {
            error = "A CTE relation requires a collection receiving contract.";
            return false;
        }

        StructuralTypeDescriptor inputElement;
        if (receiver.ElementType.Kind == StructuralTypeKind.Scalar)
        {
            if (relation.Columns.Count != 1)
            {
                error = "A primitive CTE collection requires exactly one output column.";
                return false;
            }

            inputElement = StructuralTypeDescriptor.FromClrType(relation.Columns[0].ColumnType);
        }
        else if (receiver.ElementType.Kind == StructuralTypeKind.Record)
        {
            inputElement = relation.Shape;
        }
        else
        {
            error = "A CTE row cannot be converted to a nested collection element.";
            return false;
        }

        return StructuralConstructionPlanBinder.TryValidateCompatibility(
            inputElement,
            receiver.ElementType,
            out error);
    }

    private static bool IsCompatible(
        Node argument,
        SchemaSourceParameter parameter,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
        Func<Node, CteRelationBinding?>? relationResolver)
    {
        var parameterType = parameter.ParameterType;
        if (relationResolver?.Invoke(argument) is { } relation)
            return TryValidateRelation(relation, parameter, out _);

        if (argument is NullNode)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        if (parameter.StructuralType != null)
        {
            var argumentShape = structuralTypeResolver?.Invoke(argument);
            if (argumentShape != null)
                return StructuralConstructionPlanBinder.TryValidateCompatibility(
                    argumentShape,
                    parameter.StructuralType,
                    out _);

            // Structural literals are context-bound: their shape is validated
            // by the receiver-aware expression binder after overload selection.
            if (argument.ReturnType == null)
                return true;
        }

        var argumentType = argument.ReturnType;
        if (argumentType == null)
            return true;

        return parameterType.IsAssignableFrom(argumentType) ||
               Nullable.GetUnderlyingType(parameterType) == argumentType;
    }

    private static int GetMatchScore(
        Node argument,
        SchemaSourceParameter parameter,
        Func<Node, CteRelationBinding?>? relationResolver)
    {
        if (relationResolver?.Invoke(argument) != null)
            return 4;

        if (argument is NullNode)
            return 0;

        return argument.ReturnType == parameter.ParameterType ? 2 : 1;
    }

    private static int GetMatchScore(
        ArgsListNode arguments,
        SchemaSourceSignature signature,
        Func<Node, StructuralTypeDescriptor?>? structuralTypeResolver,
        Func<Node, CteRelationBinding?>? relationResolver)
    {
        var score = 0;
        for (var index = 0; index < arguments.Args.Length; index++)
            score += GetMatchScore(arguments.Args[index], signature.Parameters[index], relationResolver);

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

    private static SchemaSourceBindingFailure CreateUnknownArgumentFailure(
        ArgumentName named,
        IReadOnlyList<SchemaSourceSignature> bindableSignatures)
    {
        var parameterNames = bindableSignatures
            .SelectMany(static signature => signature.Parameters.Select(static parameter => parameter.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(named.Name, parameterNames);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argument"] = named.Name,
            ["candidateParameters"] = string.Join(", ", parameterNames)
        };
        var message = $"Datasource argument '{named.Name}' is not present in the source signature.";
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null;

        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            facts["suggestion"] = suggestion;
            message += $" Did you mean '{suggestion}'?";

            // A replacement is safe when every reflected overload exposes the
            // same canonical parameter set. Otherwise retain the textual
            // suggestion and candidate facts, but do not offer an automatic
            // edit that could select a parameter from only one overload.
            var isUnambiguous = HasSameCanonicalParameterSet(bindableSignatures);
            if (isUnambiguous)
            {
                suggestedFixes =
                [
                    DiagnosticAction.QuickFix(
                        $"Replace '{named.Name}' with '{suggestion}'",
                        named.Span,
                        suggestion)
                ];
            }
        }

        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3079_UnknownSourceArgument,
            message,
            named.Span,
            facts,
            suggestedFixes);
    }

    private static bool HasSameCanonicalParameterSet(IReadOnlyList<SchemaSourceSignature> signatures)
    {
        if (signatures.Count == 0)
            return false;

        var expected = GetCanonicalParameterSet(signatures[0]);
        return signatures.Skip(1).All(signature =>
            string.Equals(expected, GetCanonicalParameterSet(signature), StringComparison.Ordinal));
    }

    private static string GetCanonicalParameterSet(SchemaSourceSignature signature)
    {
        return string.Join(
            "\u001f",
            signature.Parameters
                .Select(static parameter => parameter.Name)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .Select(static name => name.ToUpperInvariant()));
    }

    private static SchemaSourceBindingFailure CreateDuplicateArgumentFailure(
        ArgumentName named,
        string parameterName,
        IReadOnlyList<SchemaSourceSignature> bindableSignatures)
    {
        var parameterNames = bindableSignatures
            .SelectMany(static signature => signature.Parameters.Select(static parameter => parameter.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argument"] = named.Name,
            ["parameter"] = parameterName,
            ["candidateParameters"] = string.Join(", ", parameterNames)
        };

        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3080_DuplicateSourceArgument,
            $"Datasource argument '{named.Name}' was supplied more than once.",
            named.Span,
            facts);
    }

    private static SchemaSourceBindingFailure CreateMetadataRequirementFailure(ArgsListNode arguments)
    {
        var named = FindFirstNamedArgument(arguments);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["requiresMetadata"] = "true",
            ["bindingMode"] = "positional-only"
        };
        if (named is { } argument)
            facts["argument"] = argument.Name;

        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata,
            "Named datasource arguments require reflected constructor metadata.",
            named?.Span ?? arguments.Span,
            facts);
    }

    private static SchemaSourceBindingFailure CreateMissingArgumentFailure(
        SchemaSourceParameter parameter,
        ArgsListNode arguments,
        IReadOnlyList<SchemaSourceSignature> bindableSignatures)
    {
        var parameterNames = bindableSignatures
            .SelectMany(static signature => signature.Parameters.Select(static candidate => candidate.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["missingArgument"] = parameter.Name,
            ["expectedType"] = parameter.ParameterType.Name,
            ["candidateParameters"] = string.Join(", ", parameterNames),
            ["candidateSignatures"] = string.Join(
                "; ",
                bindableSignatures.Select(FormatSignature).Distinct(StringComparer.Ordinal).Take(5))
        };

        return new SchemaSourceBindingFailure(
            DiagnosticCode.MQ3081_MissingRequiredSourceArgument,
            $"Required datasource argument '{parameter.Name}' was not supplied.",
            FindMissingArgumentInsertionSpan(arguments),
            facts);
    }

    private static TextSpan FindMissingArgumentInsertionSpan(ArgsListNode arguments)
    {
        // Parser-created argument lists span the opening and closing
        // parentheses. Point at the closing delimiter so an editor can insert
        // the missing named argument without replacing valid text.
        return arguments.Span.Length >= 2
            ? new TextSpan(arguments.Span.End - 1, 0)
            : new TextSpan(arguments.Span.End, 0);
    }

    private static ArgumentName? FindFirstNamedArgument(ArgsListNode arguments)
    {
        foreach (var name in arguments.ArgumentNames)
            if (name is { } named)
                return named;

        return null;
    }

    private static string FormatSignature(SchemaSourceSignature signature)
    {
        var parameters = string.Join(
            ", ",
            signature.Parameters.Select(static parameter =>
                $"{parameter.Name}: {parameter.ParameterType.Name}"));
        return $"{signature.Method.MethodName}({parameters})";
    }
}
