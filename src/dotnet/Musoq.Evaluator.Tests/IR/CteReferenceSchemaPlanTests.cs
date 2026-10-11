using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CteReferenceSchemaPlanTests : BasicEntityTestBase
{
    [TestMethod]
    public void IndependentValuesSources_ShouldRetainTheirOwnTypedSchemas()
    {
        const string query = "with FirstRows as (select v.First as First from values { (First: null), (First: 1l) } v), SecondRows as (select v.Second as Second from values { (Second: 2i), (Second: 3i) } v) select f.First as First, s.Second as Second from FirstRows f cross join SecondRows s";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        var cte = (CteNode)inspection.LogicalPlan;
        var firstProject = (ProjectNode)((MultiStatementNode)cte.Definitions[0].Plan).Statements[0];
        var secondProject = (ProjectNode)((MultiStatementNode)cte.Definitions[1].Plan).Statements[0];
        var first = (ValuesScanNode)firstProject.Input;
        var second = (ValuesScanNode)secondProject.Input;

        Assert.HasCount(1, first.OutputSchema.Columns);
        Assert.AreEqual("First", first.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(long?), first.OutputSchema.Columns[0].Type);
        Assert.HasCount(1, second.OutputSchema.Columns);
        Assert.AreEqual("Second", second.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(int), second.OutputSchema.Columns[0].Type);
    }

    [TestMethod]
    public void ReusedAlias_ShouldBindCteDefinitionOutputSchema()
    {
        const string query = "with Parsed as (select v.Name as ExportedName from values { (Name: 'first') } v) select v.ExportedName as Name from Parsed v";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        var cte = (CteNode)inspection.LogicalPlan;
        var queryStatements = (MultiStatementNode)cte.Query;
        var project = (ProjectNode)queryStatements.Statements[0];
        var reference = (CteRefNode)project.Input;

        Assert.HasCount(1, reference.OutputSchema.Columns);
        Assert.AreEqual("ExportedName", reference.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(string), reference.OutputSchema.Columns[0].Type);
        Assert.AreEqual(cte.Definitions[0].Plan.OutputSchema, reference.OutputSchema);
    }
}
