using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed partial class BinarySchemaParentBindings
{
    internal sealed record Requirement(string Path, Node Source)
    {
        internal string[] Parts => Path.Split('.');
        internal int Depth => Parts.TakeWhile(part => part.Equals("parent", StringComparison.OrdinalIgnoreCase)).Count();
        internal string MemberPath => string.Join('.', Parts.Skip(Depth));
    }

    internal sealed class Context(string name, IReadOnlyList<SchemaFieldNode> fields, string[] typeParameters)
    {
        internal string Name { get; } = name;
        internal IReadOnlyList<SchemaFieldNode> Fields { get; } = fields;
        internal string[] TypeParameters { get; } = typeParameters;
        internal SortedDictionary<string, Requirement> Requirements { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, Type> Types { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal HashSet<string> NullablePaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal HashSet<Node> ExpressionReferences { get; } = [];
        internal Dictionary<string, IReadOnlyDictionary<string, string>> GenericInstances { get; } = new(StringComparer.Ordinal);
        internal IReadOnlyList<FactorySlot> Factories { get; set; } = [];
        internal string Member(string path) => $"P{Requirements.Keys.ToList().FindIndex(key => key.Equals(path, StringComparison.OrdinalIgnoreCase))}";
    }

    internal sealed record FactorySlot(string OwnerType, string ResultType)
    {
        internal string Key => $"{OwnerType}->{ResultType}";
    }

    private readonly SchemaRegistry _registry;
    private readonly Dictionary<string, Context> _contexts = new(StringComparer.OrdinalIgnoreCase);

    internal BinarySchemaParentBindings(SchemaRegistry registry)
    {
        _registry = registry;
        foreach (var schema in registry.Schemas.Select(registration => registration.Node).OfType<BinarySchemaNode>())
            Add(schema.Name, GetFields(schema), schema.TypeParameters);
        CollectGenericInstances();
        foreach (var context in _contexts.Values.Where(context => context.TypeParameters.Length > 0))
            context.Factories = DiscoverFactories(context, context.TypeParameters.ToDictionary(parameter => parameter, parameter => parameter), []).Distinct().ToArray();
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var context in _contexts.Values.ToArray())
                foreach (var child in Children(context.Fields, new Dictionary<string, string>()).Concat(
                             context.GenericInstances.Values.SelectMany(types => Children(context.Fields, types))))
                    foreach (var requirement in child.Requirements.Values.Where(requirement => requirement.Depth > 1))
                    {
                        var path = string.Join('.', requirement.Parts.Skip(1));
                        if (context.Requirements.TryAdd(path, new Requirement(path, requirement.Source)))
                            changed = true;
                    }
        }
    }

    internal void BindAll()
    {
        foreach (var context in _contexts.Values.Where(context => context.Requirements.Count == 0 && context.TypeParameters.Length == 0))
            Bind(context, [], new Dictionary<string, string>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    internal Context? Find(string name) => _contexts.GetValueOrDefault(name.Split('<')[0]);
    internal IReadOnlyDictionary<string, string> InstanceTypes(string name) => Find(name)?.GenericInstances.GetValueOrDefault(name.Replace(" ", string.Empty, StringComparison.Ordinal))
        ?? new Dictionary<string, string>();

    internal Type? ResolveType(IReadOnlyList<SchemaFieldNode> fields, string path, Node expression)
    {
        var context = _contexts.Values.OrderByDescending(candidate => candidate.Fields.Count(field => fields.Contains(field)))
            .FirstOrDefault(candidate => candidate.Fields.Any(field => fields.Contains(field)) && candidate.ExpressionReferences.Contains(expression) && candidate.Types.ContainsKey(path));
        return context?.Types.GetValueOrDefault(path);
    }

    internal bool? ParentMayBeNull(IReadOnlyList<SchemaFieldNode> fields, string path, Node expression)
    {
        var context = _contexts.Values.FirstOrDefault(candidate => candidate.Fields.Any(fields.Contains) && candidate.ExpressionReferences.Contains(expression));
        return context is null ? null : context.NullablePaths.Contains(path);
    }

    internal void RequireRoot(string name, TextSpan span)
    {
        if (Find(name) is { Requirements.Count: > 0 })
            throw new QuerySyntaxException($"Binary schema '{name}' requires an enclosing record and cannot be interpreted directly.", span);
    }

    private void Add(string name, IReadOnlyList<SchemaFieldNode> fields, string[] typeParameters)
    {
        if (_contexts.ContainsKey(name))
            return;
        var context = new Context(name, fields, typeParameters);
        _contexts.Add(name, context);
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            foreach (var expression in Expressions(field))
                foreach (var reference in ParentPaths(expression))
                    if (!available.Contains("parent"))
                    {
                        context.Requirements.TryAdd(reference.Path, reference);
                        context.ExpressionReferences.Add(reference.Source);
                    }
            RegisterInline(field is FieldDefinitionNode parsed ? parsed.TypeAnnotation : null, field.Name);
            available.Add(field.Name);
        }
    }

    private void RegisterInline(TypeAnnotationNode? type, string fieldName)
    {
        switch (type)
        {
            case InlineSchemaTypeNode inline:
                Add($"Inline_{fieldName}", inline.Fields, []);
                break;
            case ArrayTypeNode array:
                RegisterInline(array.ElementType, fieldName);
                break;
            case RepeatUntilTypeNode repeat:
                RegisterInline(repeat.ElementType, fieldName);
                break;
            case SubstreamTypeNode { Target: not null } substream:
                RegisterInline(substream.Target, fieldName);
                break;
        }
    }

    private IReadOnlyList<SchemaFieldNode> GetFields(BinarySchemaNode schema)
    {
        var fields = string.IsNullOrEmpty(schema.Extends) ? new List<SchemaFieldNode>() :
            GetFields((BinarySchemaNode)_registry.GetSchema(schema.Extends).Node).ToList();
        foreach (var field in schema.Fields)
        {
            var index = field.Name == "_" ? -1 : fields.FindIndex(candidate => candidate.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
            if (index < 0) fields.Add(field);
            else fields[index] = field;
        }
        return fields;
    }

    private IEnumerable<Context> Children(IReadOnlyList<SchemaFieldNode> fields, IReadOnlyDictionary<string, string> types) =>
        fields.OfType<FieldDefinitionNode>().SelectMany(field => Children(field.TypeAnnotation, field.Name, types));

    private IEnumerable<Context> Children(TypeAnnotationNode type, string fieldName, IReadOnlyDictionary<string, string> types)
    {
        switch (type)
        {
            case SchemaReferenceTypeNode reference:
                if (Find(types.GetValueOrDefault(reference.SchemaName) ?? reference.SchemaName) is { } named)
                    yield return named;
                break;
            case InlineSchemaTypeNode:
                yield return _contexts[$"Inline_{fieldName}"];
                break;
            case ArrayTypeNode array:
                foreach (var child in Children(array.ElementType, fieldName, types)) yield return child;
                break;
            case RepeatUntilTypeNode repeat:
                foreach (var child in Children(repeat.ElementType, fieldName, types)) yield return child;
                break;
            case SubstreamTypeNode { Target: not null } substream:
                foreach (var child in Children(substream.Target, fieldName, types)) yield return child;
                break;
        }
    }

    private void Bind(Context context, IReadOnlyList<IReadOnlyList<SchemaFieldNode>> ancestors,
        IReadOnlyDictionary<string, string> types, HashSet<string> active)
    {
        var instanceName = InstanceName(context, types);
        if (!active.Add(instanceName))
            return;
        BindRequirements(context, ancestors);
        BindChildren(context, ancestors, types, active);
        active.Remove(instanceName);
    }

    private void BindRequirements(Context context, IReadOnlyList<IReadOnlyList<SchemaFieldNode>> ancestors)
    {
        foreach (var requirement in context.Requirements.Values)
        {
            if (requirement.Depth > ancestors.Count)
                throw Invalid(context, requirement, "the required enclosing record does not exist");
            var fields = ancestors[requirement.Depth - 1];
            if (!fields.Any(field => field.Name.Equals(requirement.Parts[requirement.Depth], StringComparison.OrdinalIgnoreCase)))
                throw Invalid(context, requirement, "the enclosing field is missing or has not completed parsing");
            var type = BinarySchemaExpressionTypes.Resolve(Path(requirement.MemberPath), fields, _registry);
            if (BinarySchemaExpressionTypes.MayBeNull(Path(requirement.MemberPath), fields, _registry)) context.NullablePaths.Add(requirement.Path);
            if (type == typeof(object))
                throw Invalid(context, requirement, "the enclosing field has no compatible concrete value type");
            if (context.Types.TryGetValue(requirement.Path, out var previous))
                type = MergeTypes(previous, type, context, requirement);
            context.Types[requirement.Path] = type;
        }
    }

    private void BindChildren(Context context, IReadOnlyList<IReadOnlyList<SchemaFieldNode>> ancestors,
        IReadOnlyDictionary<string, string> types, HashSet<string> active)
    {
        var available = new List<SchemaFieldNode>();
        foreach (var field in context.Fields)
        {
            if (field is FieldDefinitionNode parsed)
                foreach (var child in Children(parsed.TypeAnnotation, field.Name, types))
                {
                    var childTypes = TypeBindings(parsed.TypeAnnotation, child, types);
                    Bind(child, new[] { (IReadOnlyList<SchemaFieldNode>)available.ToArray() }.Concat(ancestors).ToArray(), childTypes, active);
                    foreach (var requirement in child.Requirements.Values.Where(requirement => requirement.Depth > 1))
                    {
                        var path = string.Join('.', requirement.Parts.Skip(1));
                        context.Requirements.TryAdd(path, new Requirement(path, requirement.Source));
                        if (child.NullablePaths.Contains(requirement.Path)) context.NullablePaths.Add(path);
                        context.Types[path] = context.Types.TryGetValue(path, out var previous)
                            ? MergeTypes(previous, child.Types[requirement.Path], context, requirement)
                            : child.Types[requirement.Path];
                    }
                }
            available.Add(field);
        }
    }

    private static Type MergeTypes(Type left, Type right, Context context, Requirement requirement)
    {
        var leftValue = BinarySchemaExpressionTypes.Unwrap(left);
        var rightValue = BinarySchemaExpressionTypes.Unwrap(right);
        if (leftValue == rightValue)
            return BinarySchemaExpressionTypes.Lift(leftValue, BinarySchemaExpressionTypes.IsNullable(left) || BinarySchemaExpressionTypes.IsNullable(right));
        if (BinaryOperatorTypeRules.IsNumericType(leftValue) && BinaryOperatorTypeRules.IsNumericType(rightValue))
            return BinarySchemaExpressionTypes.Lift(BinaryOperatorTypeRules.GetWiderNumericType(leftValue, rightValue),
                BinarySchemaExpressionTypes.IsNullable(left) || BinarySchemaExpressionTypes.IsNullable(right));
        throw Invalid(context, requirement, "embedding sites supply incompatible field types");
    }

    private static QuerySyntaxException Invalid(Context context, Requirement requirement, string reason) =>
        new($"Binary schema '{context.Name}' reference '{requirement.Path}' is invalid: {reason}.", requirement.Source.SpanOrEmpty(), DiagnosticCode.MQ2030_UnsupportedSyntax);

    internal static Node Path(string path)
    {
        var parts = path.Split('.');
        Node node = new IdentifierNode(parts[0]);
        foreach (var part in parts.Skip(1)) node = new DotNode(node, new IdentifierNode(part), part);
        return node;
    }

    private static IEnumerable<Requirement> ParentPaths(Node expression)
    {
        if (expression is DotNode dot)
        {
            var path = string.Join('.', BinarySchemaExpressionTypes.PathParts(dot));
            if (path.StartsWith("parent.", StringComparison.OrdinalIgnoreCase))
                yield return new Requirement(path, dot);
            yield break;
        }
        foreach (var child in ChildExpressions(expression))
            foreach (var requirement in ParentPaths(child)) yield return requirement;
    }

    private static IEnumerable<Node> ChildExpressions(Node node) => node switch
    {
        BinaryNode binary => [binary.Left, binary.Right],
        UnaryNode unary => [unary.Expression],
        CastNode cast => [cast.Expression],
        CaseNode choice => choice.WhenThenPairs.SelectMany(pair => new[] { pair.When, pair.Then }).Append(choice.Else),
        AccessMethodNode method => method.Arguments.Args,
        _ => []
    };

    private static IEnumerable<Node> Expressions(SchemaFieldNode field)
    {
        if (field is ComputedFieldNode computed) yield return computed.Expression;
        if (field is not FieldDefinitionNode parsed) yield break;
        if (parsed.WhenCondition is not null) yield return parsed.WhenCondition;
        if (parsed.AtOffset is not null) yield return parsed.AtOffset;
        if (parsed.Constraint is not null) yield return parsed.Constraint.Expression;
        foreach (var expression in TypeExpressions(parsed.TypeAnnotation)) yield return expression;
    }

    private static IEnumerable<Node> TypeExpressions(TypeAnnotationNode type)
    {
        switch (type)
        {
            case ByteArrayTypeNode bytes: yield return bytes.SizeExpression; break;
            case StringTypeNode text: yield return text.SizeExpression; break;
            case ArrayTypeNode array:
                yield return array.SizeExpression;
                foreach (var expression in TypeExpressions(array.ElementType)) yield return expression;
                break;
            case RepeatUntilTypeNode repeat:
                if (repeat.Condition is not null) yield return repeat.Condition;
                foreach (var expression in TypeExpressions(repeat.ElementType)) yield return expression;
                break;
            case SubstreamTypeNode substream: yield return substream.SizeExpression; break;
        }
    }
}
