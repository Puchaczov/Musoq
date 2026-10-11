using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Evaluator.Tests.Schema.RuntimeV2;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryFunctionCatalogQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void Discovery_WhenLibraryContainsImplementationHelpers_ShouldExposeOnlySqlContracts()
    {
        const string query = "desc functions #schema";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var signatures = result.Select(static row => (string)row.ReadValue(0)).ToArray();

        Assert.IsTrue(signatures.Any(static signature => signature.Contains("RowNumber(") && signature.Contains("OVER (...)")));
        Assert.IsTrue(signatures.Any(static signature => signature.Contains("PositiveTotal(")));
        var implementationSignatures = signatures.Where(static signature => signature.Contains("__ScalarSubquery") ||
            signature.Contains("__CorrelatedScalar") || signature.Contains("WindowRowNumber") ||
            signature.Contains("IWindowFunction") || signature.Contains("Kernel")).ToArray();
        Assert.IsEmpty(implementationSignatures, string.Join("; ", implementationSignatures));
    }

    [TestMethod]
    public void CompilerOnlyDeclaration_WhenCalledFromSql_ShouldFailBeforeExecution()
    {
        const string query = "select CompilerValue() from #schema.first()";

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, CatalogLibrary>(query, LibraryExecutionFixture.CreateRows()));

        Assert.Contains("cannot be called from user SQL", error.ToString());
    }

    [TestMethod]
    public void AggregateAlias_WhenSqlNameDiffersFromClrName_ShouldExecuteRegisteredKernel()
    {
        const string query = "select InvoiceTotal(Amount, 2) as Total from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, CatalogLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [12m]);
    }

    [TestMethod]
    public void AggregateAlias_WhenClrNameIsUsedInSql_ShouldRejectTheImplementationName()
    {
        const string query = "select Calculate(Amount, 2) from #schema.first()";

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, CatalogLibrary>(query, LibraryExecutionFixture.CreateRows()));

        Assert.Contains("cannot be called from user SQL", error.ToString());
    }

    [TestMethod]
    public void CombinedDeclaration_WhenUsedAsAggregate_ShouldBindWithoutAmbiguousAttributeLookup()
    {
        const string query = "select Both(Amount) as Total from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, CatalogLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [9m]);
    }

    [TestMethod]
    public void InheritedDeclaration_WhenHiddenByDescendant_ShouldUseTheDescendantImplementation()
    {
        const string query = "select Prefix(Partition) as Value from #schema.first() where Position = 1";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, DescendantLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["derived:A"]);
    }

    [TestMethod]
    public void CorrelatedAggregate_WhenCompilerHelpersAreHidden_ShouldStillExecutePerSourceRow()
    {
        const string query = """
            select a.Position,
                   (select Sum(b.Amount) from #schema.first() b
                    where b.Partition = a.Partition) as Total
            from #schema.first() a order by a.Position
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            [1, 4m], [2, 4m], [3, 4m], [4, 2m], [5, 2m]);
    }

    [TestMethod]
    public void DistinctAggregate_WhenSyntaxUsesThePublicFamilyName_ShouldBindTheDistinctDeclaration()
    {
        const string query = "select Sum(distinct Integral) as Total from #schema.first()";
        LibraryExecutionEntity[] rows = [new(1, "A", 2, 2m, 2d), new(2, "A", 2, 2m, 2d)];
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query, rows);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [2]);
    }

    [TestMethod]
    public void PublicDecimalConversion_WhenUsedInFilterAndProjection_ShouldProduceNullableDecimals()
    {
        const string query = "select Id, ToDecimal(Amount) as Converted from #schema.first() " +
                             "where ToDecimal(Amount) > 100.50d order by Id";
        RuntimeV2RegressionEntity[] rows =
        [
            new() { Id = 1, Amount = 101d },
            new() { Id = 2, Amount = 100 },
            new() { Id = 3, Amount = null },
            new() { Id = 4, Amount = 102d }
        ];
        using var compiled = CreateAndRunVirtualMachine<RuntimeV2RegressionEntity, LibraryBase>(query, rows);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)), ("Converted", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, 101m], [4, 102m]);
    }

    public sealed class CatalogLibrary : LibraryBase
    {
        [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
        public int CompilerValue() => 41;

        [AggregateFunction(typeof(LibraryExecutionLibrary.ScaledTotalKernel), Name = "InvoiceTotal")]
        public decimal? Calculate(decimal? value, int factor) => AggregateFunction.NotInvoked<decimal?>();

        [AggregateFunction(typeof(LibraryExecutionLibrary.PositiveTotalKernel))]
        [WindowFunction(KernelType = typeof(LibraryExecutionLibrary.PositiveTotalKernel),
            Modes = WindowEvaluationModes.Running)]
        public decimal? Both(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
    }

    public class AncestorLibrary : LibraryBase
    {
        [BindableMethod]
        public string Prefix(string value) => $"base:{value}";
    }

    public sealed class DescendantLibrary : AncestorLibrary
    {
        [BindableMethod]
        public new string Prefix(string value) => $"derived:{value}";
    }
}
