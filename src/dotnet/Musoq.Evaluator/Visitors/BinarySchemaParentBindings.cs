using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Execution;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed partial class BinarySchemaParentBindings
{
    internal sealed record Requirement(string Path, Node Source, bool IsImplicit = false)
    {
        internal string[] Parts => Path.Split('.');
        internal int Depth =>
            Parts.TakeWhile(part => part.Equals("parent", StringComparison.OrdinalIgnoreCase)).Count();
        internal string MemberPath => string.Join('.', Parts.Skip(Depth));
    }

    internal sealed class Context(string name, IReadOnlyList<SchemaFieldNode> fields, string[] typeParameters)
    {
        private readonly HashSet<string> _declaredNames = fields
            .Select(field => field.Name)
            .Append(name)
            .ToHashSet(StringComparer.Ordinal);
        internal string Name { get; } = name;
        internal string TypeName => AvailableName("ParentContext");
        internal string StorageName => AvailableName("__parentContext");

        internal string FactoryName(int index) => AvailableName($"__factory{index}");

        private string AvailableName(string preferred)
        {
            while (_declaredNames.Contains(preferred))
                preferred += "_";
            return preferred;
        }

        internal IReadOnlyList<SchemaFieldNode> Fields { get; } = fields;
        internal string[] TypeParameters { get; } = typeParameters;
        internal SortedDictionary<string, Requirement> Requirements { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, Type> Types { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, FieldDefinitionNode> RecordFields { get; } = new(StringComparer.Ordinal);
        internal HashSet<string> NullablePaths { get; } = new(StringComparer.Ordinal);
        internal Dictionary<Node, string> BoundPaths { get; } = new(ReferenceEqualityComparer.Instance);
        internal HashSet<string> ImplicitNames { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, IReadOnlyDictionary<string, string>> GenericInstances { get; } =
            new(StringComparer.Ordinal);
        internal IReadOnlyList<FactorySlot> Factories { get; set; } = [];

        internal string Member(string path) =>
            $"P{Requirements.Keys.ToList().FindIndex(key => key.Equals(path, StringComparison.Ordinal))}";
    }

    internal sealed record FactorySlot(string OwnerType, string ResultType)
    {
        internal string Key => $"{OwnerType}->{ResultType}";
    }

    private readonly SchemaRegistry _registry;
    private readonly Dictionary<string, Context> _contexts = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Node, List<Context>> _expressionOwners = new(ReferenceEqualityComparer.Instance);

    internal BinarySchemaParentBindings(SchemaRegistry registry)
    {
        _registry = registry;
        foreach (var schema in registry.Schemas.Select(registration => registration.Node).OfType<BinarySchemaNode>())
            Add(schema.Name, InterpretationSchemaFields.GetFields(schema, registry), schema.TypeParameters);
        CollectGenericInstances();
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var context in _contexts.Values.ToArray())
            foreach (
                var child in Children(context.Fields, new Dictionary<string, string>())
                    .Concat(context.GenericInstances.Values.SelectMany(types => Children(context.Fields, types)))
            )
            foreach (var requirement in child.Requirements.Values.Where(requirement => requirement.Depth > 1))
            {
                var path = string.Join('.', requirement.Parts.Skip(1));
                if (context.Requirements.TryAdd(path, new Requirement(path, requirement.Source)))
                    changed = true;
            }
        }
        foreach (var context in _contexts.Values.Where(context => context.TypeParameters.Length > 0))
            context.Factories = DiscoverFactories(
                    context,
                    context.TypeParameters.ToDictionary(parameter => parameter, parameter => parameter),
                    []
                )
                .Distinct()
                .ToArray();
    }

    internal void BindAll()
    {
        foreach (
            var context in _contexts.Values.Where(context =>
                context.Requirements.Count == 0 && context.TypeParameters.Length == 0
            )
        )
            Bind(context, [], new Dictionary<string, string>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    internal Context? Find(string name) => _contexts.GetValueOrDefault(name.Split('<')[0]);

    internal IReadOnlyDictionary<string, string> InstanceTypes(string name) =>
        Find(name)?.GenericInstances.GetValueOrDefault(name.Replace(" ", string.Empty, StringComparison.Ordinal))
        ?? new Dictionary<string, string>();

    private Context? FindReference(IReadOnlyList<SchemaFieldNode> fields, Node expression)
    {
        if (!_expressionOwners.TryGetValue(expression, out var owners))
            return null;
        return owners.Count == 1 ? owners[0] : owners.MaxBy(candidate => candidate.Fields.Count(fields.Contains));
    }

    internal Type? ResolveType(IReadOnlyList<SchemaFieldNode> fields, Node expression) =>
        FindReference(fields, expression) is { } owner
            ? owner.Types.GetValueOrDefault(owner.BoundPaths[expression])
            : null;

    internal FieldDefinitionNode? ResolveRecord(IReadOnlyList<SchemaFieldNode> fields, Node expression) =>
        FindReference(fields, expression) is { } owner
            ? owner.RecordFields.GetValueOrDefault(owner.BoundPaths[expression])
            : null;

    internal bool? ParentMayBeNull(IReadOnlyList<SchemaFieldNode> fields, Node expression) =>
        FindReference(fields, expression) is { } owner
            ? owner.NullablePaths.Contains(owner.BoundPaths[expression])
            : null;

    internal void RequireRoot(string name, TextSpan span)
    {
        if (Find(name) is { Requirements.Count: > 0 })
            throw new QuerySyntaxException(
                $"Binary schema '{name}' requires an enclosing record and cannot be interpreted directly.",
                span
            );
    }

    private void Add(
        string name,
        IReadOnlyList<SchemaFieldNode> fields,
        string[] typeParameters,
        IReadOnlySet<string>? enclosingNames = null
    )
    {
        if (_contexts.ContainsKey(name))
            return;
        var context = new Context(name, fields, typeParameters);
        var implicitNames = enclosingNames
            ?.Except(fields.Select(field => field.Name), StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        _contexts.Add(name, context);
        var available = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            foreach (var expression in Expressions(field))
            foreach (var reference in ParentPaths(expression, implicitNames))
                if (reference.IsImplicit || !available.Contains(reference.Parts[0]))
                {
                    if (reference.IsImplicit && reference.Source is IdentifierNode identifier)
                        context.ImplicitNames.Add(identifier.Name);
                    context.Requirements.TryAdd(reference.Path, reference);
                    if (context.BoundPaths.TryAdd(reference.Source, reference.Path))
                    {
                        if (!_expressionOwners.TryGetValue(reference.Source, out var owners))
                            _expressionOwners[reference.Source] = owners = [];
                        owners.Add(context);
                    }
                }
            if (field is FieldDefinitionNode parsed)
                foreach (var inline in RecordTypes(parsed.TypeAnnotation).OfType<InlineSchemaTypeNode>())
                    Add(_registry.BinaryCompositions.Name(inline), inline.Fields, [], available);
            available.Add(field.Name);
        }
    }

    private static IEnumerable<TypeAnnotationNode> RecordTypes(TypeAnnotationNode type)
    {
        return type switch
        {
            ArrayTypeNode array => RecordTypes(array.ElementType),
            RepeatUntilTypeNode repeat => RecordTypes(repeat.ElementType),
            SubstreamTypeNode { Target: { } target } => RecordTypes(target),
            BinarySwitchTypeNode choice => choice.Cases.SelectMany(arm => RecordTypes(arm.BranchType)),
            SchemaReferenceTypeNode or InlineSchemaTypeNode => [type],
            _ => [],
        };
    }

    private IEnumerable<Context> Children(
        IReadOnlyList<SchemaFieldNode> fields,
        IReadOnlyDictionary<string, string> types
    ) =>
        fields
            .OfType<FieldDefinitionNode>()
            .SelectMany(field => Children(field.TypeAnnotation, types))
            .Select(site => site.Child);

    private IEnumerable<(TypeAnnotationNode Type, Context Child)> Children(
        TypeAnnotationNode type,
        IReadOnlyDictionary<string, string> types
    )
    {
        foreach (var record in RecordTypes(type))
        {
            var child = record switch
            {
                SchemaReferenceTypeNode reference => Find(
                    types.GetValueOrDefault(reference.SchemaName) ?? reference.SchemaName
                ),
                InlineSchemaTypeNode => _contexts[_registry.BinaryCompositions.Name(record)],
                _ => null,
            };
            if (child is not null)
                yield return (record, child);
        }
    }

    private void Bind(
        Context context,
        IReadOnlyList<IReadOnlyList<SchemaFieldNode>> ancestors,
        IReadOnlyDictionary<string, string> types,
        HashSet<string> active
    )
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
            if (!fields.Any(field => field.Name.Equals(requirement.Parts[requirement.Depth], StringComparison.Ordinal)))
                throw Invalid(context, requirement, "the enclosing field is missing or has not completed parsing");
            var member = Path(requirement.MemberPath);
            var type = BinarySchemaExpressionTypes.Resolve(member, fields, _registry);
            var record = BinarySchemaRecordAliases.Resolve(member, fields, _registry);
            if (record is not null)
            {
                if (
                    context.RecordFields.TryGetValue(requirement.Path, out var previousRecord)
                    && BinarySchemaRecordAliases.Identity(previousRecord, _registry)
                        != BinarySchemaRecordAliases.Identity(record, _registry)
                )
                    throw Invalid(context, requirement, "embedding sites supply incompatible record types");
                context.RecordFields[requirement.Path] = record;
            }
            if (BinarySchemaExpressionTypes.MayBeNull(member, fields, _registry))
                context.NullablePaths.Add(requirement.Path);
            if (type == typeof(object) && record is null)
                throw Invalid(context, requirement, "the enclosing field has no compatible concrete value type");
            if (context.Types.TryGetValue(requirement.Path, out var previous))
                type = MergeTypes(previous, type, context, requirement);
            context.Types[requirement.Path] = type;
        }
    }

    private void BindChildren(
        Context context,
        IReadOnlyList<IReadOnlyList<SchemaFieldNode>> ancestors,
        IReadOnlyDictionary<string, string> types,
        HashSet<string> active
    )
    {
        var available = new List<SchemaFieldNode>();
        var boundFields =
            context.TypeParameters.Length == 0
                ? context.Fields
                : FieldsFor(BinarySchemaGenericResolver.ParseReference(InstanceName(context, types))) ?? context.Fields;
        foreach (var field in boundFields)
        {
            if (field is FieldDefinitionNode parsed)
                foreach (var (record, child) in Children(parsed.TypeAnnotation, types))
                {
                    var childTypes = TypeBindings(record, child, types);
                    Bind(
                        child,
                        new[] { (IReadOnlyList<SchemaFieldNode>)available.ToArray() }.Concat(ancestors).ToArray(),
                        childTypes,
                        active
                    );
                    foreach (var requirement in child.Requirements.Values.Where(requirement => requirement.Depth > 1))
                    {
                        var path = string.Join('.', requirement.Parts.Skip(1));
                        context.Requirements.TryAdd(path, new Requirement(path, requirement.Source));
                        if (child.NullablePaths.Contains(requirement.Path))
                            context.NullablePaths.Add(path);
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
            return BinarySchemaExpressionTypes.Lift(
                leftValue,
                BinarySchemaExpressionTypes.IsNullable(left) || BinarySchemaExpressionTypes.IsNullable(right)
            );
        if (BinaryOperatorTypeRules.IsNumericType(leftValue) && BinaryOperatorTypeRules.IsNumericType(rightValue))
            return BinarySchemaExpressionTypes.Lift(
                BinaryOperatorTypeRules.GetWiderNumericType(leftValue, rightValue),
                BinarySchemaExpressionTypes.IsNullable(left) || BinarySchemaExpressionTypes.IsNullable(right)
            );
        throw Invalid(context, requirement, "embedding sites supply incompatible field types");
    }

    private static QuerySyntaxException Invalid(Context context, Requirement requirement, string reason) =>
        new(
            $"Binary schema '{context.Name}' reference '{requirement.Path}' is invalid: {reason}.",
            requirement.Source.SpanOrEmpty(),
            DiagnosticCode.MQ2030_UnsupportedSyntax
        );

    internal static Node Path(string path)
    {
        var parts = path.Split('.');
        Node node = new IdentifierNode(parts[0]);
        foreach (var part in parts.Skip(1))
            node = new DotNode(node, new IdentifierNode(part), part);
        return node;
    }

    private static IEnumerable<Requirement> ParentPaths(Node expression, IReadOnlySet<string>? implicitNames)
    {
        if (expression is DotNode dot && BinarySchemaExpressionTypes.IsFieldPath(dot))
        {
            var parts = BinarySchemaExpressionTypes.PathParts(dot).ToArray();
            var path = string.Join('.', parts);
            if (implicitNames?.Contains(parts[0]) == true)
            {
                yield return new Requirement("parent." + path, dot, true);
                yield break;
            }
            if (parts[0].Equals("parent", StringComparison.OrdinalIgnoreCase))
            {
                yield return new Requirement(path, dot);
                yield break;
            }
        }
        if (expression is IdentifierNode { Name: var name } && implicitNames?.Contains(name) == true)
        {
            yield return new Requirement("parent." + name, expression, true);
            yield break;
        }
        foreach (var child in ChildExpressions(expression))
        foreach (var requirement in ParentPaths(child, implicitNames))
            yield return requirement;
    }

    private static IEnumerable<Node> ChildExpressions(Node node) =>
        node switch
        {
            DotNode dot => [dot.Root],
            ArrayIndexNode indexed => [indexed.Array, indexed.Index],
            BinaryNode binary => [binary.Left, binary.Right],
            UnaryNode unary => [unary.Expression],
            IsNullNode predicate => [predicate.Expression],
            CastNode cast => [cast.Expression],
            CaseNode choice => choice
                .WhenThenPairs.SelectMany(pair => new[] { pair.When, pair.Then })
                .Append(choice.Else),
            AccessMethodNode method => method.Arguments.Args,
            _ => [],
        };

    private static IEnumerable<Node> Expressions(SchemaFieldNode field)
    {
        if (field is ComputedFieldNode computed)
            yield return computed.Expression;
        if (field is not FieldDefinitionNode parsed)
            yield break;
        if (parsed.WhenCondition is not null)
            yield return parsed.WhenCondition;
        if (parsed.AtOffset is not null)
            yield return parsed.AtOffset;
        if (parsed.Constraint is not null)
            yield return parsed.Constraint.Expression;
        foreach (var expression in TypeExpressions(parsed.TypeAnnotation))
            yield return expression;
    }

    private static IEnumerable<Node> TypeExpressions(TypeAnnotationNode type)
    {
        switch (type)
        {
            case ByteArrayTypeNode bytes:
                yield return bytes.SizeExpression;
                break;
            case StringTypeNode text:
                yield return text.SizeExpression;
                break;
            case ArrayTypeNode array:
                yield return array.SizeExpression;
                foreach (var expression in TypeExpressions(array.ElementType))
                    yield return expression;
                break;
            case RepeatUntilTypeNode repeat:
                if (repeat.Condition is not null)
                    yield return repeat.Condition;
                foreach (var expression in TypeExpressions(repeat.ElementType))
                    yield return expression;
                break;
            case SubstreamTypeNode substream:
                yield return substream.SizeExpression;
                if (substream.Target is not null)
                    foreach (var expression in TypeExpressions(substream.Target))
                        yield return expression;
                break;
            case BinarySwitchTypeNode choice:
                yield return choice.SelectorExpression;
                foreach (var expression in choice.Cases.SelectMany(arm => TypeExpressions(arm.BranchType)))
                    yield return expression;
                break;
        }
    }
}
