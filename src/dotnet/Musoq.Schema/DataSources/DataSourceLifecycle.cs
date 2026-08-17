using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using System.Reflection;

namespace Musoq.Schema.DataSources;

/// <summary>
/// Keeps provider construction and enumeration failures at typed datasource
/// boundaries. Query arguments are deliberately not part of the boundary data.
/// </summary>
public static class DataSourceLifecycle
{
    public static ISchemaProvider WrapProvider(ISchemaProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider is LifecycleProvider ? provider : new LifecycleProvider(provider);
    }

    public static ISchema OpenSchema(
        ISchemaProvider provider,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId)
    {
        ArgumentNullException.ThrowIfNull(provider);

        try
        {
            var schema = provider.GetSchema(schemaName) ??
                         throw new InvalidOperationException($"Schema '{schemaName}' returned null.");
            return new LifecycleSchema(schema, schemaName, alias, sourceContextId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DataSourceLifecycleException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DataSourceLifecycleException.ForOpen(
                schemaName,
                sourceName,
                alias,
                sourceContextId,
                exception);
        }
    }

    public static RowSource<T> OpenRowSource<T>(
        ISchema schema,
        string sourceName,
        SourceExecutionContext executionContext,
        object?[] parameters,
        string schemaName,
        string alias,
        string sourceContextId)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(parameters);

        try
        {
            return schema.GetRowSource<T>(sourceName, executionContext, parameters) ??
                   throw new InvalidOperationException($"Source '{sourceName}' returned null.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DataSourceLifecycleException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DataSourceLifecycleException.ForOpen(
                schemaName,
                sourceName,
                alias,
                sourceContextId,
                exception);
        }
    }

    public static IEnumerable<IReadOnlyList<T>> Read<T>(
        RowSource<T> source,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId)
    {
        ArgumentNullException.ThrowIfNull(source);

        IEnumerable<IReadOnlyList<T>> chunks;
        try
        {
            chunks = source.Chunks ??
                     throw new InvalidOperationException($"Source '{sourceName}' returned null chunks.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DataSourceLifecycleException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw DataSourceLifecycleException.ForRead(
                schemaName,
                sourceName,
                alias,
                sourceContextId,
                exception);
        }

        return ReadCore(chunks, schemaName, sourceName, alias, sourceContextId);
    }

    public static IAsyncEnumerable<IReadOnlyList<T>> ReadAsync<T>(
        IAsyncEnumerable<IReadOnlyList<T>> source,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ReadAsyncCore(source, schemaName, sourceName, alias, sourceContextId, cancellationToken);
    }

