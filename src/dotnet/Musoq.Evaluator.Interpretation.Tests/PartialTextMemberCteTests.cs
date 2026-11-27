using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class PartialTextMemberCteTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Cell", false)]
    [DataRow("Cell", true)]
    [DataRow("pattern '(?<X>a)' capture (X)", false)]
    [DataRow("pattern '(?<X>a)' capture (X)", true)]
    public void CompletedRecordBeforeLaterFailure_ShouldRetainItsTypeInRenamedCte(string member, bool reload)
    {
        var sql = $$"""
            text Cell { X: chars[1] };
            text Rec { Info: {{member}}, Tail: literal '!' };
            with Q (V) as (
                select p.ParsedFields.Info from values { (S: 'a?') } s cross apply PartialParse<Rec>(s.S) p
            )
            select q.V.X from Q q
            """;

        using var query = reload ? LoadStoredQuery(sql) : CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"]);
    }

    private CompiledQuery LoadStoredQuery(string sql)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(CreateSingleSource());
        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(
            sql,
            System.Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions
        );
        Assert.IsTrue(artifact.Succeeded, string.Join("\n", artifact.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            sql,
            artifact.Artifact!,
            provider,
            LoggerResolver,
            TestCompilationOptions
        );
        Assert.IsTrue(loaded.Succeeded, string.Join("\n", loaded.Diagnostics));
        return loaded.CompiledQuery!;
    }
}
