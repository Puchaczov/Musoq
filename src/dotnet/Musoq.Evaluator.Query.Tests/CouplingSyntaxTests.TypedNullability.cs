using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P3-015")]
    public void CoupledTypedProviderWildcard_ShouldPreserveDeclaredColumnOrder()
    {
        const string query = "table OrderedRows { City: string, Name: string, Id: int }; couple test.Entities with table OrderedRows as People; select * from People()";
        var entities = new[] { new BasicEntity { City = "Gdansk", Name = "Ada", Id = 7 } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("City", typeof(string)), ("Name", typeof(string)), ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { "Gdansk", "Ada", 7 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P3-002")]
    public void CoupledTypedProvider_ShouldPreserveProviderIdMetadata()
    {
        const string query = "table PersonRow { Id: int, Name: string }; couple test.Entities with table PersonRow as People; select p.Id as Id, p.Name as Name from People() p";
        var entities = new[] { new BasicEntity { Name = "Ada", Id = 7 } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7, "Ada" });
        TableMaterializationTestHelper.AssertColumns(result,
            ("Id", typeof(int)), ("Name", typeof(string)));
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-ValueTypeTableColumnIsAutomaticallyNullable")]
    public void ValueTypeTableColumn_ShouldPreserveProviderNullability()
    {
        const string query = "table T { Id: int }; couple test.Entities with table T as X; select Id from X()";
        var rows = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["test"] = new[] { new BasicEntity { Id = 7 } },
            ["#test"] = new[] { new BasicEntity { Id = 7 } }
        };
        using var compiled = CreateAndRunVirtualMachine(query, rows);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7 });
        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)));
    }

    [TestMethod]
    [DataRow("table PersonRow { Name: STRING, Id: Int }; couple test.Entities with table PersonRow as People; select p.Name as Name, p.Id as Id from People() p", "Mira", 8, DisplayName = "P3-003: mixed-case type names")]
    [DataRow("table PersonRow { Name: System.String, Id: System.Int32 }; couple test.Entities with table PersonRow as People; select p.Name as Name, p.Id as Id from People() p", "Noah", 9, DisplayName = "P3-004: CLR type names")]
    public void TypedCouplingTypeSpellings_ShouldMatchProviderMetadata(string query, string name, int id)
    {
        var entities = new[] { new BasicEntity { Name = name, Id = id } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { name, id });
        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Id", typeof(int)));
    }
}
