using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Traversal;

internal static class AstChildren
{
    private static readonly string[] ExcludedNodeProperties =
    [
        nameof(Node.ReturnType),
        nameof(Node.Id),
        nameof(Node.Span),
        nameof(Node.FullSpan),
        nameof(Node.HasSpan)
    ];

    public static IReadOnlyList<AstChild> Of(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var children = new List<AstChild>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        AddObjectChildren(children, string.Empty, node, visited, inspectNodeProperties: true);
        return children;
    }

    public static IReadOnlyList<string> GetTraversalMemberNames(Type nodeType)
    {
        ArgumentNullException.ThrowIfNull(nodeType);

        if (!typeof(Node).IsAssignableFrom(nodeType))
            throw new ArgumentException("Traversal metadata can only be requested for parser node types.", nameof(nodeType));

        return GetProperties(nodeType)
            .Where(property => CanContainNode(property.PropertyType))
            .Select(static property => property.Name)
            .ToArray();
    }

    private static void AddObjectChildren(
        List<AstChild> children,
        string path,
        object? value,
        HashSet<object> visited,
        bool inspectNodeProperties)
    {
        if (value == null)
            return;

        if (value is Node node && !inspectNodeProperties)
        {
            children.Add(new AstChild(path, node));
            return;
        }

        if (value is string or Type)
            return;

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                AddObjectChildren(children, $"{path}[{index}]", item, visited, inspectNodeProperties: false);
                index++;
            }

            return;
        }

        var type = value.GetType();
        var shouldInspect = value is Node && inspectNodeProperties || ShouldInspectContainer(type);
        if (!shouldInspect || !visited.Add(value))
            return;

        foreach (var property in GetProperties(type))
        {
            var childPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
            AddObjectChildren(children, childPath, property.GetValue(value), visited, inspectNodeProperties: false);
        }

        foreach (var field in GetFields(type))
        {
            var childPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
            AddObjectChildren(children, childPath, field.GetValue(value), visited, inspectNodeProperties: false);
        }
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetIndexParameters().Length == 0)
            .Where(static property => !ExcludedNodeProperties.Contains(property.Name, StringComparer.Ordinal))
            .OrderBy(static property => property.DeclaringType == typeof(Node) ? 0 : 1)
            .ThenBy(static property => property.MetadataToken)
            .ToArray();
    }

    private static FieldInfo[] GetFields(Type type)
    {
        return type
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(static field => field.MetadataToken)
            .ToArray();
    }

    private static bool CanContainNode(Type type)
    {
        if (typeof(Node).IsAssignableFrom(type))
            return true;

        if (type == typeof(string) || type == typeof(Type))
            return false;

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            return true;

        return ShouldInspectContainer(type);
    }

    private static bool ShouldInspectContainer(Type type)
    {
        return IsValueTuple(type) ||
               (type.Namespace?.StartsWith("Musoq.Parser.Nodes", StringComparison.Ordinal) == true &&
                !typeof(Node).IsAssignableFrom(type));
    }

    private static bool IsValueTuple(Type type)
    {
        return type.FullName?.StartsWith("System.ValueTuple", StringComparison.Ordinal) == true;
    }
}
