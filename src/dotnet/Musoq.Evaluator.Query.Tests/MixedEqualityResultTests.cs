using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class MixedEqualityResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void CharacterMembership_ShouldMatchOneUnitTextInEitherPosition()
    {
        const string query = """
            select v.A::char in (v.B), v.B not in (v.A::char)
            from values { (A:'a',B:'a'),(A:'a',B:'ax'),(A:null,B:'a'),(A:'a',B:'a') } v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [true, false],
            [false, true],
            [null, null],
            [true, false]
        );
    }

    [TestMethod]
    [DataRow("double")]
    [DataRow("float")]
    public void DecimalMembership_ShouldPreserveDecimalsThatRoundToTheSameFloatingValue(string type)
    {
        var query = $$"""
            select v.A in (v.B::{{type}}), v.B::{{type}} not in (v.A)
            from values {
                (A:0.10000000000000000000000001d,B:0.1d), (A:0.1d,B:0.1d), (A:null,B:0.1d), (A:0.1d,B:0.1d)
            } v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [false, true],
            [true, false],
            [null, null],
            [true, false]
        );
    }

    [TestMethod]
    public void NonFiniteFloatingValue_ShouldRemainDistinctFromANullDecimal()
    {
        const string query = """
            select v.D is distinct from (v.N::double / v.Z::double),
                   (v.N::float / v.Z::float) is not distinct from v.D
            from values { (D:null,N:0,Z:0),(D:null,N:1,Z:0),(D:null,N:null,Z:0),(D:1d,N:1,Z:1) } v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [true, false],
            [true, false],
            [false, true],
            [false, true]
        );
    }

    [TestMethod]
    public void CharacterAndStringColumns_ShouldCompareCompleteValuesAndPropagateNull()
    {
        const string query = """
            select v.C::char = v.S, v.S <> v.C::char, v.C::char is not distinct from v.S
            from values { (C:'a',S:'a'), (C:'a',S:'aa'), (C:'a',S:null), (C:null,S:null), (C:'a',S:'a') } v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [true, false, true],
            [false, true, false],
            [null, null, false],
            [null, null, true],
            [true, false, true]
        );
    }

    [TestMethod]
    public void CharacterComparedToLongOrEmptyLiteral_ShouldUseTheWholeLiteral()
    {
        const string query = "select v.S[0] = 'ab', v.S[0] = '' from values { (S:'a'), (S:'') } v";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [false, false], [false, false]);
    }

    [TestMethod]
    [DataRow("v.X", "1ul")]
    [DataRow("v.X::decimal", "1::double")]
    public void MixedNumericDistinctness_ShouldUseTheSameCoercionAsEquality(string left, string right)
    {
        var query = $$"""
            select {{left}} is distinct from {{right}}, {{right}} is not distinct from {{left}}
            from values { (X:-1),(X:1),(X:null),(X:1) } v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [true, false],
            [false, true],
            [true, false],
            [false, true]
        );
    }
}
