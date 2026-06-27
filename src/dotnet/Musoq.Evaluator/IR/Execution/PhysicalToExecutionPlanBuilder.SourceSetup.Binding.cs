using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{

    private static ExecutionExpression CreatePropertySourceExpression(
        PhysicalPropertySourceNode property,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (property.PropertiesChain.Length == 0)
        {
            throw new NotSupportedException(
                $"Execution IR property-source lowering requires at least one property for alias '{property.Alias}'.");
        }

        var propertyPath = string.Join(".", property.PropertiesChain.Select(item => item.PropertyName));

        return ExecutionExpressionConverter.Convert(
            new ColumnRef(property.SourceAlias, propertyPath, property.ResultType),
            sourceLookup);
    }

    private static bool IsRowSourceType(Type type)
    {
        Type? current = type;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(RowSource<>))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static ExecutionSourceBinding CreateSourceBinding(
        PhysicalSchemaScanNode scan,
        RowShape sourceShape,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, int> cteIndexes)
    {
        return new ExecutionSourceBinding(
            scan.SchemaName,
            scan.MethodName,
            scan.SourceContextId ?? CreateRuntimeContextIdentifier(scan.Alias),
            schemaFromIndex,
            scan.Arguments.Select(argument => ExecutionExpressionConverter.Convert(argument, sourceLookup, cteIndexes)).ToArray(),
            sourceShape.Fields,
            CreateColumnMetadata(scan.Alias, sourceShape.Fields, ExecutionColumnMetadataKind.SourceSchemaColumns),
            RowShapeLookup.ResolveSourceRequestType(sourceShape));
    }

    private static string CreateRuntimeContextIdentifier(string alias)
    {
        return $"{alias}:{SourceInstanceOrdinal}";
    }

    private static string CreateResolverVariableName(string alias)
    {
        return $"{alias}Resolver";
    }
}
