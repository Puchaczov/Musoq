using System.Linq;
using Musoq.Plugins;

namespace Musoq.Schema.Helpers;

/// <summary>Formats the SQL contract without exposing implementation factories or state.</summary>
public static class SqlFunctionSignature
{
    /// <summary>Formats a public declaration's result, name, arguments, and window context.</summary>
    public static string Format(SqlFunctionDescriptor function)
    {
        ArgumentNullException.ThrowIfNull(function);
        var arguments = string.Join(", ", function.Parameters.Select(static parameter =>
            $"{FormatArgumentType(parameter.Type)} {parameter.Name}"));
        var genericParameters = function.Declaration.IsGenericMethodDefinition
            ? $"<{string.Join(", ", function.Declaration.GetGenericArguments().Select(static type => type.Name))}>"
            : string.Empty;
        var suffix = function.Kind == SqlFunctionKind.Window ? " OVER (...)" : string.Empty;
        return $"{CSharpTypeNameHelper.GetCSharpTypeName(function.ResultType)} {function.Name}{genericParameters}({arguments}){suffix}";
    }
    private static string FormatArgumentType(Type type)
        => VariadicParameter.IsNativeSpan(type)
            ? CSharpTypeNameHelper.GetCSharpTypeName(VariadicParameter.ElementType(type)!) + "..."
            : CSharpTypeNameHelper.GetCSharpTypeName(type);
}
