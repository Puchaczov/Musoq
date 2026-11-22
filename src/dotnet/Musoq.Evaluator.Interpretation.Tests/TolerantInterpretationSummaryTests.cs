using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TolerantInterpretationSummaryTests
{
    private const string ObserverProperty = "Musoq.InterpretationAttempt.v1";
    private const string Query = """
        binary Packet { Code: byte };
        text Entry { Header: literal 'OK|', Body: rest };
        with Samples as (
            select v.Id, b.Code, t.Body as Message
            from values {
                (Id: 1, Hex: '2A', Line: 'OK|first'),
                (Id: 2, Hex: '', Line: 'NO|private input')
            } v
            outer apply TryInterpret<Packet>(FromHexToBytes(v.Hex)) b
            outer apply TryParse<Entry>(v.Line) t
        )
        select s.Id, s.Code, s.Message from Samples s order by s.Id
        """;

    [TestMethod]
    public void ObserverReportsCountsAndReasonsOnEveryWarmExecution()
    {
        using var compiled = Compile();
        var expectedRows = new (byte?, string?)[] { (42, "first"), (null, null) };
        var expectedAttempts = new (string, string, int)[]
        {
            ("TryInterpret", "Packet", 0),
            ("TryInterpret", "Packet", 1),
            ("TryParse", "Entry", 0),
            ("TryParse", "Entry", 4),
        };
        CollectionAssert.AreEqual(expectedRows, ReadRows(compiled));

        for (var run = 0; run < 2; run++)
        {
            var observed = new ConcurrentQueue<(string, string, int)>();
            using var activity = Observe((operation, schema, code) => observed.Enqueue((operation, schema, code)));
            using var nested = new Activity("Nested query execution context").Start();

            CollectionAssert.AreEqual(expectedRows, ReadRows(compiled));
            CollectionAssert.AreEquivalent(expectedAttempts, observed.ToArray());
        }

        CollectionAssert.AreEqual(expectedRows, ReadRows(compiled));
    }

    [TestMethod]
    public void CancelledEvaluationRetainsOnlyObservedAttempts()
    {
        using var compiled = Compile();
        using var cancellation = new CancellationTokenSource();
        var observed = new ConcurrentQueue<(string, string, int)>();
        using var activity = Observe(
            (operation, schema, code) =>
            {
                observed.Enqueue((operation, schema, code));
                cancellation.Cancel();
            }
        );

        Assert.Throws<OperationCanceledException>(() =>
        {
            using var result = compiled.Run(cancellation.Token);
            _ = result.Count;
        });
        CollectionAssert.AreEqual(new[] { ("TryInterpret", "Packet", 0) }, observed.ToArray());
    }

    private static CompiledQuery Compile() =>
        InstanceCreator.CompileForExecution(
            Query,
            nameof(TolerantInterpretationSummaryTests),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );

    private static (byte?, string?)[] ReadRows(CompiledQuery compiled)
    {
        using var result = compiled.Run();
        return result.Select(row => (row.GetValue<byte?>(1), row.GetValue<string?>(2))).ToArray();
    }

    private static Activity Observe(Action<string, string, int> record)
    {
        var activity = new Activity("Observe tolerant interpretations");
        activity.SetCustomProperty(ObserverProperty, record);
        return activity.Start();
    }
}
