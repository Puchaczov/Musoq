using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text 'repeat' fields.
///     Per spec section 5.9: "Parses the inner type repeatedly until delimiter or end."
///     No E2E coverage exists.
/// </summary>
[TestClass]
public class BugProbe_TextRepeatFieldTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Repeat schema type until end of input via cross apply.
    ///     Spec 5.9: "Results in array of parsed elements"
    ///     Expected: Multiple Header lines parsed and accessible via cross apply.
    /// </summary>
    [TestMethod]
    public void Text_RepeatSchemaUntilEnd_ShouldProduceArray()
    {
        var query = @"
            text Pair { Key: until '=', Value: rest };
            text Config { Entries: repeat Pair until end };
            select e.Key, e.Value from #test.lines() l
            cross apply Parse(l.Line, 'Config') c
            cross apply c.Entries e
            order by e.Key asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host=localhost" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }
}
