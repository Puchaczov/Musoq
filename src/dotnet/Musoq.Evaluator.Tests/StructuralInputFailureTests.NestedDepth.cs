using System;
using System.Collections.Generic;
using System.Threading;
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
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0164")]
    public void NestedCollectionLimitRejectsBeforeSourceConstruction()
    {
        const string query = "param(values: int[][][][][][][][]) select d.Value from #specstressdepth.probe(values: $values) d";
        NestedDepthSource.ResetConstructionCount();
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NestedDepthSchemaProvider(), new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken);
        Assert.AreEqual(0, NestedDepthSource.ConstructionCount);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", new int[][][][][][][][] { [[[[[[[1]]]]]]] });

        var exception = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });

        StringAssert.Contains(exception.ToString(), "MQ7013");
        Assert.AreEqual(0, NestedDepthSource.ConstructionCount);
    }

    private sealed class NestedDepthSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema.TrimStart('#'), "specstressdepth", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new NestedDepthSchema();
        }
    }

    private sealed class NestedDepthSchema : SchemaBase
    {
        public NestedDepthSchema() : base("specstressdepth", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<StructuredInputTable<StrictLimitRow>>("probe");
            AddTypedSource<NestedDepthSource>("probe",
                new TypedSourceRegistrationOptions(new StructuralInputLimits(8, 100, 1024)));
        }
    }

    public sealed class NestedDepthSource : RowSource<StrictLimitRow>
    {
        private static int _constructionCount;
        private readonly SourceExecutionContext _context;

        public NestedDepthSource(
            IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>>>>>> values,
            SourceExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(values);
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Interlocked.Increment(ref _constructionCount);
        }

        public static int ConstructionCount => Volatile.Read(ref _constructionCount);
        public static void ResetConstructionCount() => Interlocked.Exchange(ref _constructionCount, 0);

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
