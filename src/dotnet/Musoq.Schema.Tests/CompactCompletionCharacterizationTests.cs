using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class CompactCompletionCharacterizationTests
{
    [TestMethod]
    public void CompletionAcrossWordBoundaries_ShouldRemainIndependentAcrossParses()
    {
        var names = Enumerable.Range(0, 70).Select(index => "F" + index).ToArray();
        var layout = new ParserCompletionLayout(names);
        names[0] = "Changed";
        var parser = new SlotParser(layout);
        var first = parser.PartialInterpret(new byte[65]);
        var second = parser.PartialInterpret(new byte[1]);

        Assert.AreEqual(65, first.CompletedFields.Count);
        Assert.AreEqual(1, second.CompletedFields.Count);
        Assert.IsTrue(first.CompletedFields.Contains("F0"));
        Assert.IsTrue(first.CompletedFields.Contains("F63"));
        Assert.IsTrue(first.CompletedFields.Contains("F64"));
        Assert.IsFalse(first.CompletedFields.Contains("F65"));
        Assert.IsFalse(first.CompletedFields.Contains("Changed"));
        Assert.IsFalse(first.CompletedFields.Contains("f0"));
        Assert.IsTrue(first.CompletedFields.SetEquals(Enumerable.Range(0, 65).Select(index => "F" + index)));
        Assert.IsTrue(second.CompletedFields.IsProperSubsetOf(first.CompletedFields));
        Assert.IsTrue(first.CompletedFields.IsSupersetOf(second.CompletedFields));
        Assert.IsTrue(first.CompletedFields.Overlaps(["missing", "F64"]));
        Assert.IsFalse(first.CompletedFields.IsSubsetOf(second.CompletedFields));
        Assert.IsFalse(first.CompletedFields.IsProperSupersetOf(first.CompletedFields));
    }

    private sealed class SlotParser(ParserCompletionLayout layout)
        : BytesInterpreterBase<byte[]>,
            IParserCompletionLayoutSource
    {
        ParserCompletionLayout IParserCompletionLayoutSource.CompletionLayout => layout;
        public override string SchemaName => "Slots";

        public override byte[] InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            var result = new byte[70];
            RecordParsedValue(result);
            for (var index = 0; index < result.Length; index++)
            {
                SetCurrentField("F" + index);
                result[index] = ReadByte(data);
                RecordParsedFieldSlot(index);
            }
            return result;
        }
    }
}
