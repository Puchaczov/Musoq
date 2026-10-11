using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public sealed partial class StructuralInputFailureTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0166")]
    public void Utf16LimitCountsBytes()
    {
        const string query = "param(value: string) select d.Value from #specstressutf16.probe(value: $value) d";
        using var compiled = CompileUtf16Source(query);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "value", "abc");

        var exception = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });

        StringAssert.Contains(exception.ToString(), "MQ7013");
    }

    [TestMethod]
    [DataRow("ab")]
    [DataRow("😀")]
    public void Utf16LimitAtFourBytes_ShouldAcceptScalarParameter(string value)
    {
        using var compiled = CompileUtf16Source(
            "param(value: string) select d.Value from #specstressutf16.probe(value: $value) d");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "value", value);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0, result[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("select d.Value from #specstressutf16.probe(value: 'abc') d")]
    [DataRow("let value: string = 'abc'; select d.Value from #specstressutf16.probe(value: $value) d")]
    public void Utf16LimitExceeded_ShouldRejectInlineAndLetScalars(string query)
    {
        using var compiled = CompileUtf16Source(query);

        var exception = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });

        StringAssert.Contains(exception.ToString(), "MQ7013");
    }

    private CompiledQuery CompileUtf16Source(string query)
    {
        return InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new Utf16SchemaProvider(), new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken);
    }

    private sealed class Utf16SchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema.TrimStart('#'), "specstressutf16", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new Utf16Schema();
        }
    }

    private sealed class Utf16Schema : SchemaBase
    {
        public Utf16Schema() : base("specstressutf16", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<StructuredInputTable<StrictLimitRow>>("probe");
            AddTypedSource<Utf16Source>("probe",
                new TypedSourceRegistrationOptions(new StructuralInputLimits(32, 100, 4)));
        }
    }

    public sealed class Utf16Source : RowSource<StrictLimitRow>
    {
        private readonly SourceExecutionContext _context;

        public Utf16Source(string value, SourceExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(value);
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override IEnumerable<IReadOnlyList<StrictLimitRow>> Chunks
        {
            get
            {
                _context.EndWorkToken.ThrowIfCancellationRequested();
                yield return [new StrictLimitRow(0)];
            }
        }
    }
}
