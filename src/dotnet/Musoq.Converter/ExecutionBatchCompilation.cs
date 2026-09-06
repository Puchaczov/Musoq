using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using EvaluatorCompilationOptions = Musoq.Evaluator.CompilationOptions;
using ParserDiagnostic = Musoq.Parser.Diagnostics.Diagnostic;

namespace Musoq.Converter;

/// <summary>
/// Internal input for exhaustive test compilation. The normal public compilation
/// APIs remain single-query APIs; batching is deliberately opt-in and scoped to
/// compatible generated CLR queries.
/// </summary>
internal sealed record ExecutionBatchCompilationRequest(
    string Key,
    string Script,
    string AssemblyName,
    ISchemaProvider SchemaProvider,
    ILoggerResolver LoggerResolver,
    EvaluatorCompilationOptions CompilationOptions,
    string? ConsumerFamily = null,
    string? ConsumerTestName = null,
    string BatchOrigin = "execution-batch",
    long EnqueuedTimestamp = 0);

internal sealed record ExecutionBatchCompilationResult(
    string Key,
    BuildResult Result);

public static partial class InstanceCreator
{
    // Exhaustive test batches contain independent semantic pipelines. These
    // budgets are process-wide because several test coordinators can submit
    // batches concurrently; per-call Parallel limits alone oversubscribe the
    // compiler and the Roslyn finalizer.
    private const int ExecutionBatchPreparationDegree = 16;
    private const int ExecutionBatchFinalizationDegree = 2;
    private static readonly SemaphoreSlim ExecutionBatchPreparationBudget = new(16, 16);
    private static readonly SemaphoreSlim ExecutionBatchFinalizationBudget = new(2, 2);

    /// <summary>
    /// Compiles compatible generated queries in shared Roslyn emissions while
    /// returning one normal <see cref="BuildResult"/> per request.
    /// </summary>
    internal static IReadOnlyList<ExecutionBatchCompilationResult> CompileForExecutionBatch(
        IReadOnlyList<ExecutionBatchCompilationRequest> requests)
    {
        return CompileForExecutionBatch(requests, CancellationToken.None);
    }

    internal static IReadOnlyList<ExecutionBatchCompilationResult> CompileForExecutionBatch(
        IReadOnlyList<ExecutionBatchCompilationRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        cancellationToken.ThrowIfCancellationRequested();
        if (requests.Count == 0)
            return Array.Empty<ExecutionBatchCompilationResult>();

        var prepared = new ConcurrentBag<PreparedExecutionBatchItem>();
        var results = new ConcurrentDictionary<string, BuildResult>(StringComparer.Ordinal);
        var batchId = $"execution-batch-{Guid.NewGuid():N}";
        var uniqueRequests = new List<ExecutionBatchCompilationRequest>(requests.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (keys.Add(request.Key))
            {
                uniqueRequests.Add(request);
                continue;
            }

            results[request.Key] = BuildResult.Failure(
                Array.Empty<ParserDiagnostic>(),
                request.Script,
                new ArgumentException($"Duplicate execution batch key '{request.Key}'."));
        }

        Parallel.ForEach(
            uniqueRequests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = ExecutionBatchPreparationDegree,
                CancellationToken = cancellationToken
            },
            request =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecutionBatchPreparationBudget.Wait(cancellationToken);
                try
                {
                    using var consumer = BeginConsumerScope(request);
                    prepared.Add(PrepareExecutionBatchItem(request, batchId, requests.Count, cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    lock (results)
                    {
                        results[request.Key] = CreateFailedBatchResult(request, exception, null);
                    }
                }
                finally
                {
                    ExecutionBatchPreparationBudget.Release();
                }
            });

        var groups = prepared
            .GroupBy(item => CreateBatchCompatibilityKey(item, cancellationToken), StringComparer.Ordinal)
            .Select(static group => group.ToArray())
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        var compatibilityGroupCount = groups.Length;
        try
        {
            Parallel.ForEach(
                groups,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = ExecutionBatchFinalizationDegree,
                    CancellationToken = cancellationToken
                },
                group =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExecutionBatchFinalizationBudget.Wait(cancellationToken);
                    try
                    {
                        FinalizeBatchGroup(group, results, batchId, compatibilityGroupCount, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    finally
                    {
                        ExecutionBatchFinalizationBudget.Release();
                    }
                });

            cancellationToken.ThrowIfCancellationRequested();
            return requests
                .Select(request => new ExecutionBatchCompilationResult(
                    request.Key,
                    results.TryGetValue(request.Key, out var result)
                        ? result
                        : BuildResult.Failure(
                            Array.Empty<ParserDiagnostic>(),
                            request.Script,
                            new InvalidOperationException(
                                $"Execution batch did not produce a result for '{request.Key}'."))))
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            DisposeBatchResults(results.Values);
            throw;
        }
    }

