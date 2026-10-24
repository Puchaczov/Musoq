using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private delegate bool TryGetMethodByExactNameFunc(string exactName, out int index);

    private bool TryGetMethodWithNormalization(string name, out int index, out string? actualMethodName,
        TryGetMethodByExactNameFunc tryExactName)
    {
        if (tryExactName(name, out index))
        {
            actualMethodName = name;
            return true;
        }

        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (_normalizedToOriginalMethodNames.TryGetValue(normalizedName, out var originalName) &&
            tryExactName(originalName, out index))
        {
            actualMethodName = originalName;
            return true;
        }

        index = -1;
        actualMethodName = null;
        return false;
    }

    private bool TryGetRawMethod(string name, Type[] methodArgs, out int index, out string? actualMethodName)
    {
        return TryGetMethodWithNormalization(name, out index, out actualMethodName,
            (string exactName, out int idx) => TryGetRawMethodByExactName(exactName, methodArgs, out idx));
    }

    private bool TryGetRawMethodByExactName(string name, Type[] methodArgs, out int index)
    {
        if (!_methods.TryGetValue(name, out var methods))
        {
            index = -1;
            return false;
        }

        for (var i = 0; i < methods.Count; ++i)
        {
            var method = methods[i];
            var parameters = GetCachedParameters(method);

            if (parameters.Length != methodArgs.Length)
                continue;

            var hasMatchedArgTypes = true;

            for (var j = 0; j < parameters.Length; ++j)
            {
                if (parameters[j].ParameterType.GetUnderlyingNullable() == methodArgs[j])
                    continue;

                hasMatchedArgTypes = false;
                break;
            }

            if (!hasMatchedArgTypes)
                continue;

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    private bool TryGetAnnotatedMethod(string name, IReadOnlyList<Type> methodArgs, Type? entityType, out int index,
        out string? actualMethodName)
    {
        var argTypesArray = methodArgs as Type[] ?? CopyToArray(methodArgs);
        var cacheKey = new MethodResolutionKey(name, argTypesArray, entityType);

        if (_annotatedMethodCache.TryGetValue(cacheKey, out var cached))
        {
            index = cached.Index;
            actualMethodName = cached.ActualName;
            return cached.Found;
        }

        var found = TryGetMethodWithNormalization(name, out index, out actualMethodName,
            (string exactName, out int idx) =>
                TryGetAnnotatedMethodByExactName(exactName, methodArgs, entityType, out idx));

        _annotatedMethodCache.TryAdd(cacheKey, (found, index, actualMethodName));
        return found;
    }

    private bool TryGetAnnotatedMethod(
        string name,
        IReadOnlyList<Type> methodArgs,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        out int index,
        out string? actualMethodName)
    {
        return TryGetMethodWithNormalization(name, out index, out actualMethodName,
            (string exactName, out int idx) =>
                TryGetAnnotatedMethodByExactName(exactName, methodArgs, entityType, methodFilter, out idx));
    }

    private static Type[] CopyToArray(IReadOnlyList<Type> list)
    {
        var result = new Type[list.Count];
        for (var i = 0; i < list.Count; i++)
            result[i] = list[i];
        return result;
    }

    private bool TryGetAnnotatedMethodByExactName(string name, IReadOnlyList<Type> methodArgs, Type? entityType,
        out int index)
    {
        return TryGetAnnotatedMethodByExactName(name, methodArgs, entityType, static _ => true, out index);
    }

    private bool TryGetAnnotatedMethodByExactName(
        string name,
        IReadOnlyList<Type> methodArgs,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        out int index)
    {
        if (!_methods.TryGetValue(name, out var methods))
        {
            index = -1;
            return false;
        }

        var methodCount = methods.Count;
        var candidates = new List<MethodInfo>();

        for (var i = 0; i < methodCount; i++)
        {
            var methodInfo = methods[i];

            if (!methodFilter(methodInfo))
                continue;

            var metadata = GetCachedParameterMetadata(methodInfo);
            var parameters = metadata.Parameters.Where(static parameter => !parameter.IsDefined(typeof(InjectTypeAttribute))).ToArray();
            var optionalParametersCount = metadata.OptionalParametersCount;
            var notAnnotatedParametersCount = metadata.NotAnnotatedParametersCount;
            var paramsParameter = metadata.ParamsParameters;

            if (paramsParameter.HasParameters() && methodArgs.Count < notAnnotatedParametersCount - optionalParametersCount - 1)
                continue;

            if (!paramsParameter.HasParameters() &&
                (HasMoreArgumentsThanMethodDefinitionContains(methodArgs, notAnnotatedParametersCount) ||
                 !CanUseSomeArgumentsAsDefaultParameters(methodArgs, notAnnotatedParametersCount,
                     optionalParametersCount)))
                continue;

            var hasMatchedArgTypes = true;
            for (int f = 0,
                 g = paramsParameter.HasParameters()
                     ? Math.Min(methodArgs.Count, notAnnotatedParametersCount - 1)
                     : methodArgs.Count;
                 f < g;
                 ++f)
            {
                var rawParam = parameters[f].ParameterType;
                var param = rawParam.GetUnderlyingNullable();
                var arg = methodArgs[f];

                if (IsParameterTypeCompatible(rawParam, param, arg))
                    continue;

                hasMatchedArgTypes = false;
                break;
            }

            if (paramsParameter.HasParameters() && methodArgs.Count > notAnnotatedParametersCount - 1)
            {
                var paramsStartIndex = notAnnotatedParametersCount - 1;
                var elementType = global::Musoq.Plugins.VariadicParameter.ElementType(parameters[^1].ParameterType)!;
                for (var argumentIndex = paramsStartIndex; argumentIndex < methodArgs.Count; argumentIndex++)
                {
                    if (IsParameterTypeCompatible(elementType, elementType.GetUnderlyingNullable(), methodArgs[argumentIndex]))
                        continue;
                    hasMatchedArgTypes = false;
                    break;
                }
            }

            if (!hasMatchedArgTypes)
                continue;

            if (entityType is not null && !IsMethodCompatibleWithEntityType(methodInfo, entityType))
                continue;

            candidates.Add(methodInfo);
        }

        if (candidates.Count == 0)
        {
            index = -1;
            return false;
        }


        index = GetCachedMethodIndex(name, SelectBestMethod(name, methodArgs, candidates));
        return true;
    }
}
