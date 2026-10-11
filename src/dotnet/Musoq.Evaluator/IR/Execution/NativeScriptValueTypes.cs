using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class NativeScriptValueTypes
{
    private readonly IReadOnlyList<ScriptVariableDefinition> _variables;
    private readonly IReadOnlyList<ScriptParameterDefinition> _parameters;
    internal NativeInlineRecordBindings InlineRecords { get; }
    internal bool HasGeneratedStorage { get; }

    internal NativeScriptValueTypes(IReadOnlyList<ScriptVariableDefinition> variables,
        IReadOnlyList<ScriptParameterDefinition> parameters)
    {
        _variables = ExecutionIrCollections.Freeze(variables);
        _parameters = ExecutionIrCollections.Freeze(parameters);
        HasGeneratedStorage = _variables.Any(static value => value.StructuralType != null) ||
                              _parameters.Any(static value => value.Contract.IsStructured);
        InlineRecords = new NativeInlineRecordBindings(_variables.Count + _parameters.Count);
    }

    internal string? ResolveGeneratedTypeName(IrExpression expression)
    {
        if (expression is MethodCall method)
        {
            if (!GeneratedScalarGenericBinding.RequiresGeneratedSpecialization(method.ReturnType))
                return null;
            var bindings = GeneratedScalarGenericBinding.ResolveNames(method.Method,
                method.Arguments.Select(ResolveGeneratedTypeName).ToArray());
            return GeneratedScalarGenericBinding.ResolveResult(method.Method, bindings);
        }
        if (expression is ScriptVariableRef variable)
            for (var index = 0; index < _variables.Count; index++)
                if (_variables[index].Name == variable.Name && _variables[index].StructuralType is { } shape)
                    return StorageTypeName(shape, index, []);
        if (expression is ScriptParameterRef parameter)
            for (var index = 0; index < _parameters.Count; index++)
                if (_parameters[index].Name == parameter.Name && _parameters[index].Contract.StructuralType is { } shape)
                    return StorageTypeName(shape, _variables.Count + index, []);
        return null;
    }

    internal string DescribeGeneratedTypeName(string name)
    {
        if (ResolveRecordCarrier(name) is { } record)
            return ExecutionStructuralShapeFactory.Create(record).CanonicalType;
        if (name.EndsWith("[]", StringComparison.Ordinal))
            return DescribeGeneratedTypeName(name[..^2]) + "[]";
        const string listPrefix = "System.Collections.Generic.IReadOnlyList<";
        if (name.StartsWith(listPrefix, StringComparison.Ordinal) && name.EndsWith('>'))
            return DescribeGeneratedTypeName(name[listPrefix.Length..^1]) + "[]";
        throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
            "The described generated result has no authoritative structural type label.", Musoq.Parser.TextSpan.Empty);
    }

    internal bool IsRecordCarrier(string name) => ResolveRecordCarrier(name) is not null;

    internal bool IsUnmanagedRecordCarrier(string name)
        => ResolveRecordCarrier(name) is { } shape && IsUnmanagedShape(shape);

    private StructuralTypeDescriptor? ResolveRecordCarrier(string name)
    {
        for (var index = 0; index < _variables.Count; index++)
            if (_variables[index].StructuralType is { } shape && FindRecordCarrier(shape, index, [], name) is { } record)
                return record;
        for (var index = 0; index < _parameters.Count; index++)
            if (_parameters[index].Contract.StructuralType is { } shape &&
                FindRecordCarrier(shape, _variables.Count + index, [], name) is { } record)
                return record;
        foreach (var carrier in InlineRecords.RecordShapes)
            if (FindRecordCarrier(carrier.Value, carrier.Key, [], name) is { } record)
                return record;
        return null;
    }

    private static StructuralTypeDescriptor? FindRecordCarrier(StructuralTypeDescriptor shape, int index, int[] path, string name)
    {
        if (shape.Kind == StructuralTypeKind.Record)
        {
            if (name == StorageTypeName(shape, index, path))
                return shape;
            for (var field = 0; field < shape.Fields.Count; field++)
                if (FindRecordCarrier(shape.Fields[field].Type, index, [..path, field], name) is { } record)
                    return record;
        }
        return shape.Kind == StructuralTypeKind.Collection && shape.ElementType is { } element
            ? FindRecordCarrier(element, index, [..path, -1], name) : null;
    }

    private static bool IsUnmanagedShape(StructuralTypeDescriptor shape) => shape.Kind switch
    {
        StructuralTypeKind.Record => shape.Fields.All(static field => IsUnmanagedShape(field.Type)),
        StructuralTypeKind.Scalar => shape.ScalarType is { } type && IsUnmanagedType(type),
        _ => false
    };

    private static bool IsUnmanagedType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type.IsPointer)
            return true;
        return type.IsValueType && type.GetFields(System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .All(static field => IsUnmanagedType(field.FieldType));
    }

    private static string? StorageTypeName(StructuralTypeDescriptor shape, int index, int[] path)
    {
        if (shape.Kind == StructuralTypeKind.Record)
            return StructuralCarrierNaming.Create(index, path) + (shape.IsNullable ? "?" : "");
        if (shape.Kind != StructuralTypeKind.Collection || shape.ElementType is not { } element)
            return null;
        var elementTypeName = StorageTypeName(element, index, [.. path, -1]);
        return elementTypeName is null ? null : elementTypeName + "[]";
    }
}