    private static PreparedExecutionBatchItem PrepareExecutionBatchItem(
        ExecutionBatchCompilationRequest request,
        string batchId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request.SchemaProvider);
        ArgumentNullException.ThrowIfNull(request.LoggerResolver);

        using var telemetry = EvaluatorPerformanceTelemetry.BeginCompilation(
            request.Script,
            request.AssemblyName,
            request.SchemaProvider,
            request.CompilationOptions);
        telemetry.SetCompilationMode("batch", batchId, batchSize);
        telemetry.SetReusePath("batch-preparation");
        if (EvaluatorPerformanceTelemetry.IsEnabled)
            telemetry.SetProviderSignature(CreateProviderSignature(request.SchemaProvider, cancellationToken));

        var diagnosticContext = new DiagnosticContext(new SourceText(request.Script));
        var items = CreateBuildItems(
            request.Script,
            request.AssemblyName,
            request.SchemaProvider,
            diagnosticContext,
            cancellationToken);
        items.EmitPdb = false;
        items.EmitExecutionPlanText = false;
        items.CompilationOptions = request.CompilationOptions;
        items.EnableContextualExecution = true;

        Build(items, CreateInspectionBuildChain(request.LoggerResolver));
        cancellationToken.ThrowIfCancellationRequested();
        RejectUnsupportedMultiStatementQuery(items.RawQueryTree);

        if (diagnosticContext.HasErrors)
            throw new BatchPreparationException(
                "Query preparation produced diagnostics.",
                diagnosticContext.Diagnostics.ToArray());

