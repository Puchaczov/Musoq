using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

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
    public void OpenSchema_WhenProviderRejectsInput_ShouldPreserveTypedInputDiagnostic()
    {
        var original = new InvalidDataException("private path and source payload");
        var inputFailure = new DataSourceInputException(
            DataSourceInputFailureReason.MalformedJson,
            new DataSourceInputLocation(line: 2, column: 4, characterOffset: 13),
            original);

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
            DataSourceLifecycle.OpenSchema(new ThrowingProvider(inputFailure), "#fault", "items", "rows", "ctx-open-input"));
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, exception.Code);
        Assert.AreEqual("malformed-json", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(2L, diagnostic.InputLocation?.Line);
        Assert.AreEqual(13L, diagnostic.InputLocation?.CharacterOffset);
        Assert.AreSame(inputFailure, exception.InnerException);
        Assert.AreSame(original, inputFailure.InnerException);
        Assert.DoesNotContain("private path", diagnostic.Message);
        Assert.DoesNotContain("source payload", diagnostic.Message);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenMetadataRejectsInput_ShouldReportSafeRuntimeDiagnostic()
    {
        var inputFailure = new DataSourceInputException(
            DataSourceInputFailureReason.UnterminatedQuotedField,
            new DataSourceInputLocation(byteOffset: 23),
            new InvalidDataException("private CSV path and payload"));

        var result = InstanceCreator.CompileWithDiagnostics(
            "select Name from #fault.items() i",
            "metadata-input-diagnostic",
            new ThrowingProvider(inputFailure),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsFalse(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        var diagnostic = result.Errors.Single();
        Assert.AreEqual(
            DiagnosticCode.MQ7014_MalformedRuntimeInput,
            diagnostic.Code,
            string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, diagnostic.SourceKind);
        Assert.AreEqual("unterminated-quoted-field", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(23L, diagnostic.InputLocation?.ByteOffset);
        Assert.DoesNotContain("private CSV path", diagnostic.Message);
        Assert.DoesNotContain("payload", diagnostic.Message);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceMetadataRejectsInput_ShouldReportSafeRuntimeDiagnostic()
    {
        var inputFailure = new DataSourceInputException(
            DataSourceInputFailureReason.UnterminatedQuotedField,
            new DataSourceInputLocation(byteOffset: 23),
            new InvalidDataException("private CSV path and unterminated payload"));
        var provider = new FaultProvider
        {
            Mode = FaultMode.Metadata,
            Exception = inputFailure
        };

        var result = InstanceCreator.CompileWithDiagnostics(
            "select Name from #test.entities() e",
            "source-metadata-input-diagnostic",
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsFalse(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        var diagnostic = result.Errors.Single();
        Assert.AreEqual(
            DiagnosticCode.MQ7014_MalformedRuntimeInput,
            diagnostic.Code,
            string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, diagnostic.SourceKind);
        Assert.AreEqual("unterminated-quoted-field", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(23L, diagnostic.InputLocation?.ByteOffset);
        Assert.DoesNotContain("private CSV path", diagnostic.Message);
        Assert.DoesNotContain("unterminated payload", diagnostic.Message);
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
    public void Read_WhenInvalidDataIsRejected_ShouldUseGenericMalformedInputDiagnostic()
    {
        var original = new InvalidDataException("private path and record payload");
        var source = new TestRowSource(new ThrowingEnumerable(original));

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
        {
            foreach (var _ in DataSourceLifecycle.Read(source, "#fault", "items", "rows", "ctx-invalid-sync"))
            {
            }
        });
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, exception.Code);
        Assert.AreSame(original, exception.InnerException);
        Assert.AreEqual(RuntimeInputFailureReason.MalformedInput, exception.InputReason);
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, diagnostic.SourceKind);
        Assert.AreEqual("ctx-invalid-sync", diagnostic.Arguments["sourceContextId"]);
        Assert.IsNull(diagnostic.InputLocation);
        Assert.DoesNotContain("private path", exception.Message);
        Assert.DoesNotContain("record payload", exception.Message);
    }

    [TestMethod]
    public void Read_WhenSchemaInputFailureIsRaised_ShouldPreserveReasonAndLocation()
    {
        var inputFailure = new DataSourceInputException(
            DataSourceInputFailureReason.UnterminatedQuotedField,
            new DataSourceInputLocation(byteOffset: 23),
            new InvalidDataException("private CSV path and contents"));
        var source = new TestRowSource(new ThrowingEnumerable(new IOException("wrapped provider failure", inputFailure)));

        var exception = Assert.Throws<DataSourceLifecycleException>(() =>
        {
            foreach (var _ in DataSourceLifecycle.Read(source, "#fault", "items", "rows", "ctx-schema-input-sync"))
            {
            }
        });
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, exception.Code);
        Assert.AreEqual("unterminated-quoted-field", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(23L, diagnostic.InputLocation?.ByteOffset);
        Assert.DoesNotContain("private CSV path", diagnostic.Message);
        Assert.DoesNotContain("contents", diagnostic.Message);
    }

    [TestMethod]
    public async Task ReadAsync_WhenTypedInputFailureIsRaised_ShouldPreserveReasonAndByteOffset()
    {
        var typedFailure = new RuntimeInputDiagnosticException(
            RuntimeInputFailureReason.UnterminatedQuotedField,
            DiagnosticSourceKind.DataSource,
            new DiagnosticInputLocation(byteOffset: 23),
            new InvalidDataException("private CSV path and contents"));

        var exception = await AssertThrowsAsync<DataSourceLifecycleException>(async () =>
        {
            await foreach (var _ in DataSourceLifecycle.ReadAsync(
                               new ThrowingAsyncEnumerable(typedFailure),
                               "#fault",
                               "items",
                               "rows",
                               "ctx-invalid-async"))
            {
            }
        });
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, exception.Code);
        Assert.AreEqual("The final quoted field is not terminated.", exception.Message);
        Assert.AreEqual("ctx-invalid-async", diagnostic.Arguments["sourceContextId"]);
        Assert.AreEqual("unterminated-quoted-field", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(23L, diagnostic.InputLocation?.ByteOffset);
        Assert.DoesNotContain("private CSV path", diagnostic.Message);
        Assert.DoesNotContain("contents", diagnostic.Message);
    }

    [TestMethod]
    public async Task ReadAsync_WhenSchemaInputFailureIsRaised_ShouldPreserveReasonAndLocation()
    {
        var inputFailure = new DataSourceInputException(
            DataSourceInputFailureReason.MalformedJson,
            new DataSourceInputLocation(line: 4, column: 2, characterOffset: 18),
            new InvalidDataException("private JSON path and payload"));
        var exception = await AssertThrowsAsync<DataSourceLifecycleException>(async () =>
        {
            await foreach (var _ in DataSourceLifecycle.ReadAsync(
                               new ThrowingAsyncEnumerable(new IOException("wrapped read failure", inputFailure)),
                               "#fault",
                               "items",
                               "rows",
                               "ctx-schema-input-async"))
            {
            }
        });
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, exception.Code);
        Assert.AreEqual("malformed-json", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(4L, diagnostic.InputLocation?.Line);
        Assert.AreEqual(18L, diagnostic.InputLocation?.CharacterOffset);
        Assert.DoesNotContain("private JSON path", diagnostic.Message);
        Assert.DoesNotContain("payload", diagnostic.Message);
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
        Assert.IsNull(exception.Envelope.Offset);
        Assert.IsNull(exception.Envelope.EndOffset);
        Assert.IsNull(exception.Envelope.Snippet);
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
    public void CompiledQuery_WhenDeferredSourceMaterializationFindsInvalidData_ShouldExposeMalformedInputEnvelope()
    {
        var original = new InvalidDataException("private source path and record data");
        var provider = CreateCompiledFaultProvider(FaultMode.Read, original);

        var exception = Assert.Throws<QueryExecutionException>(() => Materialize(provider));
        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a datasource envelope.");

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, envelope.SourceKind);
        Assert.AreEqual("#test", envelope.Arguments["schema"]);
        Assert.AreEqual("entities", envelope.Arguments["source"]);
        Assert.AreEqual("e", envelope.Arguments["alias"]);
        Assert.AreEqual("read", envelope.Arguments["operation"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Arguments["sourceContextId"]));
        Assert.IsNull(envelope.InputLocation);
        Assert.IsNull(envelope.Details);
        Assert.DoesNotContain("private source path", exception.Message);
        Assert.DoesNotContain("record data", exception.Message);
        Assert.AreSame(original, exception.InnerException?.InnerException);
    }

    [TestMethod]
    public void CompiledQuery_WhenSourceCleanupFails_ShouldExposeSourceContext()
    {
        var original = new IOException("secret-cleanup-argument");
        var provider = CreateCompiledFaultProvider(FaultMode.Cleanup, original);

        var exception = Assert.Throws<QueryExecutionException>(() => Materialize(provider));

        AssertLifecycleEnvelope(exception, DiagnosticCode.MQ7012_DataSourceCleanupFailed, "cleanup", original);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryScopedRowsAreAdvertisedWithoutImplementation_ShouldReportContractError()
    {
        const string query = """
            param(path: string = 'orders.csv')
            select c.Customer as Customer, Count(*) as Orders, Sum(c.Amount::int) as Total
            from #separatedvalues.comma($path, true, 0) c
            group by c.Customer order by c.Customer
            """;

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            "query-scoped-capability-mismatch",
            new QueryScopedCapabilityMismatchSchemaProvider(),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsFalse(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        var sourceContractErrors = result.Errors
            .Where(item => item.Code == DiagnosticCode.MQ3071_SourceContractError)
            .ToArray();
        Assert.AreEqual(1, sourceContractErrors.Length, string.Join(Environment.NewLine, result.Diagnostics));
        var error = sourceContractErrors[0];
        Assert.Contains("origin=DescribeSource", error.Message);
        Assert.Contains("sourceCode=QueryScopedRowsMissingRuntimeImplementation", error.Message);
        Assert.Contains("QueryScopedRows", error.Message);
        Assert.Contains("IQueryScopedRowSourceSchema", error.Message);
        Assert.IsTrue(result.Warnings.Any(item =>
            item.Code == DiagnosticCode.MQ5013_SourceContractWarning &&
            item.Message.Contains("sourceCode=ExistingSourceDiagnostic", StringComparison.Ordinal)));
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
        Metadata,
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

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (Mode == FaultMode.Metadata)
                throw Exception;

            return base.GetTableByName(name, metadataContext, parameters);
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments)
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
                _ => base.GetRowSource<T, TArguments>(name, executionContext, nativeArguments)
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

    private sealed class QueryScopedCapabilityMismatchSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new QueryScopedCapabilityMismatchSchema();
        }
    }

    private sealed class QueryScopedCapabilityMismatchSchema : SchemaBase
    {
        public QueryScopedCapabilityMismatchSchema()
            : base("separatedvalues", CreateLibrary())
        {
            AddTable<OrderCsvTable>("comma");
            AddSource<OrderCsvRowSource>("comma");
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            var descriptor = base.DescribeSource(name, context, parameters);
            return descriptor with
            {
                TransferCapabilities = SourceTransferCapabilities.QueryScopedRows,
                ContractDiagnostics =
                [
                    .. descriptor.ContractDiagnostics,
                    SourceContractDiagnostic.Warning("Existing datasource warning.", "ExistingSourceDiagnostic")
                ]
            };
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methods);
        }
    }

    private sealed class OrderCsvTable(string path, bool hasHeader, int skipRows) : ISchemaTable
    {
        public (string Path, bool HasHeader, int SkipRows) SourceArguments { get; } = (path, hasHeader, skipRows);

        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn(nameof(OrderCsvRow.Customer), 0, typeof(string)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(OrderCsvRow.Amount), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(OrderCsvRow));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
    }

    public sealed record OrderCsvRow(string Customer, string Amount);

    private sealed class OrderCsvRowSource(string path, bool hasHeader, int skipRows) : RowSource<OrderCsvRow>
    {
        public (string Path, bool HasHeader, int SkipRows) SourceArguments { get; } = (path, hasHeader, skipRows);

        public override IEnumerable<IReadOnlyList<OrderCsvRow>> Chunks => [];
    }
}
