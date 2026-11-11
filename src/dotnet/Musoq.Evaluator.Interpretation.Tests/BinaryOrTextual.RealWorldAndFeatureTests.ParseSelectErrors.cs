using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Parse and TryParse in SELECT Should Fail

    [TestMethod]
    public void Query_ParseInSelect_ShouldProduceMeaningfulError()
    {
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select Parse<LogEntry>('INFO: booted')\n"
            + "            from #test.lines() f"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind, "Parse");
        AssertApplyGuidance(ex);
    }

    [TestMethod]
    public void Query_TryParseInSelect_ShouldProduceMeaningfulError()
    {
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select TryParse<LogEntry>('INFO: booted')\n"
            + "            from #test.lines() f"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind, "TryParse");
        AssertApplyGuidance(ex);
    }

    [TestMethod]
    public void Query_InterpretInSelect_ShouldProduceMeaningfulError()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Magic: int le\n"
            + "            };\n"
            + "            select Interpret<Header>(0x00)\n"
            + "            from #test.files() f"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = [0x00] },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind, "Interpret");
        AssertApplyGuidance(ex);
    }

    [TestMethod]
    public void Query_ParseInWhereClause_ShouldProduceMeaningfulError()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select 1\n"
            + "            from #test.lines() f\n"
            + "            where Parse<KeyValue>('key=val') is not null"
        );

        var entities = new[]
        {
            new TextEntity { Name = "data.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind, "Parse");
        AssertApplyGuidance(ex);
    }

    private static void AssertApplyGuidance(MusoqQueryException exception)
    {
        Assert.IsTrue(
            exception.Message.Contains("CROSS APPLY", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning CROSS APPLY or OUTER APPLY, got: {exception.Message}"
        );
    }

    #endregion
}
