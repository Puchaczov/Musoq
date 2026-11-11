using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedParseStatusTests
{
    [TestMethod]
    [DataRow("binary Record { Tag: byte, Value: int le };", "Value", 3, 1)]
    [DataRow("binary Child { Value: int le }; binary Record { Tag: byte, Child: Child };", "Child.Value", 3, 1)]
    [DataRow("binary Record { A: bits[5], B: bits[12] };", "B", 1, 1)]
    public void PartialBinary_WithTruncatedInput_ShouldPreserveCursorAndPathWithoutThrowing(
        string schema, string field, int inputLength, int consumed)
    {
        // Three bytes allow the first field, but never the four-byte payload. The
        // bit example consumes all available bits before its later read fails.
        using var query = Compile(schema + " select p.BytesConsumed, p.ErrorField from #schema.first() f cross apply PartialInterpret<Record>(f.Content) p",
            new byte[] { 7, 1, 2 }.AsSpan(0, inputLength).ToArray());
        using var counter = new ThreadParseExceptionCounter();
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(consumed, row.GetValue<int>(0));
        Assert.AreEqual(field, row.GetValue<string>(1));
        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    [DataRow("text Record { Prefix: literal 'INFO|', Tail: rest };", "Tail")]
    [DataRow("text Record { Tail: pattern '[0-9]+' };", "Tail")]
    [DataRow("text Record { Tail: until ':' };", "Tail")]
    public void TolerantText_WithOrdinaryMismatch_ShouldRetainLeftRowWithoutThrowing(string schema, string field)
    {
        using var query = Compile(schema + $" select f.Name, p.{field} from #schema.first() f outer apply TryParse<Record>(f.Name) p", []);
        using var counter = new ThreadParseExceptionCounter();
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual("bad", row.GetValue<string>(0));
        Assert.IsNull(row.GetValue<string?>(1));
        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    public void PartialNestedText_ShouldRetainCompletedPrefixAndSafeDiagnosticsWithoutThrowing()
    {
        const string sql = "text Child { Value: chars[4] }; text Record { Tag: chars[1], Child: Child }; select p.BytesConsumed, p.ErrorField, p.ErrorMessage from #schema.first() f cross apply PartialParse<Record>(f.Name) p";
        using var query = Compile(sql, []);
        using var counter = new ThreadParseExceptionCounter();
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(1, row.GetValue<int>(0));
        Assert.AreEqual("Child.Value", row.GetValue<string>(1));
        Assert.Contains("ISE", row.GetValue<string>(2));
        Assert.DoesNotContain("bad", row.GetValue<string>(2));
        Assert.AreEqual(0, counter.Count);
    }

    private static CompiledQuery Compile(string sql, byte[] data) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider<BinaryEntity>(
            [new() { Name = "bad", Content = data }]), new TestsLoggerResolver(),
        new CompilationOptions(usePrimitiveTypeValidation: false));

    private sealed class ThreadParseExceptionCounter : IDisposable
    {
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        public int Count { get; private set; }

        public ThreadParseExceptionCounter() => AppDomain.CurrentDomain.FirstChanceException += Observe;

        private void Observe(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (Environment.CurrentManagedThreadId == _threadId && args.Exception is ParseException)
                Count++;
        }

        public void Dispose() => AppDomain.CurrentDomain.FirstChanceException -= Observe;
    }
}
