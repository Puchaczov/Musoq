using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

internal readonly struct ParameterMetadataInfo
{
    public readonly ParameterInfo[] Parameters;
    public readonly int OptionalParametersCount;
    public readonly int NotAnnotatedParametersCount;
    public readonly ParameterInfo[] ParamsParameters;
    public readonly int ParametersToInject;
    public readonly InjectTypeAttribute[] InjectTypeAttributes;

    public ParameterMetadataInfo(ParameterInfo[] parameters)
    {
        Parameters = parameters;
        OptionalParametersCount = CountOptional(parameters);
        NotAnnotatedParametersCount = CountWithoutInjectType(parameters);
        ParamsParameters = GetParamsParameters(parameters);
        ParametersToInject = parameters.Length - NotAnnotatedParametersCount;
        InjectTypeAttributes = GetInjectTypeAttributes(parameters);
    }

    private static int CountOptional(ParameterInfo[] parameters)
    {
        var count = 0;
        for (var i = 0; i < parameters.Length; i++)
            if (parameters[i].IsOptional)
                count++;
        return count;
    }

    private static int CountWithoutInjectType(ParameterInfo[] parameters)
    {
        var count = 0;
        for (var i = 0; i < parameters.Length; i++)
            if (parameters[i].GetCustomAttribute<InjectTypeAttribute>() == null)
                count++;
        return count;
    }

    private static ParameterInfo[] GetParamsParameters(ParameterInfo[] parameters)
    {
        foreach (var parameter in parameters)
        {
            var attrs = parameter.GetCustomAttributes();

            if (attrs.Any(attr => attr.GetType().IsAssignableTo(typeof(ParamArrayAttribute)))) return [parameter];
        }

        return [];
    }

    private static InjectTypeAttribute[] GetInjectTypeAttributes(ParameterInfo[] parameters)
    {
        var result = new List<InjectTypeAttribute>();
        foreach (var parameter in parameters)
        {
            var attrs = parameter.GetCustomAttributes();

            result.AddRange(attrs.Where(attr => attr.GetType().IsAssignableTo(typeof(InjectTypeAttribute)))
                .Cast<InjectTypeAttribute>());
        }

        return result.Count > 0 ? result.ToArray() : [];
    }
}
