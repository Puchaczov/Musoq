using System;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

public class CannotResolveMethodException(string message)
    : Exception(message)
{
    public static CannotResolveMethodException CreateForNullArguments(string methodName)
    {
        return new CannotResolveMethodException($"Method {methodName} cannot be resolved because of null arguments");
    }

    public static CannotResolveMethodException CreateForCannotMatchMethodNameOrArguments(string methodName, Node[] args)
    {
        var types = args.Length > 0
            ? args.Select(f => f.ReturnType.ToString()).Aggregate((a, b) => a + ", " + b)
            : string.Empty;

        return new CannotResolveMethodException(
            $"Method {methodName} with argument types {string.Join(", ", types)} cannot be resolved");
    }
}