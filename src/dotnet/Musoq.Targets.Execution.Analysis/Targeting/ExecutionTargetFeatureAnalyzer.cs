using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.Execution.Analysis;

internal static class ExecutionTargetFeatureAnalyzer
{
    public static ExecutionTargetFeatureReport Analyze(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sink = new FeatureSink();
        foreach (var shape in plan.Shapes)
            AddShape(shape, sink);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(plan.Body))
            AddNode(node, sink);

        foreach (var expression in ExecutionIrAnalysis.FlattenExpressions(plan.Body))
            AddExpression(expression, sink);

        return new ExecutionTargetFeatureReport(sink.Features);
    }

    private static void AddNode(ExecutionNode node, FeatureSink sink)
    {
        foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            sink.AddType(variable.Type.Descriptor);

        switch (node)
        {
            case ExecutionSourceScan source:
                sink.AddSource("scan");
                sink.AddType(source.Source.Type.Descriptor);
                sink.AddType(source.Rows.Type.Descriptor);
                if (source.Binding.SourceType != null)
                    sink.AddType(source.Binding.SourceType.Descriptor);
                AddFields(source.Binding.Fields, sink);
                break;
            case ExecutionInterpretSource interpret:
                sink.AddSource("interpret");
                sink.AddType(interpret.Rows.Type.Descriptor);
                break;
            case ExecutionEnumerableSource enumerable:
                sink.AddSource("enumerable");
                sink.AddType(enumerable.Rows.Type.Descriptor);
                sink.AddType(enumerable.EnumerableType.Descriptor);
                break;
            case ExecutionComputePluginWindow plugin:
                sink.AddCallable(plugin.FactoryMethod.Descriptor);
                break;
        }
    }

    private static void AddShape(RowShape shape, FeatureSink sink)
    {
        foreach (var field in shape.Fields)
            sink.AddField(field);

        if (shape is GeneratedRowShape generatedRow)
        {
            foreach (var context in generatedRow.Contexts)
                sink.AddField(context);
        }
    }

    private static void AddFields(IEnumerable<FieldBinding> fields, FeatureSink sink)
    {
        foreach (var field in fields)
            sink.AddField(field);
    }

    private static void AddExpression(ExecutionExpression expression, FeatureSink sink)
    {
        sink.AddType(expression.ReturnType.Descriptor);

        switch (expression)
        {
            case ExecutionLiteral literal:
                sink.Add(ExecutionTargetFeatureKind.ConstantKind, ConstantId(literal.Value.Kind), literal.Value.Kind.ToString());
                if (literal.Value.EnumType != null)
                    sink.AddType(literal.Value.EnumType.Descriptor);
                if (literal.Value.ClrOnlyType != null)
                    sink.AddType(literal.Value.ClrOnlyType.Descriptor);
                break;
            case ExecutionBinary binary:
                sink.Add(ExecutionTargetFeatureKind.BinaryOperation, BinaryId(binary.Kind), binary.Kind.ToString());
                break;
            case ExecutionUnary unary:
                sink.Add(ExecutionTargetFeatureKind.UnaryOperation, UnaryId(unary.Kind), unary.Kind.ToString());
                break;
            case ExecutionStrictCast cast:
                sink.Add(
                    ExecutionTargetFeatureKind.StrictCastTarget,
                    $"strict-cast:{cast.ReturnType.Descriptor.StableName}",
                    cast.TargetTypeName);
                break;
            case ExecutionMethodCall methodCall:
                sink.AddCallable(methodCall.Method.Descriptor);
                break;
            case ExecutionAggregateCall aggregateCall:
                sink.AddCallable(aggregateCall.Method.Descriptor);
                break;
        }
    }

    private static string ConstantId(ExecutionConstantKind kind) => $"constant:{ToStableToken(kind.ToString())}";

    private static string BinaryId(BinaryOpKind kind) => $"binary:{ToStableToken(kind.ToString())}";

    private static string UnaryId(UnaryOpKind kind) => $"unary:{ToStableToken(kind.ToString())}";

    private static string ToStableToken(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $"-{char.ToLowerInvariant(character)}"
                : char.ToLowerInvariant(character).ToString()));
    }

    private sealed class FeatureSink
    {
        private readonly HashSet<ExecutionTargetFeature> _features = [];

        public IEnumerable<ExecutionTargetFeature> Features => _features;

        public void Add(ExecutionTargetFeatureKind kind, string stableId, string detail) =>
            _features.Add(new ExecutionTargetFeature(kind, stableId, detail));

        public void AddSource(string sourceKind) =>
            Add(ExecutionTargetFeatureKind.SourceKind, $"source:{sourceKind}", sourceKind);

        public void AddField(FieldBinding field)
        {
            AddType(field.Type.Descriptor);
            if (field.PublicType != null)
                AddType(field.PublicType.Descriptor);

            foreach (var modifier in field.ReadModifiers.Keys)
                Add(ExecutionTargetFeatureKind.ReadModifier, $"read-modifier:{modifier}", modifier);
        }

        public void AddCallable(ExecutionPortableCallableDescriptor callable)
        {
            Add(ExecutionTargetFeatureKind.Callable, $"callable:{callable.StableName}", callable.DisplayName);
            Add(
                ExecutionTargetFeatureKind.CallableKind,
                $"callable-kind:{ToStableToken(callable.Kind.ToString())}",
                callable.Kind.ToString());
            if (callable.DeclaringType != null)
                AddType(callable.DeclaringType);
            if (callable.ReturnType != null)
                AddType(callable.ReturnType);
            foreach (var parameter in callable.ParameterTypes)
                AddType(parameter);
        }

        public void AddType(ExecutionPortableTypeDescriptor type)
        {
            Add(
                ExecutionTargetFeatureKind.TypePortability,
                $"type-portability:{ToStableToken(type.Portability.ToString())}",
                type.Portability.ToString());
            if (type.Container != null)
            {
                Add(
                    ExecutionTargetFeatureKind.Container,
                    $"container:{ToStableToken(type.Container.Kind.ToString())}",
                    type.Container.Kind.ToString());
            }

            if (string.Equals(type.StableName, "host-opaque:dynamic-object", StringComparison.Ordinal))
                Add(ExecutionTargetFeatureKind.DynamicValue, "dynamic:host-opaque", type.DisplayName);

            foreach (var argument in type.Arguments)
                AddType(argument);
            foreach (var field in type.Fields)
                AddType(field.Type);
        }
    }
}
