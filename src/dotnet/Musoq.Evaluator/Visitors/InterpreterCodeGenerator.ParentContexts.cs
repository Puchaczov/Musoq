using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private BinarySchemaParentBindings.Context? _parentContext;

    private int _factoryCounter;

    private string GenerateParentContextMembers(string className)
    {
        _parentContext = _registry.ParentBindings.Find(className);
        if (_parentContext is not { } context || (context.Requirements.Count == 0 && context.Factories.Count == 0))
            return string.Empty;
        if (context.Types.Count != context.Requirements.Count)
            throw new QuerySyntaxException($"Binary schema '{className}' requires an enclosing record.", TextSpan.Empty);
        var members = context.Requirements.Keys.Select(path =>
        {
            var type = context.Types[path];
            var typeName = MapClrTypeName(BinarySchemaExpressionTypes.Unwrap(type)) +
                           (BinarySchemaExpressionTypes.IsNullable(type) || (!type.IsValueType && context.NullablePaths.Contains(path)) ? "?" : string.Empty);
            return $"{typeName} {context.Member(path)}";
        });
        var name = className.Split('<')[0];
        var factories = context.Factories.Select((slot, index) =>
            $"System.Func<{slot.OwnerType}, {slot.OwnerType}.ParentContext, {slot.ResultType}>? __factory{index}").ToArray();
        var declarations = string.Join("\n", factories.Select(factory => $"private readonly {factory};"));
        var parameters = string.Join(", ", factories.Select(factory => factory + " = null"));
        var assignments = string.Join(" ", context.Factories.Select((_, index) => $"this.__factory{index} = __factory{index};"));
        return $"internal readonly record struct ParentContext({string.Join(", ", members)});\n" +
               "private readonly ParentContext __parentContext;\n" + declarations + "\n" +
               $"public {name}() {{ }}\n" +
               $"internal {name}(ParentContext parentContext{(parameters.Length == 0 ? string.Empty : ", " + parameters)}) {{ __parentContext = parentContext; {assignments} }}\n";
    }

    private string CreateNestedInterpreter(string className)
    {
        if (_parentContext is { } owner && owner.TypeParameters.Contains(className))
        {
            var ownType = $"{owner.Name}<{string.Join(',', owner.TypeParameters)}>";
            var index = owner.Factories.ToList().FindIndex(slot => slot.OwnerType == ownType && slot.ResultType == className);
            return index < 0 ? $"new {className}()" : $"(__factory{index} is null ? new {className}() : __factory{index}.Invoke(__parsedValue, __parentContext))";
        }
        if (_registry.ParentBindings.Find(className) is not { } target || (target.Requirements.Count == 0 && target.Factories.Count == 0))
            return $"new {className}()";
        var arguments = target.Requirements.Values.Select(requirement =>
        {
            if (requirement.Depth == 1)
                return GenerateConditionExpression(BinarySchemaParentBindings.Path(requirement.MemberPath));
            var forwarded = string.Join('.', requirement.Parts.Skip(1));
            if (_parentContext is null || !_parentContext.Requirements.ContainsKey(forwarded))
                throw new QuerySyntaxException($"Cannot supply '{requirement.Path}' to binary schema '{className}'.", requirement.Source.SpanOrEmpty());
            return $"__parentContext.{_parentContext.Member(forwarded)}";
        });
        var factories = FactoryArguments(target, className);
        return $"new {className}(new {className}.ParentContext({string.Join(", ", arguments)}){factories})";
    }

    private string FactoryArguments(BinarySchemaParentBindings.Context target, string className)
    {
        var types = _registry.ParentBindings.InstanceTypes(className);
        var arguments = target.Factories.Select(slot =>
        {
            var closed = new BinarySchemaParentBindings.FactorySlot(BinarySchemaParentBindings.Substitute(slot.OwnerType, types),
                BinarySchemaParentBindings.Substitute(slot.ResultType, types));
            var forwarded = _parentContext?.Factories.ToList().FindIndex(candidate => candidate.Key == closed.Key) ?? -1;
            if (forwarded >= 0) return $"__factory{forwarded}";
            return CreateGenericFactory(closed);
        }).ToArray();
        return arguments.Length == 0 ? string.Empty : ", " + string.Join(", ", arguments);
    }

    private string CreateGenericFactory(BinarySchemaParentBindings.FactorySlot slot)
    {
        if (_registry.ParentBindings.Find(slot.ResultType) is not { } valueContext ||
            (valueContext.Requirements.Count == 0 && valueContext.Factories.Count == 0))
            return "null";
        var index = _factoryCounter++;
        var record = $"__record{index}";
        var parent = $"__enclosing{index}";
        var owner = _registry.ParentBindings.Find(slot.OwnerType)!;
        var arguments = valueContext.Requirements.Values.Select(requirement =>
        {
            if (requirement.Depth > 1)
                return $"{parent}.{owner.Member(string.Join('.', requirement.Parts.Skip(1)))}";
            var path = requirement.MemberPath;
            if (BinarySchemaExpressionTypes.MayBeNull(BinarySchemaParentBindings.Path(path), owner.Fields, _registry))
                path = path.Replace(".", "?.", StringComparison.Ordinal);
            return $"{record}.{path}";
        });
        var factories = FactoryArguments(valueContext, slot.ResultType);
        return $"static ({slot.OwnerType} {record}, {slot.OwnerType}.ParentContext {parent}) => new {slot.ResultType}(new {slot.ResultType}.ParentContext({string.Join(", ", arguments)}){factories})";
    }
}
