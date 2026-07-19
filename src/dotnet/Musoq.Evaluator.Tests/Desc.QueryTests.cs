using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class DescStatementTests
{
    [TestMethod]
    public void DescQuery_WithProjectedAlias_ShouldReturnProjectedMetadata()
    {
        const string query =
            "desc query (select Name as PersonName, Population + Money as Total from #A.entities())";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("PersonName", typeof(string)),
            ("Total", typeof(decimal)));
    }

    [TestMethod]
    public void DescQuery_WithStarExpansion_ShouldReturnExpandedOutputColumns()
    {
        const string query = "desc query (select * from #A.entities())";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("Population", typeof(decimal)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)),
            ("Time", typeof(DateTime)),
            ("Id", typeof(int)),
            ("NullableValue", typeof(int?)));
    }

    [TestMethod]
    public void DescQuery_WithParametersAndLets_ShouldBindInnerQueryNormally()
    {
        const string query = @"
param(adjust: decimal)
let suffix: string = '_tag'
desc query (
    select Name + $suffix as Label,
           Population + $adjust as Adjusted
    from #A.entities()
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        vm.Parameters["adjust"] = 10m;
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("Label", typeof(string)),
            ("Adjusted", typeof(decimal)));
    }

    [TestMethod]
    [DataRow("union")]
    [DataRow("union all")]
    [DataRow("except")]
    [DataRow("intersect")]
    public void DescQuery_WithSetOperator_ShouldReturnLeftProjectionMetadata(string setOperator)
    {
        var query = $@"
desc query (
    select Name as Label, Population as Amount
    from #A.entities()
    {setOperator} (Label, Amount)
    select City as Label, Money as Amount
    from #A.entities()
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("Label", typeof(string)),
            ("Amount", typeof(decimal)));
    }

    [TestMethod]
    public void DescQuery_WithFromFirstSyntax_ShouldReturnProjectedMetadata()
    {
        const string query =
            "desc query (from #A.entities() select Name as Label, Population + Money as Total)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("Label", typeof(string)),
            ("Total", typeof(decimal)));
    }

    [TestMethod]
    public void DescQuery_WithStarModifiers_ShouldReturnFinalProjectedNames()
    {
        const string query = @"
desc query (
    select * exclude (City)
             replace (Population * 2 as Population)
             rename (Name as EntityName, Population as WeightedPopulation)
    from #A.entities()
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("EntityName", typeof(string)),
            ("Country", typeof(string)),
            ("WeightedPopulation", typeof(decimal)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)),
            ("Time", typeof(DateTime)),
            ("Id", typeof(int)),
            ("NullableValue", typeof(int?)));
    }

    [TestMethod]
    public void DescQuery_WithCteParametersAndLets_ShouldReturnProjectedMetadata()
    {
        const string query = @"
param(prefix: string, adjust: decimal)
let suffix: string = '_tag'
desc query (
    with enriched as (
        select Name + $suffix as Label,
               Population + $adjust as Adjusted
        from #A.entities()
    )
    select $prefix + Label as FinalLabel,
           Adjusted + $adjust as FinalAdjusted
    from enriched
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        vm.Parameters["prefix"] = "person:";
        vm.Parameters["adjust"] = 10m;
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("FinalLabel", typeof(string)),
            ("FinalAdjusted", typeof(decimal)));
    }

    [TestMethod]
    public void DescQuery_WithJoinAndApply_ShouldBindWithoutExecutingInnerSource()
    {
        const string query = @"
desc query (
    select a.Name as LeftName,
           b.Score as RightScore,
           n.Value as Number,
           n.Ordinal as NumberOrdinal
    from #A.entities() a
    inner join #A.entities() b on a.Score = b.Score
    cross apply a.Numbers n with ordinality
)";
        var schemaProvider = new MetadataOnlySchemaProvider();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, schemaProvider.RowSourceCalls);
        AssertDescriptionRows(
            table,
            ("LeftName", typeof(string)),
            ("RightScore", typeof(int)),
            ("Number", typeof(int)),
            ("NumberOrdinal", typeof(int)));
    }

    [TestMethod]
    public void DescQuery_WithOuterApplyOrdinality_ShouldReturnNullableOrdinalWithoutExecutingInnerSource()
    {
        const string query = @"
desc query (
    select n.Ordinal as MaybeOrdinal
    from #A.entities() a
    outer apply a.Numbers n with ordinality
)";
        var schemaProvider = new MetadataOnlySchemaProvider();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, schemaProvider.RowSourceCalls);
        AssertDescriptionRows(
            table,
            ("MaybeOrdinal", typeof(int?)));
    }

    [TestMethod]
    public void DescQuery_WithWindowProjections_ShouldReturnWindowMetadata()
    {
        const string query = @"
desc query (
    select Name,
           RowNumber() over (order by NullableValue nulls last) as RowNo,
           Sum(Population) filter (where Population > 0)
               over (partition by City order by NullableValue nulls last rows between unbounded preceding and current row)
               as RunningPopulation
    from #A.entities()
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("Name", typeof(string)),
            ("RowNo", typeof(long)),
            ("RunningPopulation", typeof(decimal)));
    }

    [TestMethod]
    public void DescQuery_WithNullableAndTypeEdgeCases_ShouldReturnInferredMetadata()
    {
        const string query = @"
desc query (
    select a.NullableValue as MaybeSource,
           case when a.NullableValue is null then 0 else null end as MaybeCase,
           a.Name is distinct from a.City as IsDifferent
    from #A.entities() a
)";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var table = vm.Run(TestContext.CancellationToken);

        AssertDescriptionRows(
            table,
            ("MaybeSource", typeof(int?)),
            ("MaybeCase", typeof(int?)),
            ("IsDifferent", typeof(bool)));
    }

    [TestMethod]
    public void DescQuery_WithInvalidInnerQuery_ShouldReportInnerQueryDiagnostic()
    {
        const string query = "desc query (select Missing from #A.entities())";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada"))));

        AssertAnyEnvelopeHasCode(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void DescQuery_WhenRun_ShouldNotExecuteInnerSource()
    {
        const string query = "desc query (select Name as Label, Score + 1 as NextScore from #A.entities())";
        var schemaProvider = new MetadataOnlySchemaProvider();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, schemaProvider.RowSourceCalls);
        AssertDescriptionRows(
            table,
            ("Label", typeof(string)),
            ("NextScore", typeof(int)));
    }

    private static void AssertDescriptionShape(Table table)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Index", typeof(int)),
            ("Type", typeof(string)));
    }

    private static void AssertDescriptionRows(Table table, params (string Name, Type Type)[] expectedRows)
    {
        AssertDescriptionShape(table);

        var metadataRows = expectedRows
            .Select((row, index) => (object?[])[row.Name, index, row.Type.FullName])
            .ToArray();
        TableMaterializationTestHelper.AssertRowsInOrder(table, metadataRows);
    }

    private sealed class MetadataOnlySchemaProvider : ISchemaProvider
    {
        public int RowSourceCalls { get; private set; }

        public ISchema GetSchema(string schema)
        {
            return new MetadataOnlySchema(this);
        }

        public void RecordRowSourceCall()
        {
            RowSourceCalls++;
        }
    }

    private sealed class MetadataOnlySchema(MetadataOnlySchemaProvider provider)
        : SchemaBase("metadata", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new MetadataOnlyTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            provider.RecordRowSourceCall();
            throw new InvalidOperationException("DESC QUERY should not execute the inner source.");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class MetadataOnlyTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Components.SchemaColumn(nameof(MetadataOnlyEntity.Name), 0, typeof(string)),
            new Components.SchemaColumn(nameof(MetadataOnlyEntity.Score), 1, typeof(int)),
            new Components.SchemaColumn(nameof(MetadataOnlyEntity.Numbers), 2, typeof(int[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(MetadataOnlyEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class MetadataOnlyEntity
    {
        public string Name { get; init; } = string.Empty;

        public int Score { get; init; }

        public int[] Numbers { get; init; } = [];
    }
}