        _ = items.RenderingArtifact;
        return new PreparedExecutionBatchItem(request, items);
    }

    private static void FinalizeBatchGroup(
        IReadOnlyList<PreparedExecutionBatchItem> group,
        IDictionary<string, BuildResult> results,
        string batchId,
        int compatibilityGroupCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var telemetry = EvaluatorPerformanceTelemetry.BeginBatchFinalization(
            $"{batchId}-{Guid.NewGuid():N}",
            group.Count,
            CreateBatchCompatibilityKey(group[0], cancellationToken),
            group[0].Request.BatchOrigin,
            compatibilityGroupCount,
            GetQueueDelayMilliseconds(group[0].Request));
        TargetFinalizationResult? finalization = null;
        ExecutableQueryArtifact? artifact = null;
        IReadOnlyList<CSharpClrBatchActivationResult>? activations = null;
        try
        {
            finalization = FinalizeCompatibleGroup(group, cancellationToken);
            if (!finalization.Success || finalization.Artifact is null)
            {
                telemetry.SetFallbackReason("shared-finalization-failed");
                telemetry.SetResult(null, succeeded: false, emitted: false, loaded: false);
                FinalizeIndividually(group, results, new InvalidOperationException(
                    "Shared execution-batch emission failed: " +
                    string.Join("; ", finalization.Diagnostics.Select(static diagnostic => diagnostic.Message))),
                    cancellationToken);
                return;
            }

            try
            {
                artifact = CSharpClrBatchCompatibility.CreateBatchExecutable(
                    finalization,
                    group[0].Items.RenderingArtifacts.AccessToClassPath,
                    cancellationToken);
            }
            finally
            {
                // The batch executable owns copied bytes. The finalizer's
                // stream-backed artifact is no longer needed after that copy.
                (finalization.Artifact as IDisposable)?.Dispose();
            }

            var activationRequests = group
                .Select(static item => new CSharpClrBatchActivationRequest(
                    item.Items.RenderingArtifacts.AccessToClassPath,
                    CreateRuntimeBinding(item.Items)))
                .ToArray();
            activations = CSharpClrBatchCompatibility.ActivateBatch(
                group[0].Items.ExecutionTarget,
                artifact,
                activationRequests,
                cancellationToken);
            telemetry.SetResult(
                group[0].Items.RenderingArtifacts.AccessToClassPath,
                succeeded: true,
                emitted: true,
                loaded: true);
            (artifact as IDisposable)?.Dispose();
            artifact = null;

            for (var index = 0; index < group.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = group[index];
                var activation = activations[index];
                try
                {
                    if (!activation.Succeeded)
                        throw new InvalidOperationException(
                            $"Shared execution-batch activation failed for '{item.Request.Key}'.",
                            activation.Exception);

                    var runnable = activation.Runnable!;
                    runnable.Logger = item.Request.LoggerResolver.ResolveLogger();
                    results[item.Request.Key] = BuildResult.Success(
                        new CompiledQuery(runnable),
                        item.Items.DiagnosticContext.Diagnostics.ToArray(),
                        item.Request.Script,
                        item.Items);
                }
                catch (Exception exception)
                {
                    (activation.Runnable as IDisposable)?.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                    results[item.Request.Key] = CreateFailedBatchResult(
                        item.Request,
                        exception,
                        item.Items);
                }
            }
        }
        catch (OperationCanceledException)
        {
            DisposeBatchCancellationResources(group, results, activations, artifact);
            (finalization?.Artifact as IDisposable)?.Dispose();
            throw;
        }
        catch (Exception exception)
        {
            DisposeBatchCancellationResources(group, results, activations, artifact);
            (finalization?.Artifact as IDisposable)?.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
            telemetry.SetFallbackReason(exception.GetType().Name);
            telemetry.SetResult(null, succeeded: false, emitted: false, loaded: false);
            FinalizeIndividually(group, results, exception, cancellationToken);
        }
    }

    private static void FinalizeIndividually(
        IReadOnlyList<PreparedExecutionBatchItem> group,
        IDictionary<string, BuildResult> results,
        Exception batchException,
        CancellationToken cancellationToken)
    {
        foreach (var item in group)
        {
            ITableRunnable? runnable = null;
            CompiledQuery? compiledQuery = null;
            ExecutableQueryArtifact? artifact = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var finalization = FinalizeCompatibleGroup([item], cancellationToken);
                if (!finalization.Success || finalization.Artifact is null)
                    throw new InvalidOperationException(
                        "Individual execution emission failed after batch fallback: " +
                        string.Join("; ", finalization.Diagnostics.Select(static diagnostic => diagnostic.Message)),
                        batchException);

                artifact = CSharpClrBatchCompatibility.CreateBatchExecutable(
                    finalization,
                    item.Items.RenderingArtifacts.AccessToClassPath,
                    cancellationToken);
                var activator = ExecutionTargetCatalog.ResolveActivator(item.Items.ExecutionTarget);
                runnable = activator.ActivateTable(
                    artifact,
                    new QueryRuntimeBinding(
                        item.Items.SchemaProvider,
                        item.Items.SourceRuntimeSettingsBySourceContextId,
                        item.Items.SourceRuntimeSettingDescriptionsBySourceContextId,
                        CreateSourceExecutionPlans(item.Items)));
                cancellationToken.ThrowIfCancellationRequested();
                runnable.Logger = item.Request.LoggerResolver.ResolveLogger();
                cancellationToken.ThrowIfCancellationRequested();
                compiledQuery = new CompiledQuery(runnable);
                runnable = null;
                results[item.Request.Key] = BuildResult.Success(
                    compiledQuery,
                    item.Items.DiagnosticContext.Diagnostics.ToArray(),
                    item.Request.Script,
                    item.Items);
                compiledQuery = null;
            }
            catch (OperationCanceledException)
            {
                compiledQuery?.Dispose();
                (runnable as IDisposable)?.Dispose();
                throw;
            }
            catch (Exception exception)
            {
                compiledQuery?.Dispose();
                (runnable as IDisposable)?.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
                results[item.Request.Key] = CreateFailedBatchResult(item.Request, exception, item.Items);
            }
            finally
            {
                (artifact as IDisposable)?.Dispose();
            }
        }
    }

    private static void DisposeBatchCancellationResources(
        IReadOnlyList<PreparedExecutionBatchItem> group,
        IDictionary<string, BuildResult> results,
        IReadOnlyList<CSharpClrBatchActivationResult>? activations,
        ExecutableQueryArtifact? artifact)
    {
        for (var index = 0; index < group.Count; index++)
        {
            var key = group[index].Request.Key;
            if (results.TryGetValue(key, out var result) && result.CompiledQuery is { } compiledQuery)
            {
                compiledQuery.Dispose();
                continue;
            }

            if (activations is { Count: > 0 } && index < activations.Count)
                (activations[index].Runnable as IDisposable)?.Dispose();
        }

        (artifact as IDisposable)?.Dispose();
    }

    private static void DisposeBatchResults(IEnumerable<BuildResult> results)
    {
        foreach (var result in results)
            result.CompiledQuery?.Dispose();
    }

    private static TargetFinalizationResult FinalizeCompatibleGroup(
        IReadOnlyList<PreparedExecutionBatchItem> group,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return CSharpClrBatchCompatibility.FinalizeBatch(
            group.Select(static item => item.Items.RenderingArtifacts).ToArray(),
            group[0].Items.ExecutionTarget,
            group[0].Items.EmitPdb,
            cancellationToken);
    }

    private static string CreateBatchCompatibilityKey(
        PreparedExecutionBatchItem item,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return CSharpClrBatchCompatibility.CreateBatchCompatibilityKey(
            item.Items.RenderingArtifacts,
            item.Items.ExecutionTarget,
            item.Items.EmitPdb,
            item.Items.InterpreterSourceCode is not null,
            item.Items.QueryResultMode,
            cancellationToken);
    }

    private static IDisposable BeginConsumerScope(ExecutionBatchCompilationRequest request)
    {
        return string.IsNullOrWhiteSpace(request.ConsumerFamily)
            ? NoopConsumerScope.Instance
            : EvaluatorPerformanceTelemetry.BeginConsumerScope(
                request.ConsumerFamily!,
                request.ConsumerTestName);
    }

    private static double? GetQueueDelayMilliseconds(ExecutionBatchCompilationRequest request)
    {
        return request.EnqueuedTimestamp == 0
            ? null
            : Stopwatch.GetElapsedTime(request.EnqueuedTimestamp).TotalMilliseconds;
    }

    private static BuildResult CreateFailedBatchResult(
        ExecutionBatchCompilationRequest request,
        Exception exception,
        BuildItems? items)
    {
        var diagnostics = exception is BatchPreparationException preparation
            ? preparation.Diagnostics
            : items?.DiagnosticContext.Diagnostics.ToArray() ?? Array.Empty<ParserDiagnostic>();
        return BuildResult.Failure(diagnostics, request.Script, exception, items);
    }

    private sealed record PreparedExecutionBatchItem(
        ExecutionBatchCompilationRequest Request,
        BuildItems Items);

    private sealed class NoopConsumerScope : IDisposable
    {
        internal static NoopConsumerScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class BatchPreparationException : Exception
    {
        public BatchPreparationException(string message, IReadOnlyList<ParserDiagnostic> diagnostics)
            : base(message)
        {
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<ParserDiagnostic> Diagnostics { get; }
    }
}
