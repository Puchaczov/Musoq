using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public class MethodsMetadata
{
    private static readonly Dictionary<Type, Type[]> TypeCompatibilityTable;
    private readonly Dictionary<string, List<MethodInfo>> _methods;

    static MethodsMetadata()
    {
        TypeCompatibilityTable = new Dictionary<Type, Type[]>
        {
            {typeof(bool), new[] {typeof(bool)}},
            {typeof(short), new[] {typeof(short), typeof(bool)}},
            {typeof(int), new[] {typeof(int), typeof(short), typeof(bool)}},
            {typeof(long), new[] {typeof(long), typeof(int), typeof(short), typeof(bool)}},
            {typeof(DateTimeOffset), new[] {typeof(DateTimeOffset), typeof(TimeSpan)}},
            {typeof(DateTime), new[] {typeof(DateTime), typeof(TimeSpan)}},
            {typeof(string), new[] {typeof(string)}},
            {typeof(decimal), new[] {typeof(decimal)}},
            {typeof(TimeSpan), new[]{typeof(TimeSpan)}}
        };
    }

    /// <summary>
    ///     Initialize object.
    /// </summary>
    public MethodsMetadata()
    {
        _methods = new Dictionary<string, List<MethodInfo>>();
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
        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index))
        {
            var args = methodArgs.Length == 0 ? string.Empty : methodArgs.Select(arg => arg.Name).Aggregate((a, b) => a + ", " + b);
            throw new MissingMethodException("Unresolvable", $"{name}({args})");
        }

        return _methods[name][index];
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
        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index))
        {
            result = null;
            return false;
        }

        result = _methods[name][index];
        return true;
    }

    /// <summary>
    ///     Tries match function as if it weren't annotated. Assume that method specified parameters explicitly.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="result">Method metadata of founded method.</param>
    /// <returns>True if some method fits, else false.</returns>
    public bool TryGetRawMethod(string name, Type[] methodArgs, out MethodInfo result)
    {
        if (!TryGetRawMethod(name, methodArgs, out int index))
        {
            result = null;
            return false;
        }

        result = _methods[name][index];
        return true;
    }

    /// <summary>
    /// Register new method.
    /// </summary>
    /// <param name="methodInfo">Method to register.</param>
    protected void RegisterMethod(MethodInfo methodInfo)
    {
        RegisterMethod(methodInfo.Name, methodInfo);
    }

    /// <summary>
    ///     Tries match function as if it weren't annotated. Assume that method specified parameters explicitly.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="index">Index of method that fits requirements.</param>
    /// <returns>True if some method fits, else false.</returns>
    private bool TryGetRawMethod(string name, Type[] methodArgs, out int index)
    {
        if (!_methods.ContainsKey(name))
        {
            index = -1;
            return false;
        }

        var methods = _methods[name];

        for (var i = 0; i < methods.Count; ++i)
        {
            var method = methods[i];
            var parameters = method.GetParameters();

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

    /// <summary>
    ///     Determine if there are registered methods with specific names and types of arguments.
    /// </summary>
    /// <param name="name">Method name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="index">Index of method that fits requirements.</param>
    /// <returns>True if some method fits, else false.</returns>
    private bool TryGetAnnotatedMethod(string name, IReadOnlyList<Type> methodArgs, Type entityType, out int index)
    {
        if (!_methods.ContainsKey(name))
        {
            index = -1;
            return false;
        }

        var methods = _methods[name];

        for (int i = 0, j = methods.Count; i < j; ++i)
        {
            var methodInfo = methods[i];
            var parameters = methodInfo.GetParameters();
            var optionalParametersCount = parameters.CountOptionalParameters();
            var allParameters = parameters.Length;
            var notAnnotatedParametersCount = parameters.CountWithoutParametersAnnotatedBy<InjectTypeAttribute>();
            var paramsParameter = parameters.GetParametersWithAttribute<ParamArrayAttribute>();
            var parametersToInject = allParameters - notAnnotatedParametersCount;

            //Wrong amount of argument's. That's not our function.
            if (!paramsParameter.HasParameters() &&
                (HasMoreArgumentsThanMethodDefinitionContains(methodArgs, notAnnotatedParametersCount) ||
                 !CanUseSomeArgumentsAsDefaultParameters(methodArgs, notAnnotatedParametersCount, optionalParametersCount)))
                continue;

            var hasMatchedArgTypes = true;
            for (int f = 0, g = paramsParameter.HasParameters() ? Math.Min(methodArgs.Count - (parameters.Length - 1), parameters.Length) : methodArgs.Count; f < g; ++f)
            {
                //1. When constant value, it won't be nullable<type> but type.
                //So it is possible to call function with such value. 
                //That's why GetUnderlyingNullable exists here.
                var rawParam = parameters[f + parametersToInject].ParameterType;
                var param = rawParam.GetUnderlyingNullable();
                var arg = methodArgs[f].GetUnderlyingNullable();

                if (IsTypePossibleToConvert(param, arg) ||
                    CanSafelyPassNull(rawParam, arg) ||
                    param.IsGenericParameter ||
                    arg.IsArray && param.IsGenericType && param.Name == "IEnumerable`1" || 
                    param.IsGenericType && arg.IsGenericType && param.Name == "IEnumerable`1" && arg.Name == "IEnumerable`1" ||
                    param.IsArray && param.GetElementType().IsGenericParameter ||
                    arg.IsArray && arg.GetElementType().IsGenericParameter)
                    continue;

                hasMatchedArgTypes = false;
                break;
            }

            if (paramsParameter.HasParameters())
            {
                var paramsParameters = methodArgs.Skip(parameters.Length - 1);
                var arrayType = paramsParameters.ElementAt(0).MakeArrayType();
                var paramType = parameters[^1].ParameterType;
                hasMatchedArgTypes = paramType == arrayType || CanBeAssignedFromGeneric(paramType, arrayType);
            }

            if (!hasMatchedArgTypes)
                continue;

            //When both methods X(A a) and X(B b) exists, we must rely on in what context that method was called.
            //EntityType is used to determine that. It provides information about type of entity that called method was invoked with.
            if (entityType is not null)
            {
                var injectTypeAttributes = GetInjectTypeAttribute(methodInfo);
                var injectTypeAttribute = injectTypeAttributes.SingleOrDefault(f => f is InjectSpecificSourceAttribute, injectTypeAttributes.FirstOrDefault());
                
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

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    /// <summary>
    ///     Determine if there is more or equal values to pass to function that required parameters
    ///     and less or equal parameters than function definition has.
    /// </summary>
    /// <param name="methodArgs">Passed arguments to function.</param>
    /// <param name="parametersCount">All parameters count.</param>
    /// <param name="optionalParametersCount">Optional parameters count.</param>
    /// <returns></returns>
    private static bool CanUseSomeArgumentsAsDefaultParameters(IReadOnlyCollection<Type> methodArgs,
        int parametersCount, int optionalParametersCount)
    {
        return methodArgs.Count >= parametersCount - optionalParametersCount && methodArgs.Count <= parametersCount;
    }

    /// <summary>
    ///     Determine if passed arguments amount is greater than function can contain.
    /// </summary>
    /// <param name="methodArgs">Passed arguments.</param>
    /// <param name="parametersCount">Parameters amount.</param>
    /// <returns></returns>
    private static bool HasMoreArgumentsThanMethodDefinitionContains(IReadOnlyList<Type> methodArgs,
        int parametersCount)
    {
        return methodArgs.Count > parametersCount;
    }
        
    private void RegisterMethod(string name, MethodInfo methodInfo)
    {
        if (_methods.TryGetValue(name, out var method))
            method.Add(methodInfo);
        else
            _methods.Add(name, new List<MethodInfo> {methodInfo});
    }

    private bool CanBeAssignedFromGeneric(Type paramType, Type arrayType)
    {
        return paramType.IsArray && paramType.GetElementType()!.IsGenericParameter && arrayType.IsArray;
    }

    private static bool IsEntityTypeInjectableIntoMethod(Type entityType, InjectTypeAttribute injectTypeAttribute)
    {
        return entityType.IsAssignableTo(injectTypeAttribute.InjectType);
    }

    private static InjectTypeAttribute[] GetInjectTypeAttribute(MethodInfo methodInfo)
    {
        return methodInfo
            .GetParameters()
            .SelectMany(f => f.GetCustomAttributes())
            .Where(f => f.GetType().IsAssignableTo(typeof(InjectTypeAttribute)))
            .Cast<InjectTypeAttribute>()
            .ToArray();
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
        //when it's nullable value type or generic parameter or reference type, we can safely pass null as the compiler will match the method we chose.
        return to.IsGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>)
               || to.IsGenericParameter
               || !to.IsValueType;
    }
}