    private static IEnumerable<IReadOnlyList<T>> ReadCore<T>(
        IEnumerable<IReadOnlyList<T>> chunks,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId)
    {
        IEnumerator<IReadOnlyList<T>>? enumerator = null;
        Exception? primaryFailure = null;
        try
        {
            try
            {
                enumerator = chunks.GetEnumerator();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
                throw DataSourceLifecycleException.ForRead(
                    schemaName,
                    sourceName,
                    alias,
                    sourceContextId,
                    exception);
            }

            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch (OperationCanceledException exception)
                {
                    primaryFailure = exception;
                    throw;
                }
                catch (Exception exception)
                {
                    primaryFailure = exception;
                    throw DataSourceLifecycleException.ForRead(
                        schemaName,
                        sourceName,
                        alias,
                        sourceContextId,
                        exception);
                }

                if (!hasNext)
                    yield break;

                IReadOnlyList<T> current;
                try
                {
                    current = enumerator.Current;
                }
                catch (OperationCanceledException exception)
                {
                    primaryFailure = exception;
                    throw;
                }
                catch (Exception exception)
                {
                    primaryFailure = exception;
                    throw DataSourceLifecycleException.ForRead(
                        schemaName,
                        sourceName,
                        alias,
                        sourceContextId,
                        exception);
                }

                yield return current;
            }
        }
        finally
        {
            if (enumerator != null)
            {
                try
                {
                    enumerator.Dispose();
                }
                catch (OperationCanceledException)
                {
                    if (primaryFailure == null)
                        throw;
                }
                catch (Exception exception)
                {
                    if (primaryFailure == null)
                    {
                        throw DataSourceLifecycleException.ForCleanup(
                            schemaName,
                            sourceName,
                            alias,
                            sourceContextId,
                            exception);
                    }
                }
            }
        }
    }

    private static async IAsyncEnumerable<IReadOnlyList<T>> ReadAsyncCore<T>(
        IAsyncEnumerable<IReadOnlyList<T>> chunks,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = chunks.GetAsyncEnumerator(cancellationToken);
        Exception? primaryFailure = null;
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    primaryFailure = exception;
                    throw;
                }
                catch (Exception exception)
                {
                    primaryFailure = exception;
                    throw DataSourceLifecycleException.ForRead(
                        schemaName,
                        sourceName,
                        alias,
                        sourceContextId,
                        exception);
                }

                if (!hasNext)
                    yield break;

                IReadOnlyList<T> current;
                try
                {
                    current = enumerator.Current;
                }
                catch (OperationCanceledException exception)
                {
                    primaryFailure = exception;
                    throw;
                }
                catch (Exception exception)
                {
                    primaryFailure = exception;
                    throw DataSourceLifecycleException.ForRead(
                        schemaName,
                        sourceName,
                        alias,
                        sourceContextId,
                        exception);
                }

                yield return current;
            }
        }
        finally
        {
            try
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (primaryFailure == null)
                    throw;
            }
            catch (Exception exception)
            {
                if (primaryFailure == null)
                {
                    throw DataSourceLifecycleException.ForCleanup(
                        schemaName,
                        sourceName,
                        alias,
                        sourceContextId,
                        exception);
                }
            }
        }
    }

    private sealed class LifecycleSchema(
        ISchema inner,
        string schemaName,
        string alias,
        string sourceContextId) : ISchema
    {
        public string Name => inner.Name;

        public ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters) =>
            inner.GetTableByName(name, metadataContext, parameters);

        public SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters) =>
            inner.DescribeSource(name, context, parameters);

        public IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters) =>
            inner.DescribeSourceRuntimeSettings(name, context, parameters);

        public SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters) =>
            inner.TryPlanSource(name, request, parameters);

        public RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            var identity = executionContext.Plan.Identity;
            var effectiveSchemaName = string.IsNullOrEmpty(identity.SchemaName) ? schemaName : identity.SchemaName;
            var effectiveSourceName = string.IsNullOrEmpty(identity.MethodName) ? name : identity.MethodName;
            var effectiveAlias = string.IsNullOrEmpty(identity.Alias) ? alias : identity.Alias;
            var effectiveContextId = string.IsNullOrEmpty(identity.SourceContextId)
                ? sourceContextId
                : identity.SourceContextId;
            var source = OpenRowSource<T>(
                inner,
                name,
                executionContext,
                parameters,
                effectiveSchemaName,
                effectiveAlias,
                effectiveContextId);
            return new LifecycleRowSource<T>(
                source,
                effectiveSchemaName,
                effectiveSourceName,
                effectiveAlias,
                effectiveContextId);
        }

        public SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) =>
            inner.GetRawConstructors(metadataContext);

        public SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) =>
            inner.GetRawConstructors(methodName, metadataContext);

        public bool TryResolveMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo) =>
            inner.TryResolveMethod(method, parameters, entityType, out methodInfo);

        public bool TryResolveRawMethod(string method, Type[] parameters, [NotNullWhen(true)] out MethodInfo? methodInfo) =>
            inner.TryResolveRawMethod(method, parameters, out methodInfo);

        public bool TryResolveAggregationMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo) =>
            inner.TryResolveAggregationMethod(method, parameters, entityType, out methodInfo);

        public bool TryResolveAggregationMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            Func<MethodInfo, bool> methodFilter,
            [NotNullWhen(true)] out MethodInfo? methodInfo) =>
            inner.TryResolveAggregationMethod(method, parameters, entityType, methodFilter, out methodInfo);

        public bool TryResolveWindowFunction(string method, [NotNullWhen(true)] out MethodInfo? methodInfo) =>
            inner.TryResolveWindowFunction(method, out methodInfo);

        public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllLibraryMethods() =>
            inner.GetAllLibraryMethods();
    }

    private sealed class LifecycleRowSource<T>(
        RowSource<T> inner,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId) : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks =>
            Read(inner, schemaName, sourceName, alias, sourceContextId);
    }

    private sealed class LifecycleProvider(ISchemaProvider inner) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return OpenSchema(inner, schema, string.Empty, string.Empty, string.Empty);
        }
    }
}
