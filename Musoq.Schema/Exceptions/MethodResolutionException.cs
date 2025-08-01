using System;

namespace Musoq.Schema.Exceptions;

/// <summary>
/// Exception thrown when method resolution fails in schema operations.
/// Provides detailed information about the failed method resolution including available alternatives.
/// </summary>
public class MethodResolutionException : InvalidOperationException
{
    public string MethodName { get; }
    public string[] ProvidedParameterTypes { get; }
    public string[] AvailableSignatures { get; }

    public MethodResolutionException(string methodName, string[] providedParameterTypes, string[] availableSignatures, string message)
        : base(message)
    {
        MethodName = methodName;
        ProvidedParameterTypes = providedParameterTypes;
        AvailableSignatures = availableSignatures;
    }

    public static MethodResolutionException ForUnresolvedMethod(
        string methodName, 
        string[] providedParameterTypes, 
        string[] availableSignatures)
    {
        var providedParams = providedParameterTypes.Length == 0 ? "no parameters" : string.Join(", ", providedParameterTypes);
        var availableOptions = availableSignatures.Length == 0 
            ? "No methods available with this name." 
            : $"Available method signatures: {string.Join("; ", availableSignatures)}";

        var message = $"Cannot resolve method '{methodName}' with parameters ({providedParams}). " +
                     $"{availableOptions} " +
                     "Please check the method name and parameter types.";

        return new MethodResolutionException(methodName, providedParameterTypes, availableSignatures, message);
    }

    public static MethodResolutionException ForAmbiguousMethod(
        string methodName, 
        string[] providedParameterTypes, 
        string[] matchingSignatures)
    {
        var providedParams = string.Join(", ", providedParameterTypes);
        var matches = string.Join("; ", matchingSignatures);

        var message = $"The method call '{methodName}({providedParams})' is ambiguous. " +
                     $"Multiple method signatures match: {matches}. " +
                     "Please provide more specific parameter types to resolve the ambiguity.";

        return new MethodResolutionException(methodName, providedParameterTypes, matchingSignatures, message);
    }
}