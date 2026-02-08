using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Binary 'repeat until' construct.
///     Per spec section 4.10: "Read TLV records until type is 0x00 (terminator)"
///     Known issue: dot notation in repeat-until condition doesn't work.
/// </summary>
[TestClass]
public class BugProbe_RepeatUntilTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Repeat until with field access on last element using dot notation.
    ///     Spec 4.10: "Records[-1].Type" should reference last parsed element's field.
    ///     Known issue: Parser doesn't handle dots in the repeat-until condition expression.
    /// </summary>
    [TestMethod]
    public void Binary_RepeatUntilWithDotNotation_ShouldTerminateOnCondition()
    {
        var query = @"
            binary Record { Type: byte, Value: byte };
            binary Stream { Records: Record repeat until Records[-1].Type = 0 };
            select r.Type, r.Value from #test.files() b
            cross apply Interpret(b.Content, 'Stream') s
            cross apply s.Records r
            where r.Type <> 0
            order by r.Value asc";

        var testData = new byte[]
        {
            0x01, 0x0A,  // Type=1, Value=10
            0x02, 0x14,  // Type=2, Value=20
            0x00, 0x00   // Type=0 (terminator)
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)10, table[0][1]);
        Assert.AreEqual((byte)20, table[1][1]);
    }
}
