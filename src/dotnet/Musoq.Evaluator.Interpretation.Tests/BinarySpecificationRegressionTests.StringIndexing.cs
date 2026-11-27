using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("C = 'A'", true, false)]
    [DataRow("'A' <> C", false, true)]
    [DataRow("case C when 'A' then true else false end", true, false, false)]
    [DataRow("C = 'A'::Char", true, false)]
    public void InterpretedCharacterComparedToALiteral_ShouldUseCoreEquality(
        string expression,
        bool first,
        bool second,
        bool? missing = null
    )
    {
        var table = RunBinary(
            $"binary Rec {{ K: byte, S: string[1] ascii when K = 1, C: = S::Char, V: = {expression} }};",
            "r.V",
            [1, 65],
            [1, 66],
            [0],
            [0]
        );

        TableMaterializationTestHelper.AssertRowsUnordered(table, [first], [second], [missing], [missing]);
    }

    [TestMethod]
    [DataRow("0", 'A')]
    [DataRow("1", 'Ω')]
    [DataRow("-3", 'Ω')]
    [DataRow("2", '\0')]
    public void IndexingDecodedString_ShouldReturnCharacterAndPreserveTheByteCursor(string index, char expected)
    {
        var table = RunBinary(
            $"binary Rec {{ S: string[3] utf8, C: = S[{index}], Tail: byte }};",
            "r.C as Letter, r.Tail",
            [0x41, 0xCE, 0xA9, 42]
        );

        TableMaterializationTestHelper.AssertColumns(table, ("Letter", typeof(char)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected, (byte)42]);
    }

    [TestMethod]
    [DataRow("binary Pair { S: string[2] ascii };", "H: Pair when K = 1, C: = H.S[K]", "r.C")]
    [DataRow("binary Child { C: = parent.S[-1] };", "S: string[2] ascii when K = 1, H: Child", "r.H.C")]
    [DataRow("", "S: string[2] ascii when K = 1, A: = S || S, C: = A[-1]", "r.C")]
    [DataRow("", "S: string[2] ascii when K = 1", "r.S[-1]")]
    public void IndexingOptionalString_ShouldPreserveTheCharacterDefaultAcrossRecords(
        string definitions,
        string fields,
        string projection
    )
    {
        var table = RunBinary(
            $"{definitions} binary Rec {{ K: byte, {fields}, Tail: byte }};",
            $"{projection} as Letter, r.Tail",
            [1, 65, 66, 42],
            [0, 43],
            [0, 43]
        );

        TableMaterializationTestHelper.AssertColumns(table, ("Letter", typeof(char)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ['B', (byte)42], ['\0', (byte)43], ['\0', (byte)43]);
    }
}
