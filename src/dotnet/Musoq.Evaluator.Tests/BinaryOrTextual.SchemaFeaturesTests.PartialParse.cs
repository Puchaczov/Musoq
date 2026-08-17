using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_PartialParse_CrossApplyValidText_ShouldReturnPartialResultShape()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<KeyValue>(f.Text) p";

        var entities = new[] { new TextEntity { Name = "valid.txt", Text = "host=localhost" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.ParsedFields", typeof(Dictionary<string, object?>)),
            ("p.ErrorField", typeof(string)),
            ("p.ErrorMessage", typeof(string)),
            ("p.BytesConsumed", typeof(int)));
        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual("host", parsedFields["Key"]);
        Assert.AreEqual("localhost", parsedFields["Value"]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual("host=localhost".Length, table[0][3]);
    }

    [TestMethod]
    public void Query_PartialParse_CrossApplyMalformedText_ShouldReturnErrorMetadata()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: chars[5]
            };
            select
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<KeyValue>(f.Text) p";

        var entities = new[] { new TextEntity { Name = "truncated.txt", Text = "host=" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.ParsedFields", typeof(Dictionary<string, object?>)),
            ("p.ErrorField", typeof(string)),
            ("p.ErrorMessage", typeof(string)),
            ("p.BytesConsumed", typeof(int)));
        Assert.AreEqual(1, table.Count);
        Assert.IsInstanceOfType<Dictionary<string, object?>>(table[0][0]);
        Assert.AreEqual("Unknown", table[0][1]);
        Assert.IsNotNull(table[0][2]);
        Assert.AreEqual("host=".Length, table[0][3]);
    }

    [TestMethod]
    public void Query_PartialParse_OutsideApply_ShouldReportInterpretFunctionOutsideApply()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select PartialParse<KeyValue>(f.Text)
            from #test.lines() f";

        var entities = new[] { new TextEntity { Name = "valid.txt", Text = "host=localhost" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind);
    }
}
