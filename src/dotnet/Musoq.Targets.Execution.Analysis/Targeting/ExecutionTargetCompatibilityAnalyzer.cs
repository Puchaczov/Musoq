using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Schema;
using Musoq.Targets.Execution;

namespace Musoq.Targets.Execution.Analysis;

internal static class ExecutionTargetCompatibilityAnalyzer
{
    public static ExecutionTargetCompatibilityReport Analyze(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sink = new RequirementSink();

        foreach (var shape in plan.Shapes)
            AnalyzeShape(shape, sink);

        if (plan.FinalResult != null)
        {
            sink.AddClrType(plan.FinalResult.Source.Type);
            AnalyzeShape(plan.FinalResult.Shape, sink);
        }

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(plan.Body))
            AnalyzeNode(node, sink);

        foreach (var expression in ExecutionIrAnalysis.FlattenExpressions(plan.Body))
            AnalyzeExpression(expression, plan.Shapes, sink);

        return new ExecutionTargetCompatibilityReport(sink.ToArray());
    }

    private static void AnalyzeShape(RowShape shape, RequirementSink sink)
    {
        switch (shape)
        {
            case SourceEntityShape source:
                sink.Add(
                    ExecutionTargetRequirementKind.SchemaProviderBinding,
                    $"source entity '{source.Alias}' uses {FormatType(source.EntityType)}");
                sink.AddClrType(source.EntityType);
                AddFields(source.Fields, sink);
                AddClrPropertyPathTypes(source, sink);
                break;
            case GeneratedRowShape generated:
                AddGeneratedRow(generated.TypeName, generated.Fields.Concat(generated.Contexts), sink);
                AddFields(generated.Fields, sink);
                AddFields(generated.Contexts, sink);
                break;
            case GeneratedRecordShape generated:
                AddGeneratedRow(generated.TypeName, generated.Fields, sink);
                AddFields(generated.Fields, sink);
                break;
            case HashPayloadShape payload:
                AddGeneratedRow(payload.TypeName, payload.Fields.Concat(payload.Contexts), sink);
                AddFields(payload.Fields, sink);
                AddFields(payload.Contexts, sink);
                break;
            case AggregateGroupShape aggregate:
                AddGeneratedRow(aggregate.TypeName, CreateAggregatePortableFields(aggregate), sink);
                AddAggregateGroupShape(aggregate, sink);
                break;
            case ValuesRowShape values:
                AnalyzeShape(values.GeneratedShape, sink);
                break;
            case ExpandoAdapterShape expando:
                sink.AddClrType(expando.RuntimeType);
                AddFields(expando.Fields, sink);
                break;
            case TableRowShape table:
                AddFields(table.Fields, sink);
                AddFields(table.Contexts, sink);
                break;
            default:
                AddFields(shape.Fields, sink);
                break;
        }
    }

    private static void AnalyzeNode(ExecutionNode node, RequirementSink sink)
    {
        foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            sink.AddClrType(variable.Type);

        switch (node)
        {
            case ExecutionSourceScan sourceScan:
                sink.Add(
                    ExecutionTargetRequirementKind.SchemaProviderBinding,
                    $"source scan '{sourceScan.Binding.SchemaName}.{sourceScan.Binding.MethodName}'");
                sink.AddClrType(sourceScan.Source.Type);
                sink.AddClrType(sourceScan.Rows.Type);
                if (sourceScan.Binding.SourceType != null)
                    sink.AddClrType(sourceScan.Binding.SourceType);
                AddFields(sourceScan.Binding.Fields, sink);
                break;
            case ExecutionInterpretSource interpret:
                sink.Add(
                    ExecutionTargetRequirementKind.SchemaProviderBinding,
                    $"interpret source '{interpret.SchemaName}' via {interpret.InterpreterTypeName}");
                sink.AddClrType(interpret.Rows.Type);
                break;
            case ExecutionEnumerableSource enumerable:
                sink.AddClrType(enumerable.Rows.Type);
                sink.AddClrType(enumerable.EnumerableType);
                break;
            case ExecutionComputePluginWindow plugin:
                AddCallable(plugin.FactoryMethod, plugin.FunctionName, sink);
                break;
        }
    }

    private static void AnalyzeExpression(
        ExecutionExpression expression,
        IReadOnlyList<RowShape> shapes,
        RequirementSink sink)
    {
        sink.AddClrType(expression.ReturnType);

        switch (expression)
        {
            case ExecutionFieldRead fieldRead
                when fieldRead.Alias is { Length: > 0 } alias &&
                     (fieldRead.FieldName.Contains('.', StringComparison.Ordinal) ||
                      fieldRead.FieldName.Contains('[', StringComparison.Ordinal)):
                if (TryGetSourceEntityShape(shapes, alias) is { } sourceShape)
                    AddClrPropertyPathTypes(sourceShape, fieldRead.FieldName, sink);
                break;
            case ExecutionLiteral { Value.Kind: ExecutionConstantKind.ClrOnly } literal:
                sink.Add(
                    ExecutionTargetRequirementKind.ClrOnlyConstant,
                    literal.Value.ClrOnlyType?.DisplayName ?? "unknown CLR literal",
                    typeSymbol: literal.Value.ClrOnlyType?.Descriptor);
                break;
            case ExecutionMethodCall methodCall:
                AddCallable(methodCall.Method, methodCall.Method.MethodName, sink);
                break;
            case ExecutionAggregateCall aggregateCall:
                AddCallable(aggregateCall.Method, aggregateCall.DisplayName ?? aggregateCall.Method.MethodName, sink);
                AddAccumulator(aggregateCall.Accumulator, sink);
                break;
        }
    }

    private static SourceEntityShape? TryGetSourceEntityShape(
        IReadOnlyList<RowShape> shapes,
        string alias)
    {
        foreach (var shape in shapes)
        {
            if (shape is SourceEntityShape source &&
                string.Equals(source.Alias, alias, StringComparison.OrdinalIgnoreCase))
                return source;
        }

        return null;
    }

    private static void AddFields(IEnumerable<FieldBinding> fields, RequirementSink sink)
    {
        foreach (var field in fields)
        {
            sink.AddClrType(field.Type);
            if (field.PublicType != null)
                sink.AddClrType(field.PublicType);
        }
    }

    private static void AddClrPropertyPathTypes(SourceEntityShape source, RequirementSink sink)
    {
        Type entityType;
        try
        {
            entityType = source.EntityType.ResolveClrType();
        }
        catch (NotSupportedException)
        {
            return;
        }

        foreach (var field in source.Fields)
        {
            var path = field.AccessStrategy switch
            {
                ClrPropertyAccess property => property.PropertyName,
                NestedClrPropertyAccess nested => nested.PropertyPath,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(path))
                continue;

            AddClrPropertyPathTypes(entityType, path, sink);
        }
    }

    private static void AddClrPropertyPathTypes(
        SourceEntityShape source,
        string path,
        RequirementSink sink)
    {
        try
        {
            AddClrPropertyPathTypes(source.EntityType.ResolveClrType(), path, sink);
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void AddClrPropertyPathTypes(Type entityType, string path, RequirementSink sink)
    {
        var currentType = entityType;
        var segments = path
            .Split(['.', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out _))
                continue;

            var member = ResolvePublicMember(currentType, segment);
            if (member is null)
                return;

            if (member.DeclaringType is { } declaringType)
                sink.AddClrType(declaringType);

            var memberType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };
            if (memberType is null)
                return;

            sink.AddClrType(memberType);
            currentType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        }
    }

    private static MemberInfo? ResolvePublicMember(Type type, string name)
    {
        return type
            .GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            .FirstOrDefault(member => member switch
            {
                PropertyInfo property => property.GetMethod is { IsPublic: true },
                FieldInfo field => field.IsPublic,
                _ => false
            });
    }

    private static void AddAggregateGroupShape(AggregateGroupShape shape, RequirementSink sink)
    {
        foreach (var key in shape.Keys)
            sink.AddClrType(key.Type);

        foreach (var captured in shape.CapturedFields)
            sink.AddClrType(captured.Type);

        foreach (var accumulator in shape.Accumulators)
            AddAccumulator(accumulator, sink);

        foreach (var owner in shape.OwnerFields)
            AddGeneratedRow(owner.Shape.TypeName, CreateAggregatePortableFields(owner.Shape), sink);
    }

    private static void AddAccumulator(AggregateAccumulatorField accumulator, RequirementSink sink)
    {
        sink.AddClrType(accumulator.InputType);
        sink.AddClrType(accumulator.ResultType);
        sink.AddClrType(accumulator.AccumulatorType);
    }

    private static IEnumerable<ExecutionPortableRowFieldDescriptor> CreateAggregatePortableFields(
        AggregateGroupShape shape)
    {
        foreach (var key in shape.Keys)
        {
            yield return new ExecutionPortableRowFieldDescriptor(
                key.FieldName,
                key.Type.Descriptor,
                FieldNullability.Unknown.ToString());
        }

        foreach (var captured in shape.CapturedFields)
        {
            yield return new ExecutionPortableRowFieldDescriptor(
                captured.FieldName,
                captured.Type.Descriptor,
                FieldNullability.Unknown.ToString());
        }

        foreach (var accumulator in shape.Accumulators)
        {
            yield return new ExecutionPortableRowFieldDescriptor(
                accumulator.FieldName,
                accumulator.ResultType.Descriptor,
                FieldNullability.Unknown.ToString());
        }

        foreach (var owner in shape.OwnerFields)
        {
            yield return new ExecutionPortableRowFieldDescriptor(
                owner.FieldName,
                ExecutionPortableSymbolFactory.GeneratedRow(
                    owner.Shape.TypeName,
                    CreateAggregatePortableFields(owner.Shape)),
                FieldNullability.Unknown.ToString());
        }
    }

    private static void AddGeneratedRow(
        string typeName,
        IEnumerable<FieldBinding> fields,
        RequirementSink sink)
    {
        AddGeneratedRow(
            typeName,
            fields.Select(static field => new ExecutionPortableRowFieldDescriptor(
                field.Name,
                field.Type.Descriptor,
                field.Nullability.ToString())),
            sink);
    }

    private static void AddGeneratedRow(
        string typeName,
        IEnumerable<ExecutionPortableRowFieldDescriptor> fields,
        RequirementSink sink)
    {
        sink.Add(
            ExecutionTargetRequirementKind.GeneratedClrRow,
            typeName,
            typeSymbol: ExecutionPortableSymbolFactory.GeneratedRow(typeName, fields));
    }

    private static void AddCallable(ExecutionCallableRef method, string functionName, RequirementSink sink)
    {
        var callable = method.Descriptor;
        sink.Add(
            ExecutionTargetRequirementKind.MethodInfoCall,
            method.DisplayName,
            callableSymbol: callable);

        if (callable.Kind is not (
                ExecutionPortableCallableKind.HostPlugin or
                ExecutionPortableCallableKind.HostAggregate))
        {
            return;
        }

        sink.Add(
            ExecutionTargetRequirementKind.PluginInvocation,
            $"{functionName} -> {method.DisplayName}",
            callableSymbol: callable);
    }

    private static string FormatType(Type type)
    {
        if (type.IsArray)
            return $"{FormatType(type.GetElementType()!)}[]";

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tickIndex = name.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
            name = name[..tickIndex];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static string FormatType(ExecutionTypeRef type) => type.ClrDisplayName();

    private sealed class RequirementSink
    {
        private readonly Dictionary<RequirementIdentity, ExecutionTargetRequirement> _seen = [];
        private readonly List<ExecutionTargetRequirement> _requirements = [];

        public void Add(
            ExecutionTargetRequirementKind kind,
            string detail,
            ExecutionPortableTypeDescriptor? typeSymbol = null,
            ExecutionPortableCallableDescriptor? callableSymbol = null)
        {
            var requirement = new ExecutionTargetRequirement(kind, detail, typeSymbol, callableSymbol);
            var identity = new RequirementIdentity(
                kind,
                detail,
                typeSymbol?.StableName,
                callableSymbol?.StableName);
            if (_seen.TryGetValue(identity, out var existing))
            {
                if (!RequirementDefinitionsMatch(existing, requirement))
                {
                    throw new InvalidOperationException(
                        $"Execution target requirement '{kind}:{detail}' has conflicting symbol definitions.");
                }

                return;
            }

            _seen.Add(identity, requirement);
            _requirements.Add(requirement);
        }

        public void AddClrType(Type? type)
        {
            if (type == null)
                return;

            Add(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                FormatType(type),
                typeSymbol: ExecutionPortableSymbolFactory.FromType(type));
        }

        public void AddClrType(ExecutionTypeRef type)
        {
            Add(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                type.ClrDisplayName(),
                typeSymbol: type.Descriptor);
        }

        public IReadOnlyList<ExecutionTargetRequirement> ToArray()
        {
            return _requirements.ToArray();
        }

        private static bool RequirementDefinitionsMatch(
            ExecutionTargetRequirement left,
            ExecutionTargetRequirement right)
        {
            return left.Kind == right.Kind &&
                   string.Equals(left.Detail, right.Detail, StringComparison.Ordinal) &&
                   ExecutionPortableSymbolDefinitionComparer.AreEquivalent(left.TypeSymbol, right.TypeSymbol) &&
                   ExecutionPortableSymbolDefinitionComparer.AreEquivalent(left.CallableSymbol, right.CallableSymbol);
        }

        private readonly record struct RequirementIdentity(
            ExecutionTargetRequirementKind Kind,
            string Detail,
            string? TypeStableName,
            string? CallableStableName);
    }
}
