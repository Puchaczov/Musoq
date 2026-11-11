using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Interpreters;
using Musoq.Schema.Managers;
using BinaryParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-133 matrix. The fixture exercises the real compilation,
/// execution, interpreter and chunk-pipeline boundaries. Every asynchronous
/// wait has an explicit harness budget; a budget expiry is a failed test.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRec133CancellationAndBoundsTests : BinaryOrTextualEvaluatorTestBase
{
    private static readonly TimeSpan HarnessBudget = TimeSpan.FromSeconds(2);

    [TestMethod]
    [DataRow("CC-01")]
    [DataRow("CC-02")]
    [DataRow("CC-03")]
    [DataRow("CC-04")]
    [DataRow("CC-05")]
    [DataRow("CC-06")]
    [DataRow("CC-07")]
    [DataRow("CC-08")]
    [DataRow("CC-09")]
    [DataRow("CC-10")]
    [DataRow("CC-11")]
    [DataRow("CC-12")]
    [DataRow("EX-01")]
    [DataRow("EX-02")]
    [DataRow("EX-03")]
    [DataRow("EX-04")]
    [DataRow("EX-05")]
    [DataRow("EX-06")]
    [DataRow("EX-07")]
    [DataRow("EX-08")]
    [DataRow("EX-09")]
    [DataRow("EX-10")]
    [DataRow("EX-11")]
    [DataRow("EX-12")]
    [DataRow("ZP-01")]
    [DataRow("ZP-02")]
    [DataRow("ZP-03")]
    [DataRow("ZP-04")]
    [DataRow("ZP-05")]
    [DataRow("ZP-06")]
    [DataRow("ZP-07")]
    [DataRow("ZP-08")]
    [DataRow("ZP-09")]
    [DataRow("ZP-10")]
    [DataRow("ZP-11")]
    [DataRow("ZP-12")]
    [DataRow("CL-01")]
    [DataRow("CL-02")]
    [DataRow("CL-03")]
    [DataRow("CL-04")]
    [DataRow("CL-05")]
    [DataRow("CL-06")]
    [DataRow("CL-07")]
    [DataRow("CL-08")]
    [DataRow("CL-09")]
    [DataRow("CL-10")]
    [DataRow("CL-11")]
    [DataRow("CL-12")]
    public async Task CancellationAndBoundsMatrix_ShouldUseContractedTermination(string caseId)
    {
        if (caseId.StartsWith("CC-", StringComparison.Ordinal))
        {
            AssertPreCancelledCompilation(caseId);
            return;
        }

        if (caseId.StartsWith("EX-", StringComparison.Ordinal))
        {
            await AssertPartialExecutionCancellation(caseId);
            return;
        }

        if (caseId.StartsWith("ZP-", StringComparison.Ordinal))
        {
            await AssertZeroProgressIsBounded(caseId);
            return;
        }

        if (caseId.StartsWith("CL-", StringComparison.Ordinal))
        {
            await AssertCleanupBoundary(caseId);
            return;
        }

        Assert.Fail($"Unknown REC-133 case '{caseId}'.");
    }

    private static void AssertPreCancelledCompilation(string caseId)
    {
        var provider = new NeverEnteredProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var query = $"select r.Id from #rec133.items() r /* {caseId}-fault */";
        var exception = Assert.ThrowsExactly<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
            query,
            $"REC133_{caseId}_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(),
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken, caseId);
        Assert.AreEqual(0, provider.GetSchemaCount, caseId);
    }

    private async Task AssertPartialExecutionCancellation(string caseId)
    {
        using var session = new ControlledSession();
        var provider = new ControlledProvider(session);
        using var compiled = CompileGeneratedQuery(
            $"select r.Id from #rec133.items() r /* {caseId}-fault */",
            $"REC133_{caseId}_{Guid.NewGuid():N}",
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var cancellation = new CancellationTokenSource();

        var run = Task.Run(() => ObserveCancellation(compiled, cancellation.Token));
        OperationCanceledException? observed = null;
        try
        {
            Assert.IsTrue(session.ProducerStarted.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT producer did not start for {caseId}.");
            cancellation.Cancel();
            Assert.IsTrue(session.CancellationObserved.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT producer did not observe cancellation for {caseId}.");
            observed = await run.WaitAsync(HarnessBudget);
        }
        finally
        {
            cancellation.Cancel();
            session.ReleaseProducer.Set();
            await run.WaitAsync(HarnessBudget);
            Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT producer cleanup did not finish for {caseId}.");
        }

        Assert.IsNotNull(observed, $"{caseId} completed normally after cancellation.");
        Assert.IsTrue(observed.CancellationToken.IsCancellationRequested, caseId);
        Assert.IsTrue(session.CancellationObserved.IsSet, caseId);
    }

    private static OperationCanceledException? ObserveCancellation(CompiledQuery compiled, CancellationToken token)
    {
        try
        {
            using var result = compiled.Run(token);
            _ = result.Count;
            return null;
        }
        catch (OperationCanceledException exception)
        {
            return exception;
        }
    }

    private Task AssertZeroProgressIsBounded(string caseId)
    {
        var fixture = CreateZeroProgressInterpreter(caseId);
        using var interpreter = fixture.Interpreter;
        var exception = Assert.ThrowsExactly<BinaryParseException>(() => interpreter.Interpret(fixture.Data), caseId);
        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode, caseId);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode, caseId);
        StringAssert.Contains(exception.Details, caseId is "ZP-01" or "ZP-02" or "ZP-03" or "ZP-04" or
                "ZP-05" or "ZP-06" or "ZP-07" or "ZP-08"
                ? "made no progress"
                : "maximum of 10000 iterations", caseId);
        return Task.CompletedTask;
    }

    private static (CompiledBinaryFixture Interpreter, byte[] Data) CreateZeroProgressInterpreter(string caseId)
    {
        var suffix = caseId[3..];
        var registry = new SchemaRegistry();

        if (caseId is "ZP-01" or "ZP-02" or "ZP-03" or "ZP-04")
        {
            var repeat = RepeatUntilTypeNode.EndOfInput(
                new ByteArrayTypeNode(new IntegerNode(0, new TextSpan(0, 0))),
                $"Chunks{suffix}");
            registry.Register($"ZeroProgress{suffix}", new BinarySchemaNode(
                $"ZeroProgress{suffix}",
                [new FieldDefinitionNode($"Chunks{suffix}", repeat)]));
            var data = new byte[] { 0xAA, 0xBB };
            return (CompileInterpreter(registry, $"ZeroProgress{suffix}"), data);
        }

        if (caseId is "ZP-05" or "ZP-06" or "ZP-07" or "ZP-08")
        {
            var emptyName = $"Empty{suffix}";
            registry.Register(emptyName, new BinarySchemaNode(emptyName, []));
            var repeat = RepeatUntilTypeNode.EndOfInput(
                new SchemaReferenceTypeNode(emptyName),
                $"Items{suffix}");
            var outerName = $"Outer{suffix}";
            registry.Register(outerName, new BinarySchemaNode(
                outerName,
                [new FieldDefinitionNode($"Items{suffix}", repeat)]));
            byte[] data = caseId is "ZP-05" or "ZP-06" ? [0x01] : [0x01, 0x02, 0x03];
            return (CompileInterpreter(registry, outerName), data);
        }

        var limitedRepeat = new RepeatUntilTypeNode(
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            new BooleanNode(false),
            $"Values{suffix}");
        var limitedName = $"Limit{suffix}";
        registry.Register(limitedName, new BinarySchemaNode(
            limitedName,
            [new FieldDefinitionNode($"Values{suffix}", limitedRepeat)]));
        return (CompileInterpreter(registry, limitedName), new byte[10_001]);
    }

    private static CompiledBinaryFixture CompileInterpreter(SchemaRegistry registry, string schemaName) =>
        new(new InterpreterCodeGenerator(registry).GenerateAll(), schemaName, []);

    private static async Task AssertCleanupBoundary(string caseId)
    {
        using var session = new ControlledSession();
        var source = new ControlledSource(
            CreateSourceContext(),
            session,
            finite: caseId is "CL-09" or "CL-10" or "CL-11" or "CL-12",
            waitForCancellation: true);

        if (caseId is "CL-09" or "CL-10" or "CL-11" or "CL-12")
        {
            await AssertFiniteProducer(source, session, caseId);
            return;
        }

        await AssertDisposalCancelsProducer(source, session, caseId);
    }

    private static async Task AssertFiniteProducer(ControlledSource source, ControlledSession session, string caseId)
    {
        var read = Task.Run(() => source.Chunks.SelectMany(static chunk => chunk).ToArray());
        try
        {
            var rows = await read.WaitAsync(HarnessBudget);
            CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.Id).ToArray(), caseId);
            Assert.IsFalse(session.CancellationObserved.IsSet, caseId);
        }
        finally
        {
            session.ReleaseProducer.Set();
            await read.WaitAsync(HarnessBudget);
            Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT finite producer did not finish for {caseId}.");
        }
    }

    private static async Task AssertDisposalCancelsProducer(ControlledSource source, ControlledSession session, string caseId)
    {
        var read = Task.Run(() =>
        {
            using var enumerator = source.Chunks.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext(), caseId);
            Assert.AreEqual(1, enumerator.Current[0].Id, caseId);
        });
        try
        {
            await read.WaitAsync(HarnessBudget);
            Assert.IsTrue(session.CancellationObserved.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT cleanup cancellation was not observed for {caseId}.");
        }
        finally
        {
            session.ReleaseProducer.Set();
            await read.WaitAsync(HarnessBudget);
            Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT producer cleanup did not finish for {caseId}.");
        }
    }

    private static SourceExecutionContext CreateSourceContext()
    {
        return new SourceExecutionContext(
            "rec133-query",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            new BasicEntityTable().Columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            sourceDiagnostics: SourceDiagnostics.None);
    }

    private sealed class NeverEnteredProvider : ISchemaProvider
    {
        public int GetSchemaCount { get; private set; }

        public ISchema GetSchema(string schema)
        {
            GetSchemaCount++;
            throw new InvalidOperationException($"Provider should not be entered for {schema}.");
        }
    }

    private sealed class ControlledProvider(ControlledSession session) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "#rec133", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected schema '{schema}'.");

            return new ControlledSchema(session);
        }
    }

    private sealed class ControlledSchema(ControlledSession session)
        : SchemaBase("rec133", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!string.Equals(name, "items", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected table '{name}'.");

            return new BasicEntityTable();
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments)
        {
            return EnsureSourceType<T, BasicEntity>(
                name,
                new ControlledSource(executionContext, session, finite: false, waitForCancellation: true));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class ControlledSession : IDisposable
    {
        public ManualResetEventSlim ProducerStarted { get; } = new();
        public ManualResetEventSlim CancellationObserved { get; } = new();
        public ManualResetEventSlim ProducerExited { get; } = new();
        public ManualResetEventSlim ReleaseProducer { get; } = new();

        public void Dispose()
        {
            ProducerStarted.Dispose();
            CancellationObserved.Dispose();
            ProducerExited.Dispose();
            ReleaseProducer.Dispose();
        }
    }

    private sealed class ControlledSource(
        SourceExecutionContext context,
        ControlledSession session,
        bool finite,
        bool waitForCancellation)
        : DiagnosticChunkedRowSource<BasicEntity>(
            context,
            "rec133-items",
            new DiagnosticChunkedRowSourceOptions(4))
    {
        protected override void CollectChunks(DiagnosticChunkWriter<BasicEntity> writer)
        {
            try
            {
                writer.Write([new BasicEntity("partial") { Id = 1 }]);
                session.ProducerStarted.Set();

                if (finite)
                {
                    writer.Write([new BasicEntity("complete") { Id = 2 }]);
                    return;
                }

                if (waitForCancellation)
                {
                    session.ReleaseProducer.Wait(writer.CancellationToken);
                }
                else
                {
                    session.ReleaseProducer.Wait();
                }
            }
            catch (OperationCanceledException) when (writer.CancellationToken.IsCancellationRequested)
            {
                session.CancellationObserved.Set();
                throw;
            }
            finally
            {
                session.ProducerExited.Set();
            }
        }
    }
}
