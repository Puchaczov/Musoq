using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Helpers;

public static class CSharpTypeNameHelper
{
    private static readonly ConcurrentDictionary<MethodInfo, string> MethodSignatureCache = new();

    private static readonly Dictionary<Type, string> TypeAliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(object)] = "object",
        [typeof(string)] = "string",
        [typeof(void)] = "void"
    };

    public static string GetCSharpTypeName(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.IsGenericParameter) return type.Name;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return GetCSharpTypeName(underlyingType) + "?";
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var rank = type.GetArrayRank();
            var brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return GetCSharpTypeName(elementType) + brackets;
        }

        if (!type.IsGenericType)
            return GetCSharpTypeAlias(type);

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        var genericTypeName = genericTypeDefinition.Name;

        var tickIndex = genericTypeName.IndexOf('`');
        if (tickIndex > 0) genericTypeName = genericTypeName.Substring(0, tickIndex);

        var genericArgs = type.GetGenericArguments();
        var argNames = genericArgs.Select(GetCSharpTypeName);

        return $"{genericTypeName}<{string.Join(", ", argNames)}>";
    }

    public static string GetCSharpTypeAlias(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return TypeAliases.TryGetValue(type, out var alias) ? alias : type.Name;
    }

    public static string FormatMethodSignature(MethodInfo methodInfo)
    {
        if (methodInfo == null)
            throw new ArgumentNullException(nameof(methodInfo));

        return MethodSignatureCache.GetOrAdd(methodInfo, static mi => FormatMethodSignatureCore(mi));
    }

    private static string FormatMethodSignatureCore(MethodInfo methodInfo)
    {
        var returnTypeName = GetCSharpTypeName(methodInfo.ReturnType);
        var parameters = methodInfo.GetParameters();
        var methodName = methodInfo.Name;

        var signature = new StringBuilder();

        if (methodInfo.IsGenericMethodDefinition)
        {
            var genericParams = methodInfo.GetGenericArguments();
            signature.Append($"{returnTypeName} {methodName}<");
            for (var i = 0; i < genericParams.Length; i++)
            {
                if (i > 0)
                    signature.Append(", ");
                signature.Append(genericParams[i].Name);
            }

            signature.Append(">(");
        }
        else
        {
            signature.Append($"{returnTypeName} {methodName}(");
        }

        var paramIndex = 0;

        foreach (var parameter in parameters)
        {
            if (parameter.GetCustomAttribute<InjectTypeAttribute>() != null)
                continue;

            if (paramIndex > 0)
                signature.Append(", ");

            signature.Append($"{GetCSharpTypeName(parameter.ParameterType)} {parameter.Name}");
            paramIndex++;
        }

        signature.Append(')');

        return signature.ToString();
    }
}
