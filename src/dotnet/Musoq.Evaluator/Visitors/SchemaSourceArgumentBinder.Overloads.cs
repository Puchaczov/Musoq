using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

internal static partial class SchemaSourceArgumentBinder
{
    private static SchemaSourceBindingResult AmbiguousSourceOverload(ArgsListNode arguments, IEnumerable<SchemaSourceSignature> signatures) =>
        SchemaSourceBindingResult.Error(CreateCallableFailure(DiagnosticCode.MQ3089_AmbiguousCallableOverload,
            "Multiple datasource overloads accept the supplied arguments.", arguments, signatures));

    private static OverloadPreference CompareSourceSignatures(ArgsListNode arguments, SchemaSourceSignature left, SchemaSourceSignature right) =>
        CallableOverloadPreference.Compare(GetArgumentTypes(arguments),
            new CallableSignaturePreference(left.Parameters.Take(arguments.Args.Length).Select(static parameter => parameter.ParameterType).ToArray(),
                false, false, left.Parameters.Length - arguments.Args.Length),
            new CallableSignaturePreference(right.Parameters.Take(arguments.Args.Length).Select(static parameter => parameter.ParameterType).ToArray(),
                false, false, right.Parameters.Length - arguments.Args.Length));

    private static Type[] GetArgumentTypes(ArgsListNode arguments) => arguments.Args.Select(static argument =>
        argument is NullNode ? NullNode.NullType.Instance : argument.ReturnType ?? typeof(object)).ToArray();

    private static BoundSchemaInvocation[] SelectMostSpecificInvocations(ArgsListNode arguments, IReadOnlyList<BoundSchemaInvocation> invocations)
    {
        var types = GetArgumentTypes(arguments);
        var candidates = invocations.Select(invocation => (Invocation: invocation, Signature: CreatePreference(invocation, types.Length))).ToArray();
        return candidates.Where(candidate => !candidates.Any(other => other.Invocation != candidate.Invocation &&
                CallableOverloadPreference.Compare(types, other.Signature, candidate.Signature) == OverloadPreference.Left))
            .Select(static candidate => candidate.Invocation).ToArray();
    }

    private static CallableSignaturePreference CreatePreference(BoundSchemaInvocation invocation, int argumentCount)
    {
        var types = new Type[argumentCount];
        foreach (var binding in invocation.Arguments)
            if (binding.SourceArgumentIndex is { } sourceIndex)
                types[sourceIndex] = invocation.Signature.Parameters[binding.ParameterIndex].ParameterType;
        return new CallableSignaturePreference(types, false, false, invocation.Arguments.Count(static argument => argument.UsesDefault));
    }

    private static (BoundSchemaInvocation Invocation, int Cost)[] SelectTypedSourceCandidates(
        ArgsListNode arguments, IReadOnlyList<(BoundSchemaInvocation Invocation, int Cost)> candidates)
    {
        if (candidates.Any(static candidate => candidate.Invocation.Signature.Parameters.Any(static parameter =>
                parameter.StructuralType is { Kind: not StructuralTypeKind.Scalar })))
        {
            var bestCost = candidates.Min(static candidate => candidate.Cost);
            return candidates.Where(candidate => candidate.Cost == bestCost).ToArray();
        }

        var best = SelectMostSpecificInvocations(arguments, candidates.Select(static candidate => candidate.Invocation).ToArray());
        return candidates.Where(candidate => best.Contains(candidate.Invocation)).ToArray();
    }
}
