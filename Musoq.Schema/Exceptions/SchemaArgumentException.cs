using System;

namespace Musoq.Schema.Exceptions;

/// <summary>
///     Exception thrown when invalid arguments are provided to schema operations.
///     Provides detailed information about what went wrong with the arguments.
/// </summary>
public class SchemaArgumentException : ArgumentException
{
    public SchemaArgumentException(string argumentName, string message)
        : base(message, argumentName)
    {
    }

    public SchemaArgumentException(string argumentName, string message, Exception innerException)
        : base(message, argumentName, innerException)
    {
    }

    public static SchemaArgumentException ForNullArgument(string argumentName, string operationContext)
    {
        return new SchemaArgumentException(
            argumentName,
            $"The argument '{argumentName}' cannot be null when {operationContext}. Please provide a valid value."
        );
    }

    public static SchemaArgumentException ForEmptyString(string argumentName, string operationContext)
    {
        return new SchemaArgumentException(
            argumentName,
            $"The argument '{argumentName}' cannot be empty or whitespace when {operationContext}. Please provide a non-empty value."
        );
    }

    public static SchemaArgumentException ForInvalidMethodName(string methodName, string availableMethods)
    {
        return new SchemaArgumentException(
            nameof(methodName),
            $"The method '{methodName}' is not recognized. Available methods are: {availableMethods}. Please check the method name and try again."
        );
    }
}