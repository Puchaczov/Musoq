using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ColumnInstanceMethodDiagnosticTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select NullableValue.ToString() from #A.Entities()", DiagnosticCode.MQ3117_UnsupportedColumnInstanceMethod)]
    [DataRow("select UnknownOwner.ToString() from #A.Entities()", DiagnosticCode.MQ3015_UnknownAlias)]
    [DataRow("select Self.ToString() from #A.Entities() a inner join #B.Entities() b on a.Name = b.Name", DiagnosticCode.MQ3002_AmbiguousColumn)]
    [DataRow("select Self.ToString() from values { (Marker: 1) } m left join #A.Entities() e on m.Marker = e.Id", DiagnosticCode.MQ3117_UnsupportedColumnInstanceMethod)]
    public void MethodOwner_ShouldDistinguishColumnUnknownAndAmbiguousPrefixes(string query, DiagnosticCode code)
    {
        var failure = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Sources()));
        MusoqExceptionAssertions.AssertSingleError(failure, code, DiagnosticPhase.Bind);
        MusoqExceptionAssertions.AssertHasGuidance(failure);
    }

    [TestMethod]
    [DataRow("select Self.GetPopulation() as Population from #A.Entities() Self")]
    [DataRow("select a.GetPopulation() as Population from #A.Entities() a")]
    public void VisibleSourceAlias_ShouldContinueToResolveRegisteredMethods(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(7m, result[0].ReadValues()[0]);
    }

    [TestMethod]
    public void ObjectColumnProperty_ShouldContinueToSupportNavigation()
    {
        using var compiled = CreateAndRunVirtualMachine("select Self.Name as Name from #A.Entities()", Sources());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual("Alice", result[0].ReadValues()[0]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources() => new()
    {
        ["#A"] = [new BasicEntity { Name = "Alice", Population = 7m }],
        ["#B"] = [new BasicEntity { Name = "Alice" }]
    };
}
