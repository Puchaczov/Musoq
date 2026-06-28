using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static TypedQueryInspectionResult CompileForTypedInspection<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForTypedInspection<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            new CompilationOptions());
    }

    public static TypedQueryInspectionResult CompileForTypedInspection<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        return CompileForTypedInspection<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            []);
    }

    internal static TypedQueryInspectionResult CompileForTypedInspection<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        ArgumentNullException.ThrowIfNull(additionalReferenceTypes);

        try
        {
            var items = BuildTypedItems<TOut>(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                compilationOptions,
                additionalReferenceTypes,
                CreateInspectionBuildChain,
                emitExecutionPlanText: true);

            var product = CreateTypedBuildProduct(items, null);
            var query = CreateInspectionResult(items);
            var metadata = product.RenderMetadata;
            var rowsKind = ResolveTypedGeneratedRowsKind(metadata.RowPathKind);
            return new TypedQueryInspectionResult(
                query,
                product.ResultMode,
                metadata.FinalResultSinkKind,
                typeof(TOut),
                rowsKind,
                [])
            {
                RowPathKind = metadata.RowPathKind,
                RequiresComputeTableMethod = metadata.RequiresComputeTableMethod,
                FinalSinkRejectionKind = metadata.FinalSinkRejectionKind,
                FinalSinkRejectionReason = metadata.FinalSinkRejectionReason
            };
        }
        catch (InvalidOperationException exception) when (IsTypedOutputBindingFailure(exception))
        {
            return new TypedQueryInspectionResult(
                null,
                QueryResultMode.TypedEnumerable,
                FinalResultSinkKind.TypedSerialEnumerable,
                typeof(TOut),
                TypedGeneratedRowsKind.Unknown,
                [exception.Message]);
        }
        catch (Exception exception) when (IsTypedOutputRenderingFailure(exception))
        {
            return new TypedQueryInspectionResult(
                null,
                QueryResultMode.TypedEnumerable,
                FinalResultSinkKind.TypedSerialEnumerable,
                typeof(TOut),
                TypedGeneratedRowsKind.Unknown,
                [exception.Message])
            {
                FinalSinkRejectionKind = FinalProjectionSinkRejectionKind.Unknown,
                FinalSinkRejectionReason = exception.Message
            };
        }
    }

    private static bool IsTypedOutputBindingFailure(InvalidOperationException exception)
    {
        return exception.Message.StartsWith("Typed output ", StringComparison.Ordinal);
    }

    private static bool IsTypedOutputRenderingFailure(Exception exception)
    {
        return exception is NotSupportedException &&
               exception.Message.Contains(
            "Typed enumerable result mode requires direct typed output",
            StringComparison.Ordinal);
    }

    private static TypedGeneratedRowsKind ResolveTypedGeneratedRowsKind(QueryResultRowPathKind rowPathKind)
    {
        return rowPathKind switch
        {
            QueryResultRowPathKind.DirectRows => TypedGeneratedRowsKind.DirectRows,
            QueryResultRowPathKind.ShardRows => TypedGeneratedRowsKind.ShardRows,
            QueryResultRowPathKind.MaterializedTableRows => TypedGeneratedRowsKind.MaterializedTableRows,
            _ => TypedGeneratedRowsKind.Unknown
        };
    }
}
