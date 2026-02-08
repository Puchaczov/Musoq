using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Binary-text composition via 'as' clause.
///     Per spec section 8.5: "string[ConfigLen] utf8 as KeyValue" should parse the string
///     using a text schema.
///     Has E2E coverage in SchemaCompositionFeatureTests but may have edge cases.
/// </summary>
[TestClass]
public class BugProbe_BinaryTextCompositionTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Binary string field parsed as text schema via 'as' clause.
    ///     Spec 8.5: "The 'as' clause specifies that the string field should be further parsed using a text schema"
    ///     Expected: String bytes decoded, then parsed by text schema, producing structured access.
    /// </summary>
    [TestMethod]
    public void Binary_StringAsTextSchema_ShouldParseIntoStructured()
    {
        var query = @"
            text KeyValue { Key: until '=', Value: rest };
            binary Config {
                Len: byte,
                Data: string[Len] utf8 as KeyValue
            };
            select c.Data.Key, c.Data.Value from #test.files() b
            cross apply Interpret(b.Content, 'Config') c";

        var payload = Encoding.UTF8.GetBytes("host=local");
        var testData = new byte[] { (byte)payload.Length };
        var combined = new byte[testData.Length + payload.Length];
        Buffer.BlockCopy(testData, 0, combined, 0, testData.Length);
        Buffer.BlockCopy(payload, 0, combined, testData.Length, payload.Length);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = combined } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("local", table[0][1]);
    }
}
