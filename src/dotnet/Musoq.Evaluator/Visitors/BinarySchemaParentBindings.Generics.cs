using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Evaluator.IR.Execution;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed partial class BinarySchemaParentBindings
{
    private readonly Dictionary<string, IReadOnlyList<SchemaFieldNode>> _instanceFields = new(
        StringComparer.OrdinalIgnoreCase
    );

    internal IReadOnlyList<SchemaFieldNode>? FieldsFor(SchemaReferenceTypeNode reference)
    {
        var context = Find(reference.SchemaName);
        if (context is null || !reference.IsGenericInstantiation)
            return context?.Fields;
        if (_instanceFields.TryGetValue(reference.FullTypeName, out var cached))
            return cached;
        if (
            !_registry.TryGetSchema(reference.SchemaName, out var registration)
            || registration?.Node is not BinarySchemaNode schema
        )
            return null;
        var bindings = BinarySchemaGenericResolver.CreateBindings(
            schema,
            reference,
            BinarySchemaGenericResolver.CreateEmptyBindings()
        );
        return _instanceFields[reference.FullTypeName] = BinarySchemaGenericResolver.BindFields(
            context.Fields,
            bindings
        );
    }

    internal static string Substitute(string typeName, IReadOnlyDictionary<string, string> bindings) =>
        Regex.Replace(typeName, @"\b[A-Za-z_]\w*\b", match => bindings.GetValueOrDefault(match.Value) ?? match.Value);

    private static string InstanceName(Context context, IReadOnlyDictionary<string, string> types) =>
        context.TypeParameters.Length == 0
            ? context.Name
            : $"{context.Name}<{string.Join(',', context.TypeParameters.Select(parameter => types.GetValueOrDefault(parameter) ?? parameter))}>".Replace(
                " ",
                string.Empty,
                StringComparison.Ordinal
            );

    private void CollectGenericInstances()
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var context in _contexts.Values.ToArray())
            foreach (var types in context.GenericInstances.Values.ToArray().Prepend(new Dictionary<string, string>()))
            foreach (var parsed in context.Fields.OfType<FieldDefinitionNode>())
            foreach (var (record, child) in Children(parsed.TypeAnnotation, types))
            {
                if (child.TypeParameters.Length == 0)
                    continue;
                var bindings = TypeBindings(record, child, types);
                if (child.GenericInstances.TryAdd(InstanceName(child, bindings), bindings))
                    changed = true;
            }
        }
    }

    private IEnumerable<FactorySlot> DiscoverFactories(
        Context context,
        IReadOnlyDictionary<string, string> types,
        HashSet<string> active
    )
    {
        if (!active.Add(context.Name))
            yield break;
        foreach (var parsed in context.Fields.OfType<FieldDefinitionNode>())
        {
            foreach (
                var reference in RecordTypes(parsed.TypeAnnotation)
                    .OfType<SchemaReferenceTypeNode>()
                    .Where(reference => context.TypeParameters.Contains(reference.SchemaName))
            )
                yield return new FactorySlot(
                    InstanceName(context, types),
                    types.GetValueOrDefault(reference.SchemaName) ?? reference.SchemaName
                );
            foreach (var (record, child) in Children(parsed.TypeAnnotation, types))
            {
                var childTypes = TypeBindings(record, child, types);
                if (child.Requirements.Values.Any(requirement => ReadsGenericParent(context, requirement)))
                    yield return new FactorySlot(InstanceName(context, types), InstanceName(child, childTypes));
                if (child.TypeParameters.Length > 0)
                    foreach (var slot in DiscoverFactories(child, childTypes, active))
                        yield return slot;
            }
        }
        active.Remove(context.Name);
    }

    private static bool ReadsGenericParent(Context owner, Requirement requirement)
    {
        if (requirement.Depth != 1)
            return false;
        var name = requirement.Parts[1];
        var field = owner.Fields.OfType<FieldDefinitionNode>().FirstOrDefault(candidate => candidate.Name == name);
        return field is not null
            && RecordTypes(field.TypeAnnotation)
                .OfType<SchemaReferenceTypeNode>()
                .Any(reference =>
                    owner.TypeParameters.Contains(reference.SchemaName)
                    || reference.TypeArguments.Any(owner.TypeParameters.Contains)
                );
    }

    private static IReadOnlyDictionary<string, string> TypeBindings(
        TypeAnnotationNode type,
        Context child,
        IReadOnlyDictionary<string, string> current
    )
    {
        if (type is not SchemaReferenceTypeNode reference)
            return current;
        var actualName = Substitute(reference.FullTypeName, current);
        var arguments = GenericArguments(actualName);
        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Math.Min(child.TypeParameters.Length, arguments.Count); index++)
            bindings[child.TypeParameters[index]] = arguments[index];
        return bindings;
    }

    private static IReadOnlyList<string> GenericArguments(string name)
    {
        var start = name.IndexOf('<');
        if (start < 0)
            return [];
        var arguments = new List<string>();
        var depth = 0;
        var segmentStart = start + 1;
        for (var index = segmentStart; index < name.Length; index++)
        {
            if (name[index] == '<')
                depth++;
            if (name[index] == '>')
                depth--;
            if ((name[index] == ',' && depth == 0) || depth < 0)
            {
                arguments.Add(name[segmentStart..index].Trim());
                segmentStart = index + 1;
            }
        }
        return arguments;
    }
}
