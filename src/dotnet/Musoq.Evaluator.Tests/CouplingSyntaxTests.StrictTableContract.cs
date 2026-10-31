using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    [TestMethod]
    [DataRow("Id: int?", "int")]
    [DataRow("NullableValue: int", "int?")]
    [DataRow("Id: long", "int")]
    [DataRow("Name: int", "string")]
    public void TypedTable_IncompatibleUnselectedColumn_ShouldRejectAtDeclaration(string declaration, string providerType)
    {
        var query = $"table Contract {{ {declaration} }}; couple test.Entities with table Contract as Rows; select 1 as Value from Rows()";
        var provider = new BasicSchemaProvider<BasicEntity>(StrictContractRows());
        AssertStrictTableDiagnostic(query, provider, declaration, DiagnosticCode.MQ3005_TypeMismatch, providerType);
    }

    [TestMethod]
    public void TypedTable_UndeclaredColumn_ShouldBeUnavailable()
    {
        const string query = "table Contract { Id: int }; couple test.Entities with table Contract as Rows; select r.Name from Rows() r";
        var result = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(StrictContractRows()), LoggerResolver, TestCompilationOptions);
        try
        {
            Assert.IsFalse(result.Succeeded);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, result.Errors.Single().Code);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    [TestMethod]
    public void TypedTable_NullableColumnAndClrSynonym_ShouldPreserveValues()
    {
        const string query = "table Contract { NullableValue: int?, Id: System.Int32 }; couple test.Entities with table Contract as Rows; select r.NullableValue as Optional, r.Id as Id from Rows() r order by r.Id";
        using var compiled = CreateAndRunVirtualMachine(query, StrictContractRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Optional", typeof(int?)), ("Id", typeof(int)));
        CollectionAssert.AreEqual(new object?[] { null, 7 }, result[0].ReadValues());
        CollectionAssert.AreEqual(new object?[] { 32, 8 }, result[1].ReadValues());
    }

    [TestMethod]
    public void TypedTable_MultipleDeclarationsAndDirectSource_ShouldKeepIndependentShapes()
    {
        const string query = "table LeftRows { Name: string, Id: int }; table RightRows { City: string, Id: int }; " +
                             "couple test.Entities with table LeftRows as LeftPeople; couple test.Entities with table RightRows as RightPeople; " +
                             "select l.Name as Name, r.City as City, d.Id as Id from LeftPeople() l " +
                             "inner join RightPeople() r on l.Id = r.Id inner join #test.Entities() d on d.Id = l.Id order by d.Id";
        using var compiled = CreateAndRunVirtualMachine(query, StrictContractRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        CollectionAssert.AreEqual(new object?[] { "Ada", "Gdansk", 7 }, result[0].ReadValues());
        CollectionAssert.AreEqual(new object?[] { "Mira", "Warsaw", 8 }, result[1].ReadValues());
    }

    [TestMethod]
    public void TypedTable_ReorderedSubset_ShouldSurviveCteFilterGroupAndOrder()
    {
        const string query = "table Contract { Name: string, Id: int }; couple test.Entities with table Contract as Rows; " +
                             "with Picked as (select r.Name as Name, r.Id as Id from Rows() r where r.Id >= 7) " +
                             "select p.Name as Name, Sum(p.Id) as Total from Picked p group by p.Name order by p.Name";
        using var compiled = CreateAndRunVirtualMachine(query, StrictContractRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Ada", result[0].ReadValues()[0]);
        Assert.AreEqual(7, Convert.ToInt32(result[0].ReadValues()[1]));
        Assert.AreEqual("Mira", result[1].ReadValues()[0]);
        Assert.AreEqual(8, Convert.ToInt32(result[1].ReadValues()[1]));
    }

    [TestMethod]
    public void TypedTable_OuterJoin_ShouldLiftNonnullableResult()
    {
        const string query = "table Contract { Id: int }; couple test.Entities with table Contract as Rows; " +
                             "select r.Id as Id from values { (Key: 7), (Key: 9) } l left outer join Rows() r on l.Key = r.Id order by l.Key";
        using var compiled = CreateAndRunVirtualMachine(query, StrictContractRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int?)));
        CollectionAssert.AreEqual(new object?[] { 7 }, result[0].ReadValues());
        CollectionAssert.AreEqual(new object?[] { null }, result[1].ReadValues());
    }

    [TestMethod]
    [DataRow("Missing: int", DiagnosticCode.MQ3001_UnknownColumn, false)]
    [DataRow("Id: int", DiagnosticCode.MQ3002_AmbiguousColumn, true)]
    [DataRow("Id: int?", DiagnosticCode.MQ3005_TypeMismatch, false)]
    [DataRow("Id: int?", DiagnosticCode.MQ3005_TypeMismatch, false, true)]
    public void TypedArrayTable_ShouldUseExplicitOriginAndRejectBeforeSourceOpening(string declaration, DiagnosticCode code, bool ambiguous, bool dictionary = false)
    {
        var schema = new StrictContractArraySchema(ambiguous, dictionary);
        var query = $"table Contract {{ {declaration} }}; couple array.rows with table Contract as Rows; select 1 as Value from Rows()";
        AssertStrictTableDiagnostic(query, new StrictContractArrayProvider(schema), declaration, code, "datasource");
        Assert.IsFalse(schema.SourceOpened);
        Assert.AreEqual(TableSchemaTypeOrigin.ProviderDefined, schema.Table.Metadata.TypeOrigin);
    }

    [TestMethod]
    public void TypedArrayTable_ReorderedSparseSlots_ShouldReadOriginalStoragePositions()
    {
        const string query = "table Contract { name: string, id: int }; couple array.rows with table Contract as Rows; select r.* from Rows() r where r.Id = 42";
        var schema = new StrictContractArraySchema(false);
        var build = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(),
            new StrictContractArrayProvider(schema), LoggerResolver, TestCompilationOptions);
        Assert.IsTrue(build.Succeeded, string.Join(Environment.NewLine, build.Diagnostics));
        using var compiled = build.CompiledQuery!;
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Id", typeof(int)));
        CollectionAssert.AreEqual(new object?[] { "Ada", 42 }, result[0].ReadValues());
        Assert.IsTrue(schema.SourceOpened);
    }

    [TestMethod]
    [DataRow("Status: int")]
    [DataRow("Status: ExpectedState")]
    public void TypedTable_EnumIdentityMismatch_ShouldNameLogicalTypesAndRejectBeforeOpening(string declaration)
    {
        var descriptor = new EnumTypeDescriptor("ProviderState", EnumTypeOrigin.QueryLocal, EnumUnderlyingKind.Int32, false,
            [new EnumMemberDescriptor("Ready", EnumScalarValue.FromRaw(EnumUnderlyingKind.Int32, 1))]);
        var column = new SchemaColumn("Status", 3, typeof(int), typeof(int), descriptor);
        var schema = new StrictContractArraySchema(false, columns: [column]);
        var query = $"enum ExpectedState : int {{ Ready = 1i }}; table Contract {{ {declaration} }}; " +
                    "couple array.rows with table Contract as Rows; select 1 as Value from Rows()";
        AssertStrictTableDiagnostic(query, new StrictContractArrayProvider(schema), declaration,
            DiagnosticCode.MQ3005_TypeMismatch, "ENUM 'ProviderState'");
        Assert.IsFalse(schema.SourceOpened);
    }

    [TestMethod]
    public void TypedTable_NativeEnumCarrier_ShouldSuggestDirectProjection()
    {
        var column = new SchemaColumn("Status", 3, typeof(REC103NativeStatus));
        var schema = new StrictContractArraySchema(false, columns: [column]);
        const string declaration = "Status: short";
        const string query = "table Contract { Status: short }; couple array.rows with table Contract as Rows; select 1 as Value from Rows()";
        AssertStrictTableDiagnostic(query, new StrictContractArrayProvider(schema), declaration,
            DiagnosticCode.MQ3005_TypeMismatch, "directly without a TABLE declaration");
        Assert.IsFalse(schema.SourceOpened);
    }

    private void AssertStrictTableDiagnostic(string query, ISchemaProvider provider, string declaration, DiagnosticCode code, string fragment)
    {
        var result = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        try
        {
            Assert.IsFalse(result.Succeeded);
            Assert.HasCount(1, result.Errors);
            var diagnostic = result.Errors.Single();
            Assert.AreEqual(code, diagnostic.Code);
            Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
            Assert.AreEqual(new TextSpan(query.IndexOf(declaration, StringComparison.Ordinal), declaration.Length), diagnostic.Span);
            Assert.Contains("Contract", diagnostic.Message);
            Assert.Contains(fragment, diagnostic.Message);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> StrictContractRows() => new()
    {
        ["test"] = [new BasicEntity { Id = 7, Name = "Ada", City = "Gdansk", NullableValue = null },
                    new BasicEntity { Id = 8, Name = "Mira", City = "Warsaw", NullableValue = 32 }],
        ["#test"] = [new BasicEntity { Id = 7, Name = "Ada", City = "Gdansk", NullableValue = null },
                     new BasicEntity { Id = 8, Name = "Mira", City = "Warsaw", NullableValue = 32 }]
    };
}

internal sealed class StrictContractArrayProvider(StrictContractArraySchema schema) : ISchemaProvider
{
    public ISchema GetSchema(string name) => schema;
}

internal sealed class StrictContractArraySchema(bool ambiguous, bool dictionary = false, ISchemaColumn[]? columns = null) : SchemaBase("array", new MethodsAggregator(new MethodsManager())), IQueryScopedRowSourceSchema
{
    public StrictContractArrayTable Table { get; } = new(ambiguous, dictionary, columns);
    public bool SourceOpened { get; private set; }
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] parameters) => Table;
    public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext context) =>
        [new SchemaMethodInfo(methodName, Musoq.Schema.Reflection.ConstructorInfo.Empty())];
    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows |
                SourceTransferCapabilities.LogicalScalarReads
        };
    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(string name,
        QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments
    {
        SourceOpened = true;
        return new StrictContractArrayRows<TRow, TMaterializer>(request.Shape);
    }
    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments parameters)
        => throw new InvalidOperationException("The strict-contract fixture requires native query-scoped transfer.");
}

internal sealed class StrictContractArrayTable(bool ambiguous, bool dictionary, ISchemaColumn[]? columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns ?? (ambiguous
        ? [new SchemaColumn("Id", 3, typeof(int)), new SchemaColumn("Id", 7, typeof(int))]
        : [new SchemaColumn("Id", 3, typeof(int)), new SchemaColumn("Name", 7, typeof(string))]);
    public SchemaTableMetadata Metadata { get; } = new(dictionary ? typeof(IReadOnlyDictionary<string, object?>) : typeof(object[]));
    public ISchemaColumn? GetColumnByName(string name) => GetColumnsByName(name).SingleOrDefault();
    public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed class StrictContractArrayRows<TRow, TMaterializer>(QueryRowShape shape) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var reader = new FixtureReader(shape);
        writer.Write(new TRow[] { TMaterializer.Materialize(ref reader) });
    }
    private readonly struct FixtureReader(QueryRowShape shape) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            object[] values = ["unused", "wrong name", -1, 42, 0, 0, 0, "Ada"];
            return (T)values[shape.Fields[slot].SourceColumnIndex];
        }
    }
}
