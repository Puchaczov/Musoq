using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedScalarGenericBinding
{
    internal static IReadOnlyDictionary<int, string> Resolve(MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments)
    {
        if (!method.IsConstructedGenericMethod ||
            !method.GetGenericArguments().Any(RequiresGeneratedSpecialization))
            return ImmutableDictionary<int, string>.Empty;
        return ResolveNames(method, arguments.Select(static argument => argument switch
        {
            ExecutionFieldRead read => read.GeneratedTypeName,
            ExecutionScriptParameterRead read => read.GeneratedTypeName,
            ExecutionScriptVariableRead read => read.GeneratedTypeName,
            ExecutionMethodCall call => call.GeneratedResultTypeName,
            ExecutionStructuralRecord record => record.GeneratedTypeName,
            ExecutionStructuralArray { GeneratedElementTypeName: { } elementName } => elementName + "[]",
            _ => null
        }).ToArray());
    }

    internal static IReadOnlyDictionary<int, string> ResolveNames(MethodInfo method,
        IReadOnlyList<string?> argumentNames)
    {
        if (!method.IsConstructedGenericMethod)
            return ImmutableDictionary<int, string>.Empty;
        var parameters = method.GetGenericMethodDefinition().GetParameters().Where(static parameter =>
            parameter.GetCustomAttribute<InjectTypeAttribute>() == null).ToArray();
        var names = ImmutableDictionary.CreateBuilder<int, string>();
        for (var index = 0; index < parameters.Length && index < argumentNames.Count; index++)
            if (argumentNames[index] is { Length: > 0 } name)
                BindGeneratedType(parameters[index].ParameterType, name, names);
        foreach (var binding in names.ToArray())
            if (!RequiresGeneratedSpecialization(method.GetGenericArguments()[binding.Key]))
                names.Remove(binding.Key);
        return names.ToImmutable();
    }
    internal static bool RequiresGeneratedSpecialization(Type type)
    {
        if (type == typeof(object) || type == typeof(Musoq.Schema.StructuralInputs.StructuralValue) ||
            type == typeof(Musoq.Schema.StructuralInputs.GeneratedRecordConstraintWitness))
            return true;
        if (type.IsArray)
            return RequiresGeneratedSpecialization(type.GetElementType()!);
        return type.IsGenericType && type.GetGenericArguments().Any(RequiresGeneratedSpecialization);
    }

    private static void BindGeneratedType(Type template, string name,
        ImmutableDictionary<int, string>.Builder names)
    {
        if (template.IsGenericParameter)
        {
            var position = template.GenericParameterPosition;
            if (names.TryGetValue(position, out var existing) && existing != name)
                throw new InvalidOperationException("A generic scalar parameter cannot erase different generated value shapes.");
            names[position] = name;
            return;
        }
        if (!template.ContainsGenericParameters)
            return;
        if (template.IsArray && name.EndsWith("[]", StringComparison.Ordinal))
        {
            BindGeneratedType(template.GetElementType()!, name[..^2], names);
            return;
        }
        if (!template.IsGenericType)
            return;
        var parameters = template.GetGenericArguments();
        // Native arrays satisfy typed collection interfaces without changing their element carrier.
        if (parameters.Length == 1 && name.EndsWith("[]", StringComparison.Ordinal))
        {
            BindGeneratedType(parameters[0], name[..^2], names);
            return;
        }
        var opening = name.IndexOf('<');
        if (opening < 0 || !name.EndsWith('>'))
            return;
        var values = SplitTypeArguments(name[(opening + 1)..^1]);
        if (values.Count != parameters.Length)
            throw new InvalidOperationException("Generated scalar generic arguments do not match their concrete shape.");
        for (var index = 0; index < parameters.Length; index++)
            BindGeneratedType(parameters[index], values[index], names);
    }

    private static List<string> SplitTypeArguments(string arguments)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == '<') depth++;
            else if (arguments[index] == '>') depth--;
            else if (arguments[index] == ',' && depth == 0)
            {
                result.Add(arguments[start..index].Trim());
                start = index + 1;
            }
        }
        result.Add(arguments[start..].Trim());
        return result;
    }

    internal static string? ResolveResult(MethodInfo method, IReadOnlyDictionary<int, string> names)
    {
        if (!method.IsConstructedGenericMethod || names.Count == 0)
            return null;
        var template = method.GetGenericMethodDefinition().ReturnType;
        return template.ContainsGenericParameters ? RenderType(template, method.GetGenericArguments(), names) : null;
    }

    private static string RenderType(Type template, Type[] concrete, IReadOnlyDictionary<int, string> names)
    {
        if (template.IsGenericParameter)
            return names.TryGetValue(template.GenericParameterPosition, out var name)
                ? name : concrete[template.GenericParameterPosition].FullName!.Replace('+', '.');
        if (template.IsArray)
            return RenderType(template.GetElementType()!, concrete, names) + "[]";
        if (!template.IsGenericType)
            return template.FullName!.Replace('+', '.');
        var definitionName = template.GetGenericTypeDefinition().FullName!;
        var tick = definitionName.IndexOf('`');
        return definitionName[..tick].Replace('+', '.') + "<" + string.Join(", ",
            template.GetGenericArguments().Select(type => RenderType(type, concrete, names))) + ">";
    }

    internal static void AppendFingerprint(System.Text.StringBuilder builder, ExecutionMethodCall call)
    {
        builder.Append(":generated-result:").Append(call.GeneratedResultTypeName);
        foreach (var binding in call.GeneratedGenericArguments.OrderBy(static pair => pair.Key))
            builder.Append(":generated-type:").Append(binding.Key).Append('=').Append(binding.Value);
    }

}
