using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public class MethodsMetadata
{
    private static readonly Dictionary<Type, Type[]> TypeCompatibilityTable = new()
    {
        { typeof(bool), [typeof(bool)] },
        { typeof(short), [typeof(short)] },
        { typeof(int), [typeof(int), typeof(short)] },
        { typeof(long), [typeof(long), typeof(int), typeof(short)] },
        { typeof(DateTimeOffset), [typeof(DateTimeOffset)] },
        { typeof(DateTime), [typeof(DateTime)] },
        { typeof(string), [typeof(string)] },
        { typeof(decimal), [typeof(decimal)] },
        { typeof(TimeSpan), [typeof(TimeSpan)] }
    };

    private static readonly Dictionary<Type, HashSet<Type>> ValidImplicitConversions = new()
    {
        [typeof(sbyte)] =
            [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] =
        [
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
            typeof(double), typeof(decimal)
        ],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] =
        [
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
        ],
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(char)] =
        [
            typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal)
        ]
    };

    private static readonly Dictionary<(Type, Type), int> ConversationCosts = new()
    {
        [(typeof(sbyte), typeof(short))] = 1,
        [(typeof(sbyte), typeof(int))] = 2,
        [(typeof(sbyte), typeof(long))] = 3,
        [(typeof(sbyte), typeof(float))] = 4,
        [(typeof(sbyte), typeof(double))] = 5,
        [(typeof(sbyte), typeof(decimal))] = 6,

        [(typeof(byte), typeof(short))] = 1,
        [(typeof(byte), typeof(ushort))] = 1,
        [(typeof(byte), typeof(int))] = 2,
        [(typeof(byte), typeof(uint))] = 2,
        [(typeof(byte), typeof(long))] = 3,
        [(typeof(byte), typeof(ulong))] = 3,
        [(typeof(byte), typeof(float))] = 4,
        [(typeof(byte), typeof(double))] = 5,
        [(typeof(byte), typeof(decimal))] = 6,

        [(typeof(short), typeof(int))] = 1,
        [(typeof(short), typeof(long))] = 2,
        [(typeof(short), typeof(float))] = 3,
        [(typeof(short), typeof(double))] = 4,
        [(typeof(short), typeof(decimal))] = 5,

        [(typeof(ushort), typeof(int))] = 1,
        [(typeof(ushort), typeof(uint))] = 1,
        [(typeof(ushort), typeof(long))] = 2,
        [(typeof(ushort), typeof(ulong))] = 2,
        [(typeof(ushort), typeof(float))] = 3,
        [(typeof(ushort), typeof(double))] = 4,
        [(typeof(ushort), typeof(decimal))] = 5,

        [(typeof(int), typeof(long))] = 1,
        [(typeof(int), typeof(float))] = 2,
        [(typeof(int), typeof(double))] = 2,
        [(typeof(int), typeof(decimal))] = 3,

        [(typeof(uint), typeof(long))] = 1,
        [(typeof(uint), typeof(ulong))] = 1,
        [(typeof(uint), typeof(float))] = 2,
        [(typeof(uint), typeof(double))] = 2,
        [(typeof(uint), typeof(decimal))] = 3,

        [(typeof(long), typeof(float))] = 1,
        [(typeof(long), typeof(double))] = 1,
        [(typeof(long), typeof(decimal))] = 2,

        [(typeof(ulong), typeof(float))] = 1,
        [(typeof(ulong), typeof(double))] = 1,
        [(typeof(ulong), typeof(decimal))] = 2,

        [(typeof(float), typeof(double))] = 1,

        [(typeof(char), typeof(ushort))] = 1,
        [(typeof(char), typeof(int))] = 2,
        [(typeof(char), typeof(uint))] = 2,
        [(typeof(char), typeof(long))] = 3,
        [(typeof(char), typeof(ulong))] = 3,
        [(typeof(char), typeof(float))] = 4,
        [(typeof(char), typeof(double))] = 5,
        [(typeof(char), typeof(decimal))] = 6
    };

    private readonly Dictionary<(string Name, MethodInfo Method), int> _methodIndexCache = new();

    private readonly Dictionary<string, List<MethodInfo>> _methods;
    private readonly Dictionary<string, string> _normalizedToOriginalMethodNames;

    private readonly Dictionary<MethodInfo, ParameterMetadataInfo> _parameterMetadataCache = new();

    /// <summary>
    ///     Initialize object.
    /// </summary>
    public MethodsMetadata()
    {
        _methods = new Dictionary<string, List<MethodInfo>>();
        _normalizedToOriginalMethodNames = new Dictionary<string, string>();
    }

    private ParameterMetadataInfo GetCachedParameterMetadata(MethodInfo method)
    {
        if (_parameterMetadataCache.TryGetValue(method, out var cached))
            return cached;

        var parameters = method.GetParameters();
        var metadata = new ParameterMetadataInfo(parameters);
        _parameterMetadataCache[method] = metadata;
        return metadata;
    }

    private ParameterInfo[] GetCachedParameters(MethodInfo method)
    {
        return GetCachedParameterMetadata(method).Parameters;
    }

    private int GetCachedMethodIndex(string name, MethodInfo method)
    {
        return _methodIndexCache.TryGetValue((name, method), out var index)
            ? index
            : _methods[name].IndexOf(method);
    }

    /// <summary>
    ///     Gets method that fits name and types of arguments passed.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="entityType">Type of entity.</param>
    /// <returns>Method that fits requirements.</returns>
    public MethodInfo GetMethod(string name, Type[] methodArgs, Type entityType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "resolving a method");

        if (methodArgs == null)
            throw SchemaArgumentException.ForNullArgument(nameof(methodArgs), "resolving a method");

        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index, out var actualMethodName))
        {
            var availableSignatures = GetAvailableMethodSignatures(name);
            var providedTypes = methodArgs.Select(arg => arg?.Name ?? "null").ToArray();

            throw MethodResolutionException.ForUnresolvedMethod(name, providedTypes, availableSignatures);
        }

        return _methods[actualMethodName][index];
    }

    /// <summary>
    ///     Gets the registered method if exists.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="methodArgs">The types of arguments methods contains.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="result">Method metadata of founded method.</param>
    /// <returns>True if method exists, otherwise false.</returns>
    public bool TryGetMethod(string name, Type[] methodArgs, Type entityType, out MethodInfo result)
    {
        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index, out var actualMethodName))
        {
            result = null;
            return false;
        }

        result = _methods[actualMethodName][index];
        return true;
    }

    /// <summary>
    ///     Tries to match method as if it weren't annotated. Assume that method specified parameters explicitly.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="result">Method metadata of founded method.</param>
    /// <returns>True if some method fits, else false.</returns>
    public bool TryGetRawMethod(string name, Type[] methodArgs, out MethodInfo result)
    {
        if (!TryGetRawMethod(name, methodArgs, out var index, out var actualMethodName))
        {
            result = null;
            return false;
        }

        result = _methods[actualMethodName][index];
        return true;
    }

    /// <summary>
    ///     Register new method.
    /// </summary>
    /// <param name="methodInfo">Method to register.</param>
    protected void RegisterMethod(MethodInfo methodInfo)
    {
        RegisterMethod(methodInfo.Name, methodInfo);
    }

    private bool TryGetRawMethod(string name, Type[] methodArgs, out int index, out string actualMethodName)
    {
        if (TryGetRawMethodByExactName(name, methodArgs, out index))
        {
            actualMethodName = name;
            return true;
        }


        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (_normalizedToOriginalMethodNames.TryGetValue(normalizedName, out var originalName))
            if (TryGetRawMethodByExactName(originalName, methodArgs, out index))
            {
                actualMethodName = originalName;
                return true;
            }

        index = -1;
        actualMethodName = null;
        return false;
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

    private bool TryGetAnnotatedMethod(string name, IReadOnlyList<Type> methodArgs, Type entityType, out int index,
        out string actualMethodName)
    {
        if (TryGetAnnotatedMethodByExactName(name, methodArgs, entityType, out index))
        {
            actualMethodName = name;
            return true;
        }

        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (_normalizedToOriginalMethodNames.TryGetValue(normalizedName, out var originalName))
            if (TryGetAnnotatedMethodByExactName(originalName, methodArgs, entityType, out index))
            {
                actualMethodName = originalName;
                return true;
            }

        index = -1;
        actualMethodName = null;
        return false;
    }

    private bool TryGetAnnotatedMethodByExactName(string name, IReadOnlyList<Type> methodArgs, Type entityType,
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
            scoredMethods[i] = (MeasureHowCloseTheMethodIsAgainstTheArguments(methods[i]), i);

        scoredMethods.Sort((a, b) => a.Score.CompareTo(b.Score));

        MethodInfo firstMatchMethod = null;
        MethodInfo secondMatchMethod = null;

        for (var i = 0; i < methodCount; i++)
        {
            var methodOriginalIndex = scoredMethods[i].Index;
            var methodInfo = methods[methodOriginalIndex];
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

                if (IsTypePossibleToConvert(param, arg) ||
                    CanSafelyPassNull(rawParam, arg) ||
                    (param.IsGenericParameter && TypeConformsToConstraints(param, arg)) ||
                    ((arg.IsArray || arg.GetInterface("IEnumerable") != null) && param.IsGenericType &&
                     param.Name == "IEnumerable`1") ||
                    (param.IsGenericType && arg.IsGenericType && param.Name == "IEnumerable`1" &&
                     arg.Name == "IEnumerable`1") ||
                    (param.IsArray && param.GetElementType().IsGenericParameter && arg.IsArray) ||
                    (arg.IsArray && arg.GetElementType().IsGenericParameter)
                   )
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
                hasMatchedArgTypes = paramType.GetUnderlyingNullable() == arrayType ||
                                     CanBeAssignedFromGeneric(paramType, arrayType);
            }

            if (!hasMatchedArgTypes)
                continue;


            if (entityType is not null)
            {
                var injectTypeAttributes = GetInjectTypeAttribute(methodInfo);
                var injectTypeAttribute = injectTypeAttributes.SingleOrDefault(f => f is InjectSpecificSourceAttribute,
                    injectTypeAttributes.FirstOrDefault());

                if (injectTypeAttribute is null)
                    goto breakAll;

                var isGroupAttribute = injectTypeAttribute is InjectGroupAttribute;

                if (isGroupAttribute)
                    goto breakAll;

                var isQueryStatsAttribute = injectTypeAttribute is InjectQueryStatsAttribute;

                if (isQueryStatsAttribute)
                    goto breakAll;

                if (!IsEntityTypeInjectableIntoMethod(entityType, injectTypeAttribute))
                    continue;
            }

            breakAll:

            if (firstMatchMethod == null)
                firstMatchMethod = methodInfo;
            else if (secondMatchMethod == null) secondMatchMethod = methodInfo;
        }

        if (firstMatchMethod == null)
        {
            index = -1;
            return false;
        }


        index = GetCachedMethodIndex(name, firstMatchMethod);
        return true;

        int MeasureHowCloseTheMethodIsAgainstTheArguments(MethodInfo registeredMethod)
        {
            var parameters = GetCachedParameters(registeredMethod);
            var howClosePassedTypesAre = 0;

            if (parameters.Length < methodArgs.Count) return int.MaxValue;

            for (int i = methodArgs.Count - 1, j = 0; i >= j; --i)
            {
                var param = parameters[i].ParameterType.GetUnderlyingNullable();
                var arg = methodArgs[i].GetUnderlyingNullable();

                if (param == arg)
                    howClosePassedTypesAre += 0;
                else if (param.IsAssignableFrom(arg))
                    howClosePassedTypesAre += GetInheritanceDepth(arg, param);
                else if (CanImplicitlyConvert(arg, param))
                    howClosePassedTypesAre += GetNumericConversionCost(arg, param);
                else if (param.IsGenericParameter)
                    howClosePassedTypesAre += 999;
                else
                    break;
            }

            return howClosePassedTypesAre;
        }

        int GetInheritanceDepth(Type derived, Type target)
        {
            if (derived == target) return 0;

            var depth = 0;
            var current = derived;

            while (current != null && current != target)
            {
                depth++;
                current = current.BaseType;
            }

            return current == null ? -1 : depth;
        }
    }

    private static bool CanUseSomeArgumentsAsDefaultParameters(IReadOnlyCollection<Type> methodArgs,
        int parametersCount, int optionalParametersCount)
    {
        return methodArgs.Count >= parametersCount - optionalParametersCount && methodArgs.Count <= parametersCount;
    }

    private static bool HasMoreArgumentsThanMethodDefinitionContains(IReadOnlyList<Type> methodArgs,
        int parametersCount)
    {
        return methodArgs.Count > parametersCount;
    }

    private void RegisterMethod(string name, MethodInfo methodInfo)
    {
        int index;
        if (_methods.TryGetValue(name, out var method))
        {
            index = method.Count;
            method.Add(methodInfo);
        }
        else
        {
            index = 0;
            _methods.Add(name, [methodInfo]);
        }

        _methodIndexCache[(name, methodInfo)] = index;

        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (normalizedName != name)
            if (!_normalizedToOriginalMethodNames.ContainsKey(normalizedName))
                _normalizedToOriginalMethodNames[normalizedName] = name;
    }

    private static bool CanBeAssignedFromGeneric(Type paramType, Type arrayType)
    {
        var isParamArray = paramType.IsArray;

        if (!isParamArray)
            return false;

        var paramElementType = paramType.GetElementType()!;
        var isParamGeneric = paramElementType.IsGenericParameter || paramElementType.IsArray;
        var isArrayArray = arrayType.IsArray;

        return isParamGeneric && isArrayArray;
    }

    private static bool IsEntityTypeInjectableIntoMethod(Type entityType, InjectTypeAttribute injectTypeAttribute)
    {
        return entityType.IsAssignableTo(injectTypeAttribute.InjectType);
    }

    private InjectTypeAttribute[] GetInjectTypeAttribute(MethodInfo methodInfo)
    {
        return GetCachedParameterMetadata(methodInfo).InjectTypeAttributes;
    }

    private static bool IsTypePossibleToConvert(Type to, Type from)
    {
        if (from == typeof(IDynamicMetaObjectProvider))
            return true;
        if (TypeCompatibilityTable.TryGetValue(to, out var value))
            return value.Any(f => f == from);
        return to == from || to.IsAssignableFrom(from);
    }

    private static bool CanSafelyPassNull(Type to, Type from)
    {
        if (from.FullName != typeof(NullNode.NullType).FullName)
            return false;
        return (to.IsGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>))
               || to.IsGenericParameter
               || !to.IsValueType;
    }

    private static bool TypeConformsToConstraints(Type genericType, Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        var interfaces = genericType.GetGenericParameterConstraints()
            .Where(t => t.IsInterface);

        if (interfaces.Any(@interface => !effectiveType.GetInterfaces().Contains(@interface))) return false;

        var specialConstraints = genericType.GenericParameterAttributes;

        if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
            && !effectiveType.IsValueType)
            return false;

        if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0
            && effectiveType.IsValueType)
            return false;

        var baseConstraint = genericType.GetGenericParameterConstraints()
            .FirstOrDefault(t => !t.IsInterface);
        if (baseConstraint != null)
            if (!baseConstraint.IsAssignableFrom(effectiveType))
                return false;

        if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) == 0) return true;

        if (effectiveType.IsValueType) return true;

        var constructor = effectiveType.GetConstructor(Type.EmptyTypes);

        return constructor != null;
    }

    private static Type FindCommonBaseType(params Type[] types)
    {
        if (types == null || types.Length == 0)
            return typeof(object);

        if (types.Length == 1)
            return types[0];

        var commonBaseTypes = GetTypeHierarchy(types[0]);

        for (var i = 1; i < types.Length; i++)
        {
            var currentHierarchy = GetTypeHierarchy(types[i]);
            commonBaseTypes.IntersectWith(currentHierarchy);

            if (commonBaseTypes.Count == 1 && commonBaseTypes.Single() == typeof(object))
                return typeof(object);
        }

        return FindMostSpecificType(commonBaseTypes);
    }

    private static Type FindCommonBaseType(IReadOnlyList<Type> types, int startIndex)
    {
        var count = types.Count - startIndex;
        if (count <= 0)
            return typeof(object);

        if (count == 1)
            return types[startIndex];

        var commonBaseTypes = GetTypeHierarchy(types[startIndex]);

        for (var i = startIndex + 1; i < types.Count; i++)
        {
            var currentHierarchy = GetTypeHierarchy(types[i]);
            commonBaseTypes.IntersectWith(currentHierarchy);

            if (commonBaseTypes.Count == 1 && commonBaseTypes.Single() == typeof(object))
                return typeof(object);
        }

        return FindMostSpecificType(commonBaseTypes);
    }

    private static HashSet<Type> GetTypeHierarchy(Type type)
    {
        var hierarchy = new HashSet<Type>();
        if (type == null)
            return hierarchy;

        var current = type;
        while (current != null)
        {
            hierarchy.Add(current);
            current = current.BaseType;
        }

        return hierarchy;
    }

    private static Type FindMostSpecificType(HashSet<Type> types)
    {
        if (types == null || types.Count == 0)
            return typeof(object);

        var mostSpecific = typeof(object);
        foreach (var type in types)
        {
            if (mostSpecific == typeof(object))
            {
                mostSpecific = type;
                continue;
            }

            if (type.IsSubclassOf(mostSpecific))
                mostSpecific = type;
        }

        return mostSpecific;
    }

    private static bool CanImplicitlyConvert(Type from, Type to)
    {
        if (from == null || to == null)
            return false;

        if (!from.IsPrimitive || !to.IsPrimitive) return false;

        return ValidImplicitConversions.ContainsKey(from) && ValidImplicitConversions[from].Contains(to);
    }

    private static int GetNumericConversionCost(Type from, Type to)
    {
        return ConversationCosts.GetValueOrDefault((from, to), int.MaxValue);
    }

    private string[] GetAvailableMethodSignatures(string methodName)
    {
        if (!_methods.TryGetValue(methodName, out var methods))
            return [];

        return methods.Select(m =>
        {
            var parameters = GetCachedParameters(m);
            var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
            return $"{methodName}({string.Join(", ", paramTypes)})";
        }).ToArray();
    }

    /// <summary>
    ///     Gets all registered methods with their metadata.
    /// </summary>
    /// <returns>Dictionary of method names to their MethodInfo list.</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllMethods()
    {
        return _methods.ToDictionary(
            kvp => kvp.Key, IReadOnlyList<MethodInfo> (kvp) => kvp.Value.AsReadOnly()
        );
    }
}
