using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private MethodInfo SelectBestMethod(string name, IReadOnlyList<Type> arguments, IReadOnlyList<MethodInfo> methods)
    {
        var candidates = methods.Select(method => (Method: method, Signature: CreateSignaturePreference(method, arguments.Count))).ToArray();
        var best = candidates.Where(candidate => !candidates.Any(other => other.Method != candidate.Method &&
            CallableOverloadPreference.Compare(arguments, other.Signature, candidate.Signature) == OverloadPreference.Left)).ToArray();
        best = best.Where(candidate => !best.Any(other =>
            IsNativeLibraryAdapterFor(other.Method, candidate.Method) ||
            IsQueryStatsContractAdapterFor(other.Method, candidate.Method))).ToArray();
        if (best.Length == 1)
            return best[0].Method;

        throw MethodResolutionException.ForAmbiguousMethod(name,
            arguments.Select(static argument => argument.Name).ToArray(),
            best.Select(static candidate => candidate.Method.ToString()!).Order(StringComparer.Ordinal).ToArray());
    }

    private static bool IsQueryStatsContractAdapterFor(MethodInfo native, MethodInfo legacy)
    {
        if (native == legacy || native.DeclaringType != typeof(LibraryBase) || legacy.DeclaringType != typeof(LibraryBase) ||
            native.Name != legacy.Name || native.ReturnType != legacy.ReturnType)
            return false;
        var nativeParameters = native.GetParameters();
        var legacyParameters = legacy.GetParameters();
        return nativeParameters.Length == legacyParameters.Length &&
               nativeParameters.Zip(legacyParameters).All(pair => pair.First.ParameterType == pair.Second.ParameterType ||
                   (pair.First.ParameterType == typeof(IQueryStats) && pair.Second.ParameterType == typeof(QueryStats) &&
                    pair.First.IsDefined(typeof(InjectQueryStatsAttribute)) && pair.Second.IsDefined(typeof(InjectQueryStatsAttribute))));
    }

    private static bool IsNativeLibraryAdapterFor(MethodInfo native, MethodInfo legacy)
    {
        if (native == legacy || native.DeclaringType != typeof(LibraryBase) || legacy.DeclaringType != typeof(LibraryBase) ||
            native.Name != legacy.Name || native.GetGenericArguments().Length != legacy.GetGenericArguments().Length)
            return false;
        var nativeParameters = native.GetParameters();
        var legacyParameters = legacy.GetParameters();
        if (nativeParameters.Length != legacyParameters.Length || nativeParameters.Length == 0 ||
            !VariadicParameter.IsNativeSpan(nativeParameters[^1].ParameterType) ||
            !legacyParameters[^1].IsDefined(typeof(ParamArrayAttribute)))
            return false;

        return nativeParameters[..^1].Zip(legacyParameters[..^1]).All(pair => SameGenericShape(pair.First.ParameterType, pair.Second.ParameterType)) &&
               SameGenericShape(VariadicParameter.ElementType(nativeParameters[^1].ParameterType)!, legacyParameters[^1].ParameterType.GetElementType()!) &&
               SameGenericShape(native.ReturnType, legacy.ReturnType);
    }

    private static bool SameGenericShape(Type left, Type right)
    {
        if (left == right)
            return true;
        if (left.IsGenericParameter && right.IsGenericParameter)
            return left.GenericParameterPosition == right.GenericParameterPosition;
        if (left.IsArray && right.IsArray)
            return left.GetArrayRank() == right.GetArrayRank() && SameGenericShape(left.GetElementType()!, right.GetElementType()!);
        return left.IsGenericType && right.IsGenericType && left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition() &&
               left.GetGenericArguments().Zip(right.GetGenericArguments()).All(pair => SameGenericShape(pair.First, pair.Second));
    }

    private CallableSignaturePreference CreateSignaturePreference(MethodInfo method, int argumentCount)
    {
        var metadata = GetCachedParameterMetadata(method);
        var parameters = metadata.Parameters.Where(static parameter => !parameter.IsDefined(typeof(InjectTypeAttribute))).ToArray();
        var variadic = metadata.ParamsParameters.HasParameters();
        var suppliedTypes = new Type[argumentCount];
        for (var index = 0; index < argumentCount; index++)
        {
            var parameterIndex = Math.Min(index, parameters.Length - 1);
            var parameterType = parameters[parameterIndex].ParameterType;
            if (variadic && parameterIndex == parameters.Length - 1)
                parameterType = VariadicParameter.ElementType(parameterType)!;
            suppliedTypes[index] = parameterType;
        }

        return new CallableSignaturePreference(suppliedTypes, variadic, method.IsGenericMethod,
            Math.Max(0, metadata.NotAnnotatedParametersCount - argumentCount - (variadic ? 1 : 0)));
    }
}
