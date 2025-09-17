using System;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
/// Exception thrown when a method cannot be resolved during query evaluation.
/// Provides detailed information about the method resolution failure and suggestions.
/// </summary>
public class CannotResolveMethodException : Exception
{
    public string MethodName { get; }
    public string[] ArgumentTypes { get; }
    public string[] AvailableSignatures { get; }

    public CannotResolveMethodException(string message, string methodName = null, string[] argumentTypes = null, string[] availableSignatures = null)
        : base(message)
    {
        MethodName = methodName ?? string.Empty;
        ArgumentTypes = argumentTypes ?? new string[0];
        AvailableSignatures = availableSignatures ?? new string[0];
    }

    public static CannotResolveMethodException CreateForNullArguments(string methodName)
    {
        var message = $"Cannot resolve method '{methodName}' because one or more arguments are null. " +
                     "This usually indicates a problem with column references or expressions. " +
                     "Please check that all referenced columns exist and expressions are valid.";

        return new CannotResolveMethodException(message, methodName);
    }
    
    public static CannotResolveMethodException CreateForCannotMatchMethodNameOrArguments(string methodName, Node[] args, string[] availableSignatures = null)
    {
        var argTypes = args?.Length > 0
            ? args.Where(a => a?.ReturnType != null).Select(f => f.ReturnType.ToString()).ToArray()
            : new string[0];
        
        var argumentsText = argTypes.Length > 0 ? string.Join(", ", argTypes) : "no arguments";
        
        var availableText = availableSignatures?.Length > 0
            ? $"\n\nAvailable method signatures:\n{string.Join("\n", availableSignatures.Select(s => $"- {s}"))}"
            : "\n\nNo matching methods found. Please check the method name and available functions.";

        var message = $"Cannot resolve method '{methodName}' with arguments ({argumentsText}).{availableText}" +
                     "\n\nPlease check:\n" +
                     "- Method name spelling\n" +
                     "- Number and types of arguments\n" +
                     "- Available functions in the current context";

        return new CannotResolveMethodException(message, methodName, argTypes, availableSignatures);
    }

    public static CannotResolveMethodException CreateForAmbiguousMatch(string methodName, Node[] args, string[] matchingSignatures)
    {
        var argTypes = args?.Where(a => a?.ReturnType != null).Select(f => f.ReturnType.ToString()).ToArray() ?? new string[0];
        var argumentsText = argTypes.Length > 0 ? string.Join(", ", argTypes) : "no arguments";
        
        var matchesText = matchingSignatures?.Length > 0
            ? $"\n\nAmbiguous matches:\n{string.Join("\n", matchingSignatures.Select(s => $"- {s}"))}"
            : "";

        var message = $"The method call '{methodName}({argumentsText})' is ambiguous.{matchesText}" +
                     "\n\nPlease provide more specific argument types or use explicit casting to resolve the ambiguity.";

        return new CannotResolveMethodException(message, methodName, argTypes, matchingSignatures);
    }

    public static CannotResolveMethodException CreateForUnsupportedOperation(string methodName, string context)
    {
        var message = $"The method '{methodName}' is not supported in {context}. " +
                     "Some operations may not be available in certain query contexts or with specific data types. " +
                     "Please check the documentation for supported operations.";

        return new CannotResolveMethodException(message, methodName);
    }
}