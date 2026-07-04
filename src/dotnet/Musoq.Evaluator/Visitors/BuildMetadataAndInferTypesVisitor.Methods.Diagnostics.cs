using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Visitors.SemanticExpressionDiagnosticFacts;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static CannotResolveMethodException CreateMethodResolutionExceptionWithSuggestion(
        string methodName, Node[] args, MethodResolutionContext context)
    {
        var allMethods = context.SchemaTablePair.Schema.GetAllLibraryMethods();
        var availableNames = allMethods.Keys;
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(methodName, availableNames, maxDistance: 2);

        if (TryCreateScriptParameterMethodResolutionMessage(methodName, args, allMethods, out var parameterMessage))
            return new CannotResolveMethodException(parameterMessage);

        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            var types = args.Length > 0
                ? string.Join(", ", args.Select(f => f.ReturnType?.ToString() ?? "null"))
                : string.Empty;

            var message = string.IsNullOrEmpty(types)
                ? $"Method {methodName} cannot be resolved. Did you mean '{suggestion}'?"
                : $"Method {methodName} with argument types {types} cannot be resolved. Did you mean '{suggestion}'?";

            return new CannotResolveMethodException(message);
        }

        return CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments(methodName, args);
    }

    private static bool TryCreateScriptParameterMethodResolutionMessage(
        string methodName,
        Node[] args,
        IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> allMethods,
        out string message)
    {
        var parameters = CollectScriptParameterReferences(args);
        if (parameters.Count == 0)
        {
            message = string.Empty;
            return false;
        }

        var argumentTypes = args.Length > 0
            ? string.Join(", ", args.Select(arg => FormatTypeName(arg.ReturnType)))
            : string.Empty;
        var parameterDescriptions = string.Join(
            ", ",
            parameters.Select(parameter => $"${parameter.Name} ({FormatTypeName(parameter.ReturnType)})"));
        var overloads = GetMethodOverloads(methodName, allMethods);
        var expectedShapes = overloads.Count == 0
            ? string.Empty
            : $" Expected overloads include: {FormatMethodOverloadShapes(overloads)}.";

        message = $"Method {methodName} with argument types {argumentTypes} cannot be resolved. " +
                  $"Script parameter argument(s) {parameterDescriptions} use their declared types during method resolution." +
                  expectedShapes +
                  " Declare the parameter with a compatible type or use an explicit conversion.";
        return true;
    }

    private static List<ParameterReferenceNode> CollectScriptParameterReferences(IEnumerable<Node> nodes)
    {
        var parameters = new List<ParameterReferenceNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in nodes)
            CollectScriptParameterReferences(node, parameters, seen);

        return parameters;
    }

    private static void CollectScriptParameterReferences(
        Node? node,
        ICollection<ParameterReferenceNode> parameters,
        ISet<string> seen)
    {
        switch (node)
        {
            case null:
                return;
            case ParameterReferenceNode parameter:
                if (seen.Add(parameter.Name))
                    parameters.Add(parameter);
                return;
            case BinaryNode binary:
                CollectScriptParameterReferences(binary.Left, parameters, seen);
                CollectScriptParameterReferences(binary.Right, parameters, seen);
                return;
            case UnaryNode unary:
                CollectScriptParameterReferences(unary.Expression, parameters, seen);
                return;
            case ArgsListNode args:
                foreach (var arg in args.Args)
                    CollectScriptParameterReferences(arg, parameters, seen);
                return;
            case AccessMethodNode method:
                CollectScriptParameterReferences(method.Arguments, parameters, seen);
                CollectScriptParameterReferences(method.ExtraAggregateArguments, parameters, seen);
                return;
            case BetweenNode between:
                CollectScriptParameterReferences(between.Expression, parameters, seen);
                CollectScriptParameterReferences(between.Min, parameters, seen);
                CollectScriptParameterReferences(between.Max, parameters, seen);
                return;
            case CaseNode caseNode:
                foreach (var (when, then) in caseNode.WhenThenPairs)
                {
                    CollectScriptParameterReferences(when, parameters, seen);
                    CollectScriptParameterReferences(then, parameters, seen);
                }

                CollectScriptParameterReferences(caseNode.Else, parameters, seen);
                return;
            case FieldNode field:
                CollectScriptParameterReferences(field.Expression, parameters, seen);
                return;
            case IsNullNode isNull:
                CollectScriptParameterReferences(isNull.Expression, parameters, seen);
                return;
        }
    }

    private static IReadOnlyList<MethodInfo> GetMethodOverloads(
        string methodName,
        IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> allMethods)
    {
        foreach (var (name, methods) in allMethods)
        {
            if (string.Equals(name, methodName, StringComparison.OrdinalIgnoreCase))
                return methods;
        }

        return [];
    }

    private static string FormatMethodOverloadShapes(IReadOnlyList<MethodInfo> overloads)
    {
        var shapes = overloads
            .Take(4)
            .Select(FormatMethodShape)
            .ToArray();
        var suffix = overloads.Count > shapes.Length
            ? $", and {overloads.Count - shapes.Length} more"
            : string.Empty;

        return string.Join(", ", shapes) + suffix;
    }

    private static string FormatMethodShape(MethodInfo method)
    {
        var parameterTypes = method
            .GetParameters()
            .Where(parameter => parameter.GetCustomAttribute<InjectSpecificSourceAttribute>() == null)
            .Select(parameter => FormatTypeName(parameter.ParameterType));

        return $"{method.Name}({string.Join(", ", parameterTypes)})";
    }
}
