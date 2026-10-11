using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

public sealed partial class StructuralInputBindingTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0157")]
    public void ExplicitNullSuppressesConstructorDefault()
    {
        const string query = "select d.IsNull from #specstressnullable.probe(input: (Mode: null)) d";
        using var compiled = CompileNullableConstructorInput(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("IsNull", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { true });
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-ExplicitNullForNullableStructuralFieldIsPreserved")]
    public void ExplicitNullForNullableStructuralFieldIsPreserved()
    {
        const string query = "select d.IsNull from #specstressnullable.probe(input: (Mode: null)) d";
        using var compiled = CompileNullableConstructorInput(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue((bool)result[0].ReadValues()[0]!);
    }

    [TestMethod]
    [DataRow("select d.IsNull from #specstressnullable.probe(input: (Mode: 'provided')) d", false)]
    [DataRow("select d.IsNull from #specstressnullable.number(input: (Value: null)) d", true)]
    [DataRow("select d.IsNull from #specstressnullable.number(input: (Value: 9)) d", false)]
    public void NullableConstructorArgument_ShouldPreserveSuppliedValue(string query, bool expectedNull)
    {
        using var compiled = CompileNullableConstructorInput(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(expectedNull, result[0].ReadValue(0));
    }

    private CompiledQuery CompileNullableConstructorInput(string query)
    {
        return InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NullableDefaultSchemaProvider(),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false),
            TestContext.CancellationToken);
    }

    public readonly record struct NullableDefaultInput(string? Mode = "constructor-default");
    public readonly record struct NullableNumberDefaultInput(int? Value = 7);
    public readonly record struct NullableDefaultRow(bool IsNull);

    private sealed class NullableDefaultSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema.TrimStart('#'), "specstressnullable", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new NullableDefaultSchema();
        }
    }

    private sealed class NullableDefaultSchema : SchemaBase
    {
        public NullableDefaultSchema() : base("specstressnullable", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<StructuredInputTable<NullableDefaultRow>>("probe");
            AddTypedSource<NullableDefaultSource>("probe");
            AddTable<StructuredInputTable<NullableDefaultRow>>("number");
            AddTypedSource<NullableNumberDefaultSource>("number");
        }
    }

    public sealed class NullableDefaultSource : RowSource<NullableDefaultRow>
    {
        private readonly NullableDefaultRow _row;

        public NullableDefaultSource(NullableDefaultInput input, SourceExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _row = new NullableDefaultRow(input.Mode is null);
        }

        public override IEnumerable<IReadOnlyList<NullableDefaultRow>> Chunks
        {
            get { yield return [_row]; }
        }
    }

    public sealed class NullableNumberDefaultSource : RowSource<NullableDefaultRow>
    {
        private readonly NullableDefaultRow _row;

        public NullableNumberDefaultSource(NullableNumberDefaultInput input, SourceExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _row = new NullableDefaultRow(input.Value is null);
        }

        public override IEnumerable<IReadOnlyList<NullableDefaultRow>> Chunks
        {
            get { yield return [_row]; }
        }
    }
}
