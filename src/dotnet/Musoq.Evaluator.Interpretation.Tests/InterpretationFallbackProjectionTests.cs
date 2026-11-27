using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationFallbackProjectionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void NullFallbackOverDecodedValues_ShouldPreserveMemberTypeAndNullability(bool binary, bool nullFallback)
    {
        var declarations = binary
            ? "binary A { V: byte }; binary R { D: A[1] };"
            : "text A { V: chars[1] }; text R { D: A };";
        var valid = binary ? "2a" : "a";
        var left = binary ? "TryInterpret<R>(FromHexToBytes(s.S))" : "TryParse<R>(s.S)";
        var right = binary ? "Interpret<R>(FromHexToBytes('00'))" : "Parse<R>('b')";
        var fallback = nullFallback ? "null" : "r.D";
        var additional = nullFallback ? "" : $" cross apply {right} r";
        var path = binary ? "p.X[0].V" : "p.X.V";
        var sql = $$"""
            {{declarations}}
            with P as (select l.D ?? {{fallback}} as X
                from values { (K:1,S:'{{valid}}'), (K:0,S:''), (K:0,S:'') } s
                outer apply {{left}} l{{additional}})
            select {{path}} as V from P p
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        var type = binary ? (nullFallback ? typeof(byte?) : typeof(byte)) : typeof(string);
        TableMaterializationTestHelper.AssertColumns(table, ("V", type));
        object first = binary ? (byte)42 : "a";
        object? second =
            nullFallback ? null
            : binary ? (byte)0
            : "b";
        TableMaterializationTestHelper.AssertRowsUnordered(table, [first], [second], [second]);
    }
}
