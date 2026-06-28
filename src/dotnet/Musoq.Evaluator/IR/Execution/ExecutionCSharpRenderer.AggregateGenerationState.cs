using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private void EnsureAggregateGenerationState(ExecutionPlan plan)
    {
        _aggregateGroupTypeNames.Clear();
        _parallelFilterProjectFunctionNames.Clear();
        _parallelSingleKeyAggregateFunctionNames.Clear();

        AssignAggregateGroupTypeNames(plan.Shapes.OfType<AggregateGroupShape>().ToArray());
        AssignParallelFilterProjectFunctionNames(CollectParallelFilterProjectLoops(plan.Body).ToArray());
        AssignParallelSingleKeyAggregateFunctionNames(CollectParallelSingleKeyAggregateLoops(plan.Body).ToArray());
    }

    private void AssignAggregateGroupTypeNames(IReadOnlyList<AggregateGroupShape> shapes)
    {
        var usedNames = new HashSet<string>(
            shapes.Select(static shape => shape.TypeName),
            StringComparer.Ordinal);
        var duplicateIndex = 0;

        foreach (var group in shapes.GroupBy(CreateAggregateGroupShapeSignature))
        {
            var groupShapes = group.ToArray();
            if (groupShapes.Length == 1)
            {
                _aggregateGroupTypeNames[group.Key] = groupShapes[0].TypeName;
                continue;
            }

            var typeName = CreateSharedAggregateGroupTypeName(usedNames, duplicateIndex++);
            foreach (var shape in groupShapes)
                _aggregateGroupTypeNames[CreateAggregateGroupShapeSignature(shape)] = typeName;
        }
    }

    private void AssignParallelSingleKeyAggregateFunctionNames(IReadOnlyList<ExecutionParallelSingleKeyAggregateLoop> loops)
    {
        var helperIndex = 0;

        foreach (var loop in loops)
        {
            var descriptor = CreateParallelSingleKeyAggregateDescriptor(loop);
            if (_parallelSingleKeyAggregateFunctionNames.ContainsKey(descriptor))
                continue;

            var suffix = helperIndex.ToString(CultureInfo.InvariantCulture);
            _parallelSingleKeyAggregateFunctionNames[descriptor] = $"ParallelSingleKeyAggregate_{suffix}";
            helperIndex++;
        }
    }

    private void AssignParallelFilterProjectFunctionNames(IReadOnlyList<ExecutionParallelFilterProjectLoop> loops)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var loop in loops)
        {
            if (_parallelFilterProjectFunctionNames.ContainsKey(loop))
                continue;

            var baseName = $"Populate{CreatePascalIdentifier(loop.AppendRow.Table.Name)}";
            var functionName = CreateUniqueHelperName(baseName, usedNames);
            _parallelFilterProjectFunctionNames.Add(loop, functionName);
        }
    }

    private string CreateParallelFilterProjectFunctionName(ExecutionParallelFilterProjectLoop parallelProject)
    {
        return _parallelFilterProjectFunctionNames.TryGetValue(parallelProject, out var functionName)
            ? functionName
            : $"Populate{CreatePascalIdentifier(parallelProject.AppendRow.Table.Name)}";
    }

    private static string CreateUniqueHelperName(string baseName, HashSet<string> usedNames)
    {
        var index = 0;
        while (true)
        {
            var candidate = index == 0
                ? baseName
                : $"{baseName}{index.ToString(CultureInfo.InvariantCulture)}";
            if (usedNames.Add(candidate))
                return candidate;

            index++;
        }
    }

    private static string CreatePascalIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        var capitalizeNext = true;
        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext
                ? char.ToUpperInvariant(character)
                : character);
            capitalizeNext = false;
        }

        if (builder.Length == 0)
            return "_";

        if (!SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
            builder.Insert(0, '_');

        var identifier = builder.Length <= GeneratedRowNamingPolicy.MaxIdentifierLength
            ? builder.ToString()
            : builder.ToString(0, GeneratedRowNamingPolicy.MaxIdentifierLength);
        return SyntaxFacts.IsValidIdentifier(identifier)
            ? identifier
            : CreateIdentifierCandidate(identifier, 0);
    }

    private static string CreateSharedAggregateGroupTypeName(HashSet<string> usedNames, int duplicateIndex)
    {
        while (true)
        {
            var typeName = $"AggregateGroup{duplicateIndex.ToString(CultureInfo.InvariantCulture)}";
            if (usedNames.Add(typeName))
                return typeName;

            duplicateIndex++;
        }
    }

    private string GetAggregateGroupTypeName(AggregateGroupShape shape)
    {
        return _aggregateGroupTypeNames.TryGetValue(CreateAggregateGroupShapeSignature(shape), out var typeName)
            ? typeName
            : shape.TypeName;
    }

    private AggregateGroupShape CreateRenderableAggregateGroupShape(AggregateGroupShape shape)
    {
        return shape with
        {
            TypeName = GetAggregateGroupTypeName(shape),
            OwnerFields = shape.OwnerFields
                .Select(owner => owner with { Shape = CreateRenderableAggregateGroupShape(owner.Shape) })
                .ToArray()
        };
    }

    private string CreateParallelSingleKeyAggregateFunctionName(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var descriptor = CreateParallelSingleKeyAggregateDescriptor(parallelAggregate);
        return _parallelSingleKeyAggregateFunctionNames.TryGetValue(descriptor, out var functionName)
            ? functionName
            : $"ParallelSingleKeyAggregate_{parallelAggregate.GroupsToFinalize.Name}";
    }

    private string CreateParallelSingleKeyAggregateDescriptor(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder
            .Append("source:")
            .Append(ExecutionExpressionFingerprint.ForAggregateType(parallelAggregate.Source.Type))
            .Append("|keyType:")
            .Append(ExecutionExpressionFingerprint.ForAggregateType(parallelAggregate.KeyType))
            .Append("|group:")
            .Append(CreateAggregateGroupShapeSignature(parallelAggregate.GroupShape))
            .Append("|key:")
            .Append(ExecutionExpressionFingerprint.ForParallelAggregate(parallelAggregate.Key, parallelAggregate))
            .Append("|body:")
            .Append(CreateBlockSignature(parallelAggregate.AggregateBody, parallelAggregate))
            .Append("|captures:");

        foreach (var capture in CollectParallelSingleKeyAggregateCaptures(parallelAggregate))
        {
            builder
                .Append(ExecutionExpressionFingerprint.ForAggregateType(capture.Type))
                .Append(';');
        }

        return builder.ToString();
    }
}
