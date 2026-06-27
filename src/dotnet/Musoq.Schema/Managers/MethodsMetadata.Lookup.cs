using System.Collections.Generic;
using System.Reflection;
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
        Span<(int Score, int Index)> scoredMethods = methodCount <= 32
            ? stackalloc (int, int)[methodCount]
            : new (int, int)[methodCount];

        for (var i = 0; i < methodCount; i++)
            scoredMethods[i] = methodFilter(methods[i])
                ? (MeasureMethodCloseness(methods[i], methodArgs), i)
                : (int.MaxValue, i);

        scoredMethods.Sort((a, b) =>
        {
            var scoreComparison = a.Score.CompareTo(b.Score);
            if (scoreComparison != 0)
                return scoreComparison;

            return GetMethodResolutionPriority(methods[a.Index])
                .CompareTo(GetMethodResolutionPriority(methods[b.Index]));
        });

        MethodInfo? firstMatchMethod = null;

        for (var i = 0; i < methodCount; i++)
        {
            var methodOriginalIndex = scoredMethods[i].Index;
            var methodInfo = methods[methodOriginalIndex];

            if (!methodFilter(methodInfo))
                continue;

            var metadata = GetCachedParameterMetadata(methodInfo);
            var parameters = metadata.Parameters;
            var optionalParametersCount = metadata.OptionalParametersCount;
            var notAnnotatedParametersCount = metadata.NotAnnotatedParametersCount;
            var paramsParameter = metadata.ParamsParameters;
            var parametersToInject = metadata.ParametersToInject;


            if (!paramsParameter.HasParameters() &&
                (HasMoreArgumentsThanMethodDefinitionContains(methodArgs, notAnnotatedParametersCount) ||
                 !CanUseSomeArgumentsAsDefaultParameters(methodArgs, notAnnotatedParametersCount,
                     optionalParametersCount)))
                continue;

            var hasMatchedArgTypes = true;
            for (int f = 0,
                 g = paramsParameter.HasParameters()
                     ? Math.Min(methodArgs.Count - (parameters.Length - 1), parameters.Length)
                     : methodArgs.Count;
                 f < g;
                 ++f)
            {
                var rawParam = parameters[f + parametersToInject].ParameterType;
                var param = rawParam.GetUnderlyingNullable();
                var arg = methodArgs[f].GetUnderlyingNullable();

                if (IsParameterTypeCompatible(rawParam, param, arg))
                    continue;

                hasMatchedArgTypes = false;
                break;
            }

            if (paramsParameter.HasParameters() && methodArgs.Count > notAnnotatedParametersCount - 1)
            {
                var paramsStartIndex = notAnnotatedParametersCount - 1;
                var paramsCount = methodArgs.Count - paramsStartIndex;
                var commonType = paramsCount == 1
                    ? methodArgs[paramsStartIndex]
                    : FindCommonBaseType(methodArgs, paramsStartIndex);
                var arrayType = commonType.MakeArrayType();
                var paramType = parameters[^1].ParameterType;
                hasMatchedArgTypes = paramType.GetUnderlyingNullable() == arrayType.GetUnderlyingNullable() ||
                                     CanBeAssignedFromGeneric(paramType, arrayType);
            }

            if (!hasMatchedArgTypes)
                continue;

            if (entityType is not null && !IsMethodCompatibleWithEntityType(methodInfo, entityType))
                continue;

            firstMatchMethod = methodInfo;
            break;
        }

        if (firstMatchMethod == null)
        {
            index = -1;
            return false;
        }


        index = GetCachedMethodIndex(name, firstMatchMethod);
        return true;
    }
}
