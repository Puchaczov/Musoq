using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ScalarOperatorRowTests : BasicEntityTestBase
{
    [TestMethod]
    public void BitwiseAnd_ShouldPreserveColumnValuesAndNull()
    {
        const string sql =
            "select id as Id, a & b as Bits from values {(id:1,a:12,b:10),(id:2,a:null,b:10),(id:3,a:12,b:12)} v order by id";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Bits", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 8], [2, null], [3, 12]);
    }

    [TestMethod]
    public void BitwiseOr_ShouldPreserveColumnValuesAndNull()
    {
        const string sql =
            "select id as Id, a | b as Bits from values {(id:1,a:12,b:10),(id:2,a:null,b:10),(id:3,a:12,b:12)} v order by id";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Bits", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 14], [2, null], [3, 12]);
    }

    [TestMethod]
    public void BitwiseXor_ShouldPreserveColumnValuesAndNull()
    {
        const string sql =
            "select id as Id, a ^ b as Bits from values {(id:1,a:12,b:10),(id:2,a:null,b:10),(id:3,a:12,b:12)} v order by id";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Bits", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 6], [2, null], [3, 0]);
    }

    [TestMethod]
    public void DeclarationBoundary_ShouldExecuteTheFollowingStatementAndPreserveItsValue()
    {
        const string sql = "let n: int = 41; select $n + 1 as Result from values {(seed:0)} v";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Result", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [42]);
    }

    [TestMethod]
    public void OmittedScalarType_ShouldIdentifyAnInvalidStructuralInitializer()
    {
        const string sql = "let n = 41; select $n + 1 from values {(seed:0)} v";
        var failure = Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>())
        );
        Assert.AreEqual(
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
            failure.PrimaryEnvelope.Code
        );
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
    }

    [TestMethod]
    public void ValuesExpressions_ShouldUseScriptValuesAndArithmeticPrecedence()
    {
        const string sql =
            "let n: int = 3; select id as Id, a as Result from values {(id:1,a:$n+2*4),(id:2,a:($n+2)*4)} v order by id";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Result", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 11], [2, 20]);
    }

    [TestMethod]
    public void InequalitySpellings_ShouldPreserveFalseTrueAndUnknown()
    {
        const string sql =
            "select id as Id, v.x <> 1 as First, v.x != 1 as Second from values {(id:1,x:1),(id:2,x:2),(id:3,x:null)} v order by id";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("First", typeof(bool?)),
            ("Second", typeof(bool?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, false, false], [2, true, true], [3, null, null]);
    }
}
