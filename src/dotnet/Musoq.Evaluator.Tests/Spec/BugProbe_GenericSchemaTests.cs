using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Generic/parameterized schemas.
///     Per spec section 8.4: "Parameterized schemas for reusable patterns"
///     Known issue: code generator emits generic type without type args (CS0305).
/// </summary>
[TestClass]
public class BugProbe_GenericSchemaTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Generic schema instantiation.
    ///     Spec 8.4 example: "binary LengthPrefixed&lt;T&gt; { Length: int le, Data: T[Length] }"
    ///     Expected: Generic schema should be instantiated with concrete type at compile time.
    /// </summary>
    [TestMethod]
    public void Binary_GenericSchema_ShouldInstantiateCorrectly()
    {
        var query = @"
            binary Item { Value: byte };
            binary LengthPrefixed<T> {
                Length: byte,
                Data: T[Length]
            };
            binary Container {
                Items: LengthPrefixed<Item>
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply c.Items.Data d
            order by d.Value asc";

        var testData = new byte[] { 0x02, 0x0A, 0x14 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)10, table[0][0]);
        Assert.AreEqual((byte)20, table[1][0]);
    }
}
