using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Helpers;

public static class CSharpTypeNameHelper
{
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

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericTypeName = genericTypeDefinition.Name;

            var tickIndex = genericTypeName.IndexOf('`');
            if (tickIndex > 0) genericTypeName = genericTypeName.Substring(0, tickIndex);

            var genericArgs = type.GetGenericArguments();
            var argNames = genericArgs.Select(GetCSharpTypeName);

            return $"{genericTypeName}<{string.Join(", ", argNames)}>";
        }

        return GetCSharpTypeAlias(type);
    }

    public static string GetCSharpTypeAlias(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(object)) return "object";
        if (type == typeof(string)) return "string";
        if (type == typeof(void)) return "void";

        return type.Name;
    }

    public static string FormatMethodSignature(MethodInfo methodInfo, bool includeNamespace = false)
    {
        if (methodInfo == null)
            throw new ArgumentNullException(nameof(methodInfo));

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
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].GetCustomAttribute<InjectTypeAttribute>() != null)
                continue;

            if (paramIndex > 0)
                signature.Append(", ");

            signature.Append($"{GetCSharpTypeName(parameters[i].ParameterType)} {parameters[i].Name}");
            paramIndex++;
        }

        signature.Append(")");

        return signature.ToString();
    }
}