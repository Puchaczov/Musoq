using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private BinarySchemaParentBindings.Context? _parentContext;

    private int _factoryCounter;

    private string GenerateParentContextMembers(string className, string constructorName)
    {
        _parentContext = _registry.ParentBindings.Find(className);
        if (_parentContext is not { } context || (context.Requirements.Count == 0 && context.Factories.Count == 0))
            return string.Empty;
        if (context.Types.Count != context.Requirements.Count)
            throw new QuerySyntaxException(
                $"Binary schema '{className}' requires an enclosing record.",
                TextSpan.Empty
            );
        var members = context.Requirements.Keys.Select(path =>
        {
            var type = context.Types[path];
            var typeName =
                (
                    context.RecordFields.TryGetValue(path, out var record)
                        ? GetClrTypeNameForFieldDefinition(record)
                        : MapClrTypeName(BinarySchemaExpressionTypes.Unwrap(type))
                )
                + (
                    BinarySchemaExpressionTypes.IsNullable(type)
                    || (!type.IsValueType && context.NullablePaths.Contains(path))
                        ? "?"
                        : string.Empty
                );
            return $"{typeName} {context.Member(path)}";
        });
        var factories = ParentFactoryDeclarations(context);
        var declarations = string.Join("\n", factories.Select(factory => $"private readonly {factory};"));
        var parameters = string.Join(", ", factories.Select(factory => factory + " = null"));
        var assignments = string.Join(
            " ",
            context.Factories.Select((_, index) => $"this.{context.FactoryName(index)} = {context.FactoryName(index)};")
        );
        return $"internal readonly record struct {context.TypeName}({string.Join(", ", members)});\n"
            + $"private readonly {context.TypeName} {context.StorageName};\n"
            + declarations
            + "\n"
            + $"public {constructorName}() {{ }}\n"
            + $"internal {constructorName}({context.TypeName} parentContext{(parameters.Length == 0 ? string.Empty : ", " + parameters)}) {{ {context.StorageName} = parentContext; {assignments} }}\n";
    }

    private string ForwardParentContextConstructors(InterpreterDeclaration declaration, string name)
    {
        if (
            !declaration.UsesBinaryInput
            || _registry.ParentBindings.Find(declaration.Name) is not { } context
            || (context.Requirements.Count == 0 && context.Factories.Count == 0)
        )
            return string.Empty;
        var parameters = string.Concat(
            ParentFactoryDeclarations(context).Select(factory => ", " + factory + " = null")
        );
        var arguments = string.Concat(context.Factories.Select((_, index) => $", {context.FactoryName(index)}"));
        return $"public {name}() {{ }}\ninternal {name}({context.TypeName} parentContext{parameters}) : base(parentContext{arguments}) {{ }}\n";
    }

    private string[] ParentFactoryDeclarations(BinarySchemaParentBindings.Context context) =>
        context
            .Factories.Select(
                (slot, index) =>
                    $"System.Func<{InterpretationCSharpNames.Reference(slot.OwnerType)}, {InterpretationCSharpNames.Reference(slot.OwnerType)}.{_registry.ParentBindings.Find(slot.OwnerType)!.TypeName}, {InterpretationCSharpNames.Reference(slot.ResultType)}>? {context.FactoryName(index)}"
            )
            .ToArray();

    private string CreateNestedInterpreter(string className)
    {
        var reference = InterpretationCSharpNames.Reference(className);
        if (_parentContext is { TypeParameters.Length: > 0 } owner)
        {
            var ownType = $"{owner.Name}<{string.Join(',', owner.TypeParameters)}>";
            var index = owner
                .Factories.ToList()
                .FindIndex(slot => slot.OwnerType == ownType && slot.ResultType == className);
            if (index >= 0)
                return $"({owner.FactoryName(index)} is null ? new {reference}() : {owner.FactoryName(index)}.Invoke(__parsedValue, {owner.StorageName}))";
            if (owner.TypeParameters.Contains(className))
                return $"new {reference}()";
        }
        if (
            _registry.ParentBindings.Find(className) is not { } target
            || (target.Requirements.Count == 0 && target.Factories.Count == 0)
        )
            return $"new {reference}()";
        var arguments = target.Requirements.Values.Select(requirement =>
        {
            if (requirement.Depth == 1)
                return GenerateConditionExpression(BinarySchemaParentBindings.Path(requirement.MemberPath));
            var forwarded = string.Join('.', requirement.Parts.Skip(1));
            if (_parentContext is null || !_parentContext.Requirements.ContainsKey(forwarded))
                throw new QuerySyntaxException(
                    $"Cannot supply '{requirement.Path}' to binary schema '{className}'.",
                    requirement.Source.SpanOrEmpty()
                );
            return $"{_parentContext.StorageName}.{_parentContext.Member(forwarded)}";
        });
        var factories = FactoryArguments(target, className);
        return $"new {reference}(new {reference}.{target.TypeName}({string.Join(", ", arguments)}){factories})";
    }

    private string FactoryArguments(BinarySchemaParentBindings.Context target, string className)
    {
        var types = _registry.ParentBindings.InstanceTypes(className);
        var arguments = target
            .Factories.Select(slot =>
            {
                var closed = new BinarySchemaParentBindings.FactorySlot(
                    BinarySchemaParentBindings.Substitute(slot.OwnerType, types),
                    BinarySchemaParentBindings.Substitute(slot.ResultType, types)
                );
                var forwarded =
                    _parentContext?.Factories.ToList().FindIndex(candidate => candidate.Key == closed.Key) ?? -1;
                if (forwarded >= 0)
                    return _parentContext!.FactoryName(forwarded);
                return CreateGenericFactory(closed);
            })
            .ToArray();
        return arguments.Length == 0 ? string.Empty : ", " + string.Join(", ", arguments);
    }

    private string CreateGenericFactory(BinarySchemaParentBindings.FactorySlot slot)
    {
        if (
            _registry.ParentBindings.Find(slot.ResultType) is not { } valueContext
            || (valueContext.Requirements.Count == 0 && valueContext.Factories.Count == 0)
        )
            return "null";
        var index = _factoryCounter++;
        var record = $"__record{index}";
        var parent = $"__enclosing{index}";
        var owner = _registry.ParentBindings.Find(slot.OwnerType)!;
        var arguments = valueContext.Requirements.Values.Select(requirement =>
        {
            if (requirement.Depth > 1)
                return $"{parent}.{owner.Member(string.Join('.', requirement.Parts.Skip(1)))}";
            var path = InterpretationCSharpNames.Reference(requirement.MemberPath);
            if (
                BinarySchemaExpressionTypes.MayBeNull(
                    BinarySchemaParentBindings.Path(requirement.MemberPath),
                    owner.Fields,
                    _registry
                )
            )
                path = path.Replace(".", "?.", StringComparison.Ordinal);
            return $"{record}.{path}";
        });
        var factories = FactoryArguments(valueContext, slot.ResultType);
        return $"static ({InterpretationCSharpNames.Reference(slot.OwnerType)} {record}, {InterpretationCSharpNames.Reference(slot.OwnerType)}.{owner.TypeName} {parent}) => new {InterpretationCSharpNames.Reference(slot.ResultType)}(new {InterpretationCSharpNames.Reference(slot.ResultType)}.{valueContext.TypeName}({string.Join(", ", arguments)}){factories})";
    }
}
