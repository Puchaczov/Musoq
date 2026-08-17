using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DataSourceLifecycleDiagnosticTests
{
    [TestMethod]
    public void OpenSchema_WhenProviderThrows_ShouldUseSafeOpenDiagnostic()
    {
        var original = new InvalidOperationException("secret-source-argument");
        var provider = new ThrowingProvider(original);

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
            DataSourceLifecycle.OpenSchema(provider, "#fault", "items", "rows", "ctx-1"));

        Assert.AreEqual(DiagnosticCode.MQ7010_DataSourceOpenFailed, exception.Code);
        Assert.AreSame(original, exception.InnerException);
        Assert.DoesNotContain("secret-source-argument", exception.Message);
        Assert.AreEqual("#fault", exception.SchemaName);
        Assert.AreEqual("items", exception.SourceName);
        Assert.AreEqual("rows", exception.Alias);
        Assert.AreEqual("ctx-1", exception.SourceContextId);
        Assert.AreEqual("open", exception.Operation);

        var diagnostic = exception.ToDiagnostic();
        Assert.AreEqual(DiagnosticPhase.DataSource, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, diagnostic.SourceKind);
        Assert.IsFalse(diagnostic.Location.IsValid);
        Assert.AreEqual("#fault", diagnostic.Arguments["schema"]);
        Assert.IsFalse(diagnostic.Arguments.ContainsKey("arguments"));
    }

    [TestMethod]
    public void Read_WhenMoveNextThrows_ShouldUseReadDiagnosticAndPreserveInnerException()
    {
        var original = new IOException("secret-read-argument");
        var source = new TestRowSource(new ThrowingEnumerable(original));

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
        {
            foreach (var _ in DataSourceLifecycle.Read(source, "#fault", "items", "rows", "ctx-2"))
            {
            }
        });

        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, exception.Code);
        Assert.AreSame(original, exception.InnerException);
        Assert.AreEqual("read", exception.Operation);
    }

    [TestMethod]
    public void Read_WhenDisposeThrows_ShouldUseCleanupDiagnostic()
    {
        var original = new IOException("secret-cleanup-argument");
        var source = new TestRowSource(new DisposeThrowingEnumerable(original));

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
        {
            foreach (var _ in DataSourceLifecycle.Read(source, "#fault", "items", "rows", "ctx-3"))
            {
            }
        });

        Assert.AreEqual(DiagnosticCode.MQ7012_DataSourceCleanupFailed, exception.Code);
        Assert.AreSame(original, exception.InnerException);
        Assert.AreEqual("cleanup", exception.Operation);
    }

    [TestMethod]
    public void Read_WhenCancelled_ShouldPreserveCancellation()
    {
        var cancellation = new OperationCanceledException("cancelled");
        var source = new TestRowSource(new ThrowingEnumerable(cancellation));

        var exception = Assert.Throws<OperationCanceledException>(() =>
        {
            foreach (var _ in DataSourceLifecycle.Read(source, "#fault", "items", "rows", "ctx-4"))
            {
            }
        });

        Assert.AreSame(cancellation, exception);
    }

    [TestMethod]
    public async Task ReadAsync_WhenMoveNextThrows_ShouldUseReadDiagnostic()
    {
        var original = new IOException("secret-async-argument");

        var exception = await AssertThrowsAsync<DataSourceLifecycleException>(async () =>
        {
            await foreach (var _ in DataSourceLifecycle.ReadAsync(
                               new ThrowingAsyncEnumerable(original),
                               "#fault",
                               "items",
                               "rows",
                               "ctx-5"))
            {
            }
        });

        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, exception.Code);
        Assert.AreSame(original, exception.InnerException);
    }

    [TestMethod]
    public void QueryExecutionException_ShouldExposeSafeDatasourceEnvelope()
    {
        var original = new IOException("secret-source-argument");
        var lifecycle = DataSourceLifecycleException.ForRead("#fault", "items", "rows", "ctx-6", original);

        var exception = QueryExecutionException.ForDataSourceFailure(lifecycle);

        Assert.IsNotNull(exception.Envelope);
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, exception.Envelope.Code);
        Assert.AreEqual(DiagnosticPhase.DataSource, exception.Envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, exception.Envelope.SourceKind);
        Assert.AreSame(lifecycle, exception.InnerException);
        Assert.DoesNotContain("secret-source-argument", exception.Message);
        Assert.AreEqual("ctx-6", exception.Envelope.Arguments["sourceContextId"]);
    }

    [TestMethod]
    public void CompiledQuery_WhenSourceOpenFails_ShouldExposeSourceContext()
    {
        var original = new InvalidOperationException("secret-open-argument");
        var provider = CreateCompiledFaultProvider(FaultMode.Open, original);

        var exception = Assert.Throws<QueryExecutionException>(() => Materialize(provider));

        AssertLifecycleEnvelope(exception, DiagnosticCode.MQ7010_DataSourceOpenFailed, "open", original);
    }

    [TestMethod]
    public void CompiledQuery_WhenSourceReadFails_ShouldExposeSourceContext()
    {
        var original = new IOException("secret-read-argument");
        var provider = CreateCompiledFaultProvider(FaultMode.Read, original);

        var exception = Assert.Throws<QueryExecutionException>(() => Materialize(provider));

        AssertLifecycleEnvelope(exception, DiagnosticCode.MQ7011_DataSourceReadFailed, "read", original);
    }

    [TestMethod]
    public void CompiledQuery_WhenSourceCleanupFails_ShouldExposeSourceContext()
    {
        var original = new IOException("secret-cleanup-argument");
        var provider = CreateCompiledFaultProvider(FaultMode.Cleanup, original);

        var exception = Assert.Throws<QueryExecutionException>(() => Materialize(provider));

        AssertLifecycleEnvelope(exception, DiagnosticCode.MQ7012_DataSourceCleanupFailed, "cleanup", original);
    }

    private static FaultProvider CreateCompiledFaultProvider(FaultMode mode, Exception exception)
    {
        var provider = new FaultProvider();
        var compiled = InstanceCreator.CompileForExecution(
            "select Name from #test.entities() e",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        provider.Mode = mode;
        provider.Exception = exception;
        provider.CompiledQuery = compiled;
        return provider;
    }

    private static void Materialize(FaultProvider provider)
    {
        using var compiled = provider.CompiledQuery!;
        _ = TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    private static void AssertLifecycleEnvelope(
        QueryExecutionException exception,
        DiagnosticCode code,
        string operation,
        Exception original)
    {
        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a datasource envelope.");
        Assert.AreEqual(code, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.DataSource, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, envelope.SourceKind);
        Assert.AreEqual("#test", envelope.Arguments["schema"]);
        Assert.AreEqual("entities", envelope.Arguments["source"]);
        Assert.AreEqual("e", envelope.Arguments["alias"]);
        Assert.AreEqual(operation, envelope.Arguments["operation"]);
        Assert.IsTrue(envelope.Arguments.TryGetValue("sourceContextId", out var sourceContextId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(sourceContextId));
        Assert.IsFalse(envelope.Arguments.ContainsKey("parameters"));
        Assert.DoesNotContain(original.Message, exception.Message);
        var preserved = exception.InnerException?.InnerException ??
                        throw new AssertFailedException("The original datasource exception was not preserved.");
        Assert.AreSame(original, preserved);
    }

    private enum FaultMode
    {
        None,
        Open,
        Read,
        Cleanup
    }

    private sealed class FaultProvider : ISchemaProvider
    {
        private readonly FaultSchema _schema = new();

        public FaultMode Mode { get; set; }

        public Exception Exception { get; set; } = new InvalidOperationException();

        public CompiledQuery? CompiledQuery { get; set; }

        public ISchema GetSchema(string schema)
        {
            Assert.AreEqual("#test", schema);
            _schema.Mode = Mode;
            _schema.Exception = Exception;
            return _schema;
        }
    }

    private sealed class FaultSchema : GenericSchema<BasicEntity, BasicEntityTable>
    {
        public FaultSchema()
            : base([], BasicEntity.TestNameToIndexMap, BasicEntity.TestIndexToObjectAccessMap)
        {
        }

        public FaultMode Mode { get; set; }

        public Exception Exception { get; set; } = new InvalidOperationException();

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return Mode switch
            {
                FaultMode.Open => throw Exception,
                FaultMode.Read => EnsureSourceType<T, BasicEntity>(
                    name,
                    new EntityFaultRowSource(new ThrowingEntityEnumerable(Exception))),
                FaultMode.Cleanup => EnsureSourceType<T, BasicEntity>(
                    name,
                    new EntityFaultRowSource(new CleanupEntityEnumerable(Exception))),
                _ => base.GetRowSource<T>(name, executionContext, parameters)
            };
        }
    }

    private sealed class EntityFaultRowSource(
        IEnumerable<IReadOnlyList<BasicEntity>> chunks) : RowSource<BasicEntity>
    {
        public override IEnumerable<IReadOnlyList<BasicEntity>> Chunks => chunks;
    }

    private sealed class ThrowingEntityEnumerable(Exception exception) : IEnumerable<IReadOnlyList<BasicEntity>>
    {
        public IEnumerator<IReadOnlyList<BasicEntity>> GetEnumerator() => new ThrowingEntityEnumerator(exception);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CleanupEntityEnumerable(Exception exception) : IEnumerable<IReadOnlyList<BasicEntity>>
    {
        public IEnumerator<IReadOnlyList<BasicEntity>> GetEnumerator() => new CleanupEntityEnumerator(exception);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ThrowingEntityEnumerator(Exception exception) : IEnumerator<IReadOnlyList<BasicEntity>>
    {
        public IReadOnlyList<BasicEntity> Current => [];
        object IEnumerator.Current => Current;

        public bool MoveNext() => throw exception;
        public void Reset() => throw new NotSupportedException();
        public void Dispose()
        {
        }
    }

    private sealed class CleanupEntityEnumerator(Exception exception) : IEnumerator<IReadOnlyList<BasicEntity>>
    {
        public IReadOnlyList<BasicEntity> Current => [];
        object IEnumerator.Current => Current;

        public bool MoveNext() => false;
        public void Reset() => throw new NotSupportedException();
        public void Dispose() => throw exception;
    }

    private sealed class ThrowingProvider(Exception exception) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw exception;
        }
    }

    private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected {typeof(TException).Name}.");
        return null!;
    }

    private sealed class TestRowSource(IEnumerable<IReadOnlyList<int>> chunks) : RowSource<int>
    {
        public override IEnumerable<IReadOnlyList<int>> Chunks => chunks;
    }

    private sealed class ThrowingEnumerable(Exception exception) : IEnumerable<IReadOnlyList<int>>
    {
        public IEnumerator<IReadOnlyList<int>> GetEnumerator() => new ThrowingEnumerator(exception);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DisposeThrowingEnumerable(Exception exception) : IEnumerable<IReadOnlyList<int>>
    {
        public IEnumerator<IReadOnlyList<int>> GetEnumerator() => new DisposeThrowingEnumerator(exception);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ThrowingEnumerator(Exception exception) : IEnumerator<IReadOnlyList<int>>
    {
        public IReadOnlyList<int> Current => [];
        object IEnumerator.Current => Current;

        public bool MoveNext() => throw exception;
        public void Reset() => throw new NotSupportedException();
        public void Dispose()
        {
        }
    }

    private sealed class DisposeThrowingEnumerator(Exception exception) : IEnumerator<IReadOnlyList<int>>
    {
        public IReadOnlyList<int> Current => [];
        object IEnumerator.Current => Current;

        public bool MoveNext() => false;
        public void Reset() => throw new NotSupportedException();
        public void Dispose() => throw exception;
    }

    private sealed class ThrowingAsyncEnumerable(Exception exception) : IAsyncEnumerable<IReadOnlyList<int>>
    {
        public IAsyncEnumerator<IReadOnlyList<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ThrowingAsyncEnumerator(exception);
        }
    }

    private sealed class ThrowingAsyncEnumerator(Exception exception) : IAsyncEnumerator<IReadOnlyList<int>>
    {
        public IReadOnlyList<int> Current => [];

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromException<bool>(exception);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
