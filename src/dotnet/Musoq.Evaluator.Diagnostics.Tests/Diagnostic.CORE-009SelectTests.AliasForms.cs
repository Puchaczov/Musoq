using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class DiagnosticCore009SelectTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0179")]
    public void ProjectionAliasFormsExposeNames()
    {
        const string query = "select p.Value + 1 PlusOne, p.Value + 1 as AlsoPlusOne, p.Value + 1 [Bracket Plus One], p.Value from values { (Value: 1) } p";
        using var compiled = CompileAliasForms(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        AssertAliasFormsResult(result);
    }

    [TestMethod]
    public void ExplicitProjectionAliasForms_ShouldExposeDeclaredNamesAndValues()
    {
        const string query = "select p.Value + 1 PlusOne, p.Value + 1 as AlsoPlusOne, p.Value + 1 [Bracket Plus One], p.Value as Value from values { (Value: 1) } p";
        using var compiled = CompileAliasForms(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        AssertAliasFormsResult(result);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0185")]
    public void DuplicateAliasesResolveFirstProjection()
    {
        const string query = "select p.Left as Same, p.Right as Same from values { (Left: 1, Right: 2) } p where Same = 1";
        using var compiled = CompileAliasForms(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Same", typeof(int)), ("Same", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 1, 2 });
    }

    [TestMethod]
    public void DuplicateAliasesPredicate_ShouldUseFirstProjectionValue()
    {
        const string query = "select p.Left as Same, p.Right as Same from values { (Left: 1, Right: 2) } p where Same = 2";
        using var compiled = CompileAliasForms(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, result.Count);
    }

    private static void AssertAliasFormsResult(Musoq.Evaluator.Tables.Table result)
    {
        TableMaterializationTestHelper.AssertColumns(result,
            ("PlusOne", typeof(int)), ("AlsoPlusOne", typeof(int)),
            ("Bracket Plus One", typeof(int)), ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 2, 2, 2, 1 });
    }

    private CompiledQuery CompileAliasForms(string query)
    {
        return InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
    }
}
