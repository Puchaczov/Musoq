using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void IndependentValuesSources_ReusedAliasRetainsNullableAndNumericTypes()
    {
        const string query = "with FirstRows as (select v.First as First from values { (First: null), (First: 1l) } v), SecondRows as (select v.Second as Second from values { (Second: 2i), (Second: 3i) } v) select f.First as First, s.Second as Second from FirstRows f cross join SecondRows s";
        using var compiled = InstanceCreator.CompileForExecution(query, nameof(IndependentValuesSources_ReusedAliasRetainsNullableAndNumericTypes),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("First", typeof(long?)), ("Second", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { null, 2 }, new object?[] { null, 3 },
            new object?[] { 1L, 2 }, new object?[] { 1L, 3 });
    }

    [TestMethod]
    public void CteReference_ReusedSourceAliasRetainsRenamedColumns()
    {
        const string query = "with Parsed as (select v.Name as ExportedName from values { (Name: 'first'), (Name: 'second') } v) select v.ExportedName as Name from Parsed v";
        using var compiled = InstanceCreator.CompileForExecution(query, nameof(CteReference_ReusedSourceAliasRetainsRenamedColumns),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "first" },
            new object?[] { "second" });
    }
}
