using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed partial class BinarySchemaParentBindings
{
    internal static string Substitute(string typeName, IReadOnlyDictionary<string, string> bindings) =>
        Regex.Replace(typeName, @"\b[A-Za-z_]\w*\b", match => bindings.GetValueOrDefault(match.Value) ?? match.Value);

    private static string InstanceName(Context context, IReadOnlyDictionary<string, string> types) => context.TypeParameters.Length == 0
        ? context.Name
        : $"{context.Name}<{string.Join(',', context.TypeParameters.Select(parameter => types.GetValueOrDefault(parameter) ?? parameter))}>".Replace(" ", string.Empty, StringComparison.Ordinal);

    private void CollectGenericInstances()
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var context in _contexts.Values.ToArray())
                foreach (var types in context.GenericInstances.Values.ToArray().Prepend(new Dictionary<string, string>()))
                    foreach (var parsed in context.Fields.OfType<FieldDefinitionNode>())
                        foreach (var child in Children(parsed.TypeAnnotation, parsed.Name, types))
                        {
                            if (child.TypeParameters.Length == 0) continue;
                            var bindings = TypeBindings(parsed.TypeAnnotation, child, types);
                            if (child.GenericInstances.TryAdd(InstanceName(child, bindings), bindings)) changed = true;
                        }
        }
    }

    private IEnumerable<FactorySlot> DiscoverFactories(Context context, IReadOnlyDictionary<string, string> types, HashSet<string> active)
    {
        if (!active.Add(context.Name)) yield break;
        foreach (var parsed in context.Fields.OfType<FieldDefinitionNode>())
        {
            var type = ElementType(parsed.TypeAnnotation);
            if (type is SchemaReferenceTypeNode reference && context.TypeParameters.Contains(reference.SchemaName))
                yield return new FactorySlot(InstanceName(context, types), types.GetValueOrDefault(reference.SchemaName) ?? reference.SchemaName);
            foreach (var child in Children(parsed.TypeAnnotation, parsed.Name, types).Where(child => child.TypeParameters.Length > 0))
                foreach (var slot in DiscoverFactories(child, TypeBindings(parsed.TypeAnnotation, child, types), active)) yield return slot;
        }
        active.Remove(context.Name);
    }

    private static IReadOnlyDictionary<string, string> TypeBindings(TypeAnnotationNode type, Context child, IReadOnlyDictionary<string, string> current)
    {
        type = ElementType(type);
        if (type is not SchemaReferenceTypeNode reference)
            return current;
        var actualName = Substitute(reference.FullTypeName, current);
        var arguments = GenericArguments(actualName);
        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Math.Min(child.TypeParameters.Length, arguments.Count); index++)
            bindings[child.TypeParameters[index]] = arguments[index];
        return bindings;
    }

    private static TypeAnnotationNode ElementType(TypeAnnotationNode type)
    {
        while (type is ArrayTypeNode or RepeatUntilTypeNode or SubstreamTypeNode { Target: not null })
            type = type switch { ArrayTypeNode array => array.ElementType, RepeatUntilTypeNode repeat => repeat.ElementType, SubstreamTypeNode sub => sub.Target!, _ => type };
        return type;
    }

    private static IReadOnlyList<string> GenericArguments(string name)
    {
        var start = name.IndexOf('<');
        if (start < 0) return [];
        var arguments = new List<string>();
        var depth = 0;
        var segmentStart = start + 1;
        for (var index = segmentStart; index < name.Length; index++)
        {
            if (name[index] == '<') depth++;
            if (name[index] == '>') depth--;
            if ((name[index] == ',' && depth == 0) || depth < 0)
            {
                arguments.Add(name[segmentStart..index].Trim());
                segmentStart = index + 1;
            }
        }
        return arguments;
    }

}
