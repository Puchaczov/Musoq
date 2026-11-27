using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class CoreLibraryResultTests : BasicEntityTestBase
{
    private void AssertRow(string expressions, params object?[] expected)
    {
        var sql = $"select {expressions} from values {{(seed: 0)}} v";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected);
    }

    [TestMethod]
    public void Contains_ShouldMatchOrdinalIgnoreCaseAndPreserveNull()
    {
        AssertRow(
            "Contains('aBCab','bc') as C0, Contains('aBCab','xy') as C1, Contains(null::string,'x') as C2, Contains('x',null::string) as C3",
            true,
            false,
            null,
            null
        );
    }

    [TestMethod]
    public void StartsWith_ShouldMatchOnlyThePrefixAndPreserveNull()
    {
        AssertRow(
            "StartsWith('aBCab','Abc') as C0, StartsWith('aBCab','bc') as C1, StartsWith(null::string,'x') as C2, StartsWith('x',null::string) as C3",
            true,
            false,
            null,
            null
        );
    }

    [TestMethod]
    public void EndsWith_ShouldMatchOnlyTheSuffixAndPreserveNull()
    {
        AssertRow(
            "EndsWith('aBCab','CAB') as C0, EndsWith('aBCab','bc') as C1, EndsWith(null::string,'x') as C2, EndsWith('x',null::string) as C3",
            true,
            false,
            null,
            null
        );
    }

    [TestMethod]
    public void Replace_ShouldReplaceEveryMatchAndRetainInputForMissingReplacementArguments()
    {
        AssertRow(
            "Replace('aBcA','a','x') as C0, Replace('aBcA',null::string,'x') as C1, Replace('aBcA','a',null::string) as C2, Replace(null::string,'a','x') as C3",
            "xBcx",
            "aBcA",
            "aBcA",
            null
        );
    }

    [TestMethod]
    public void IndexOf_ShouldUseZeroBasedUtf16PositionAndPreserveNull()
    {
        AssertRow(
            "IndexOf('A😀Bc','b') as C0, IndexOf('A😀Bc','xy') as C1, IndexOf(null::string,'x') as C2, IndexOf('x',null::string) as C3",
            3,
            -1,
            null,
            null
        );
    }

    [TestMethod]
    public void ToLower_ShouldUseInvariantCasingAndPreserveNull()
    {
        AssertRow("ToLower('ŻÓŁĆ') as C0, ToLower('') as C1, ToLower(null::string) as C2", "żółć", "", null);
    }

    [TestMethod]
    public void ToUpper_ShouldUseInvariantCasingAndPreserveNull()
    {
        AssertRow("ToUpper('żółć') as C0, ToUpper('') as C1, ToUpper(null::string) as C2", "ŻÓŁĆ", "", null);
    }

    [TestMethod]
    public void Substring_ShouldUsePrefixLengthOrZeroBasedUtf16IndexAndLength()
    {
        AssertRow(
            """
            Substring('A😀B',1) as C0,
            Substring('A😀B',2) as C1,
            Substring('A😀B',3) as C2,
            Substring('A😀B',1,2) as C3,
            Substring('A😀B',3,20) as C4,
            Substring('A😀B',null::int) as C5,
            Substring('A😀B',null::int,1) as C6,
            Substring('A😀B',1,null::int) as C7,
            Substring(null::string,1) as C8,
            Substring(null::string,1,2) as C9
            """,
            "A",
            "A\uD83D",
            "A😀",
            "😀",
            "B",
            null,
            null,
            null,
            null,
            null
        );
    }

    [TestMethod]
    [DataRow("sbyte", "2A00")]
    [DataRow("byte", "2A00")]
    [DataRow("short", "2A00")]
    [DataRow("ushort", "2A00")]
    [DataRow("int", "2A000000")]
    [DataRow("uint", "2A000000")]
    [DataRow("long", "2A00000000000000")]
    [DataRow("ulong", "2A00000000000000")]
    [DataRow("decimal", "2A000000000000000000000000000000")]
    [DataRow("float", "00002842")]
    [DataRow("double", "0000000000004540")]
    public void ToHexNumericOverloads_ShouldPreserveUnderlyingRepresentationAndTypedNulls(
        string typeName,
        string expected
    )
    {
        var sql = $"ToHex(42::{typeName}) as Value, ToHex(null::{typeName}) as Missing";
        AssertRow(sql, expected, null);
    }

    [TestMethod]
    [DataRow("sbyte", "101010")]
    [DataRow("byte", "101010")]
    [DataRow("short", "101010")]
    [DataRow("ushort", "101010")]
    [DataRow("int", "101010")]
    [DataRow("uint", "101010")]
    [DataRow("long", "101010")]
    [DataRow("ulong", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("decimal", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("float", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("double", "100000001000101000000000000000000000000000000000000000000000000")]
    public void ToBinNumericOverloads_ShouldPreserveUnderlyingRepresentationAndTypedNulls(
        string typeName,
        string expected
    )
    {
        var sql = $"ToBin(42::{typeName}) as Value, ToBin(null::{typeName}) as Missing";
        AssertRow(sql, expected, null);
    }

    [TestMethod]
    [DataRow("sbyte", "52")]
    [DataRow("byte", "52")]
    [DataRow("short", "52")]
    [DataRow("ushort", "52")]
    [DataRow("int", "52")]
    [DataRow("uint", "52")]
    [DataRow("long", "52")]
    [DataRow("ulong", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("decimal", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("float", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("double", "401050000000000000000")]
    public void ToOctaNumericOverloads_ShouldPreserveUnderlyingRepresentationAndTypedNulls(
        string typeName,
        string expected
    )
    {
        var sql = $"ToOcta(42::{typeName}) as Value, ToOcta(null::{typeName}) as Missing";
        AssertRow(sql, expected, null);
    }

    [TestMethod]
    [DataRow("sbyte", "42")]
    [DataRow("byte", "42")]
    [DataRow("short", "42")]
    [DataRow("ushort", "42")]
    [DataRow("int", "42")]
    [DataRow("uint", "42")]
    [DataRow("long", "42")]
    [DataRow("ulong", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("decimal", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("float", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("double", "4631107791820423168")]
    public void ToDecNumericOverloads_ShouldPreserveUnderlyingRepresentationAndTypedNulls(
        string typeName,
        string expected
    )
    {
        var sql = $"ToDec(42::{typeName}) as Value, ToDec(null::{typeName}) as Missing";
        AssertRow(sql, expected, null);
    }

    [TestMethod]
    public void BooleanToBytes_ShouldEncodeTrueFalseAndMissing()
    {
        AssertRow(
            "ToHex(ToBytes(true::bool)) as C0, ToHex(ToBytes(false::bool)) as C1, ToHex(ToBytes(null::bool)) as C2",
            "01",
            "00",
            null
        );
    }

    [TestMethod]
    public void Int16ToBytes_ShouldPreserveSignedAndUnsignedWidthsAndByteOrder()
    {
        AssertRow(
            "ToHex(ToBytes(258::short)) as C0, ToHex(ToBytes(32769::ushort)) as C1, ToHex(ToBytes(null::short)) as C2, ToHex(ToBytes(null::ushort)) as C3",
            "0201",
            "0180",
            null,
            null
        );
    }

    [TestMethod]
    public void Int32ToBytes_ShouldPreserveSignedAndUnsignedWidthsAndByteOrder()
    {
        AssertRow(
            "ToHex(ToBytes(16909060::int)) as C0, ToHex(ToBytes(2147483649l::uint)) as C1, ToHex(ToBytes(null::int)) as C2, ToHex(ToBytes(null::uint)) as C3",
            "04030201",
            "01000080",
            null,
            null
        );
    }

    [TestMethod]
    public void Int64ToBytes_ShouldPreserveSignedAndUnsignedWidthsAndByteOrder()
    {
        AssertRow(
            "ToHex(ToBytes(72623859790382856l::long)) as C0, ToHex(ToBytes(9223372036854775809ul::ulong)) as C1, ToHex(ToBytes(null::long)) as C2, ToHex(ToBytes(null::ulong)) as C3",
            "0807060504030201",
            "0100000000000080",
            null,
            null
        );
    }

    [TestMethod]
    public void GetBytes_ShouldSliceInUtf16BeforeEncodingUtf8AndPropagateMissingBounds()
    {
        AssertRow(
            """
            ToHex(GetBytes('AéZ',1,1)) as C0,
            ToHex(GetBytes('A😀Z',2::int,1::int)) as C1,
            ToHex(GetBytes('AZ',0,1)) as C2,
            ToHex(GetBytes(null::string,1,0)) as C3,
            ToHex(GetBytes('AZ',null::int,0)) as C4,
            ToHex(GetBytes('AZ',1,null::int)) as C5
            """,
            "C3A9",
            "F09F9880",
            "",
            null,
            null,
            null
        );
    }

    [TestMethod]
    public void ToBase64_ShouldUseByteOffsetAndLengthAndPropagateEveryMissingArgument()
    {
        AssertRow(
            """
            ToBase64(GetBytes('ABC'),1,1) as C0,
            ToBase64(GetBytes('ABC'),1::int,2::int) as C1,
            ToBase64(GetBytes('ABC'),0,0) as C2,
            ToBase64(GetBytes(null::string),0,0) as C3,
            ToBase64(GetBytes('ABC'),null::int,1) as C4,
            ToBase64(GetBytes('ABC'),1,null::int) as C5
            """,
            "Qg==",
            "QkM=",
            "",
            null,
            null,
            null
        );
    }

    [TestMethod]
    public void AggregateValues_WithoutDelimiter_ShouldRetainNullSlotsForTextAndChars()
    {
        const string sql = """
            with data as (select t, t::char as c from values {(t:'A'),(t:null),(t:'B')} v)
            select AggregateValues(t) as TextResult, AggregateValues(c) as CharResult from data
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TextResult", typeof(string)),
            ("CharResult", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A,,B", "A,,B"]);
    }

    [TestMethod]
    public void AggregateValues_WithDelimiter_ShouldSkipNullSlotsForTextAndChars()
    {
        const string sql = """
            with data as (select t, t::char as c from values {(t:'A'),(t:null),(t:'B')} v)
            select AggregateValues(t,'/') as TextResult, AggregateValues(c,'/') as CharResult from data
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TextResult", typeof(string)),
            ("CharResult", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A/B", "A/B"]);
    }
}
