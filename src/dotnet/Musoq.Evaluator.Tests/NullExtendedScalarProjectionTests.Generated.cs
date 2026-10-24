using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

public sealed partial class NullExtendedScalarProjectionTests
{
    [TestMethod]
    public void MissingScalarCall_ShouldRemainTypedWithoutBoxingOrReflection()
    {
        const string sql = "select Coalesce(b.Dummy, 'fallback') as F from values { (Dummy: 'x') } a left outer join values { (Dummy: 'x') } b on 1=0";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            new TestSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        Assert.Contains("Coalesce<string>", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("new string[]", inspection.GeneratedCSharpCode);
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["fallback"]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }
}
