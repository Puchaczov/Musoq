using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public sealed class ScriptParameterBindingException : InvalidOperationException, IDiagnosticException
{

    public ScriptParameterBindingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ScriptParameterBindingException(string message)
        : base(message)
    {
    }

    public ScriptParameterBindingException()
    {
    }
    private ScriptParameterBindingException(DiagnosticCode code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public DiagnosticCode Code { get; }

    public TextSpan? Span => null;

    public static ScriptParameterBindingException MissingRequired(string name)
    {
        return new ScriptParameterBindingException(
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            $"Required script parameter '{name}' was not provided.");
    }

    public static ScriptParameterBindingException TypeMismatch(
        string name,
        Type expectedType,
        object? value,
        Exception innerException)
    {
        var actualType = value?.GetType();
        return new ScriptParameterBindingException(
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            $"Script parameter '{name}' expected a value of type '{FormatType(expectedType)}' but received '{(actualType != null ? FormatType(actualType) : "null")}'.",
            innerException);
    }

    public static ScriptParameterBindingException NullNotAllowed(string name, Type expectedType)
    {
        return new ScriptParameterBindingException(
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            $"Script parameter '{name}' expected a non-null value of type '{FormatType(expectedType)}'.");
    }

    public static ScriptParameterBindingException Unknown(string name)
    {
        return new ScriptParameterBindingException(
            DiagnosticCode.MQ7006_UnknownScriptParameter,
            $"Script parameter '{name}' was provided but is not declared.");
    }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.ErrorUnknownLocation(
            Code,
            Message,
            sourceKind: DiagnosticSourceKind.Runtime);
    }

    private static string FormatType(Type type)
    {
        var underlyingNullableType = Nullable.GetUnderlyingType(type);
        if (underlyingNullableType != null)
            return $"{FormatType(underlyingNullableType)}?";

        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var suffix = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return $"{FormatType(type.GetElementType()!)}{suffix}";
        }

        if (type.IsGenericType)
        {
            var typeName = type.GetGenericTypeDefinition().Name;
            var aritySeparator = typeName.IndexOf('`');
            if (aritySeparator >= 0)
                typeName = typeName[..aritySeparator];

            var arguments = type.GetGenericArguments();
            var formattedArguments = new string[arguments.Length];
            for (var index = 0; index < arguments.Length; index++)
                formattedArguments[index] = FormatType(arguments[index]);

            return $"{typeName}<{string.Join(", ", formattedArguments)}>";
        }

        if (type == typeof(string))
            return "string";
        if (type == typeof(char))
            return "char";
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(byte))
            return "byte";
        if (type == typeof(sbyte))
            return "sbyte";
        if (type == typeof(short))
            return "short";
        if (type == typeof(ushort))
            return "ushort";
        if (type == typeof(int))
            return "int";
        if (type == typeof(uint))
            return "uint";
        if (type == typeof(long))
            return "long";
        if (type == typeof(ulong))
            return "ulong";
        if (type == typeof(float))
            return "float";
        if (type == typeof(double))
            return "double";
        if (type == typeof(decimal))
            return "decimal";

        return type.Name;
    }
}
