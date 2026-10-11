using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal static class NativeNestedFieldBinding
{
    internal static ExecutionExpression? TryCreate(ColumnRef column, string alias, FieldBinding field,
        IReadOnlyDictionary<string, RowShape> shapes)
    {
        if (shapes.TryGetValue(alias, out var generatedShape) && generatedShape is GeneratedRowShape generated &&
            field.AccessStrategy is GeneratedRowNestedAccess generatedPath)
        {
            var root = generated.Fields.FirstOrDefault(candidate =>
                candidate.AccessStrategy is GeneratedFieldAccess access && access.FieldName == generatedPath.FieldName);
            if (root is null || root.Type.ResolveClrType() is not { } rootType)
                return null;
            var generatedReceiver = new ExecutionFieldRead(alias, root.Name, root.Type, root.AccessStrategy, root.GeneratedTypeName)
            { Stability = root.Stability, SourceReadType = root.SourceReadType, EnumType = root.EnumType };
            return CreateMembers(column, generatedReceiver, rootType, generatedPath.PropertyPath.Split('.'));
        }
        if (field.AccessStrategy is not NestedClrPropertyAccess path ||
            !shapes.TryGetValue(alias, out var shape) || shape is not SourceEntityShape source)
            return null;
        var indexed = NativeIndexedFieldBinding.TryCreate(alias, field, source, path.PropertyPath);
        if (indexed is not null) return indexed;
        var names = path.PropertyPath.Split('.');
        if (names.Length < 2)
            return null;
        var receiverType = source.EntityType.ResolveClrType();
        var rootProperty = receiverType.GetProperty(names[0]);
        if (rootProperty is null)
            return null;
        var rootBinding = source.Fields.FirstOrDefault(candidate => candidate.Name == names[0]);
        ExecutionExpression receiver = new ExecutionFieldRead(alias, names[0], rootProperty.PropertyType,
            new ClrPropertyAccess(names[0]))
        {
            Stability = rootBinding?.Stability ?? field.Stability
        };
        return CreateMembers(column, receiver, rootProperty.PropertyType, names.Skip(1));
    }
    private static ExecutionExpression? CreateMembers(ColumnRef column, ExecutionExpression receiver,
        Type receiverType, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var property = receiverType.GetProperty(name);
            if (property is null)
                return null;
            receiver = new ExecutionMemberRead(receiver, name, property.PropertyType, false)
            {
                IsNullSafe = !receiverType.IsValueType || Nullable.GetUnderlyingType(receiverType) != null
            };
            receiverType = property.PropertyType;
        }
        return receiverType == column.ReturnType ? receiver : null;
    }
}
