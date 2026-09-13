using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Interpreters;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
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
        var session = new ControlledSession();
        var provider = new ControlledProvider(session);
        using var compiled = CompileGeneratedQuery(
            $"select r.Id from #rec133.items() r /* {caseId}-fault */",
            $"REC133_{caseId}_{Guid.NewGuid():N}",
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var cancellation = new CancellationTokenSource();

        var run = Task.Run(() => compiled.Run(cancellation.Token).Count);
        Assert.IsTrue(session.ProducerStarted.Wait(HarnessBudget),
            $"HARNESS_TIMEOUT producer did not start for {caseId}.");

        cancellation.Cancel();

        try
        {
            await run.WaitAsync(HarnessBudget);
            Assert.Fail($"{caseId} completed normally after cancellation.");
        }
        catch (OperationCanceledException exception)
        {
            Assert.IsTrue(exception.CancellationToken.IsCancellationRequested, caseId);
        }
        finally
        {
            session.ReleaseProducer.Set();
        }

        Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
            $"HARNESS_TIMEOUT producer cleanup did not finish for {caseId}.");
    }

    private async Task AssertZeroProgressIsBounded(string caseId)
    {
        var interpreter = CreateZeroProgressInterpreter(caseId, out var data);
        var invocation = Task.Run(() => InvokeInterpret(interpreter, data));

        try
        {
            await invocation.WaitAsync(HarnessBudget);
            Assert.Fail($"{caseId} unexpectedly completed a zero-progress repetition.");
        }
        catch (TargetInvocationException wrapper)
        {
            Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException, caseId);
            var exception = (BinaryParseException)wrapper.InnerException!;
            Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode, caseId);
            Assert.AreEqual("ISE0009", exception.FormattedErrorCode, caseId);
            StringAssert.Contains(exception.Details, caseId is "ZP-01" or "ZP-02" or "ZP-03" or "ZP-04" or
                "ZP-05" or "ZP-06" or "ZP-07" or "ZP-08"
                ? "made no progress"
                : "maximum of 10000 iterations", caseId);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"HARNESS_TIMEOUT zero-progress reproduction did not terminate for {caseId}.");
        }
    }

    private static object CreateZeroProgressInterpreter(string caseId, out byte[] data)
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
            data = [0xAA, 0xBB];
            return CompileInterpreter(registry, $"ZeroProgress{suffix}");
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
            data = caseId is "ZP-05" or "ZP-06" ? [0x01] : [0x01, 0x02, 0x03];
            return CompileInterpreter(registry, outerName);
        }

        var limitedRepeat = new RepeatUntilTypeNode(
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            new BooleanNode(false),
            $"Values{suffix}");
        var limitedName = $"Limit{suffix}";
        registry.Register(limitedName, new BinarySchemaNode(
            limitedName,
            [new FieldDefinitionNode($"Values{suffix}", limitedRepeat)]));
        data = new byte[10_001];
        return CompileInterpreter(registry, limitedName);
    }

    private static object CompileInterpreter(SchemaRegistry registry, string schemaName)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        using var compilationUnit = new InterpreterCompilationUnit(
            $"REC133_{Guid.NewGuid():N}",
            code);
        if (!compilationUnit.Compile())
            throw new InvalidOperationException(
                $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");

        var type = compilationUnit.GetInterpreterType(schemaName) ??
                   throw new InvalidOperationException($"Interpreter '{schemaName}' was not generated.");
        return Activator.CreateInstance(type)!;
    }

    private static object InvokeInterpret(object interpreter, byte[] data)
    {
        var method = interpreter.GetType().GetMethod("Interpret", [typeof(byte[])]) ??
                     throw new InvalidOperationException("Interpret(byte[]) method was not generated.");
        return method.Invoke(interpreter, [data])!;
    }

    private static async Task AssertCleanupBoundary(string caseId)
    {
        var session = new ControlledSession();
        var source = new ControlledSource(
            CreateSourceContext(),
            session,
            finite: caseId is "CL-09" or "CL-10" or "CL-11" or "CL-12",
            waitForCancellation: true);

        if (caseId is "CL-09" or "CL-10" or "CL-11" or "CL-12")
        {
            var rows = await Task.Run(() => source.Chunks.SelectMany(static chunk => chunk).ToArray())
                .WaitAsync(HarnessBudget);
            Assert.HasCount(2, rows, caseId);
            Assert.IsFalse(session.CancellationObserved.IsSet, caseId);
            Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
                $"HARNESS_TIMEOUT finite producer did not finish for {caseId}.");
            return;
        }

        await Task.Run(() =>
        {
            using var enumerator = source.Chunks.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext(), caseId);
            Assert.AreEqual(1, enumerator.Current[0].Id, caseId);
        }).WaitAsync(HarnessBudget);

        Assert.IsTrue(session.CancellationObserved.Wait(HarnessBudget),
            $"HARNESS_TIMEOUT cleanup cancellation was not observed for {caseId}.");
        Assert.IsTrue(session.ProducerExited.Wait(HarnessBudget),
            $"HARNESS_TIMEOUT producer cleanup did not finish for {caseId}.");
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

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, BasicEntity>(
                name,
                new ControlledSource(executionContext, session, finite: false, waitForCancellation: false));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class ControlledSession
    {
        public ManualResetEventSlim ProducerStarted { get; } = new();
        public ManualResetEventSlim CancellationObserved { get; } = new();
        public ManualResetEventSlim ProducerExited { get; } = new();
        public ManualResetEventSlim ReleaseProducer { get; } = new();
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
            session.ProducerStarted.Set();
            try
            {
                writer.Write([new BasicEntity("partial") { Id = 1 }]);

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
