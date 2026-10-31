using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    private sealed partial class TestTextInterpreter
    {
        public System.Text.RegularExpressions.Match TestReadPatternMatch(ReadOnlySpan<char> text, string pattern,
            string? fieldName = null, bool greedy = false, bool lazy = false)
            => ReadPatternMatch(text, pattern, fieldName, greedy, lazy);
    }

    [TestMethod]
    public void ReadPatternMatch_ConcurrentReuse_KeepsPositionsAndCapturesLocal()
    {
        Parallel.For(0, 32, static _ => AssertRepeatedCaptures());
    }

    [TestMethod]
    public void ReadPatternMatch_ReusedPattern_PreservesGreediness()
    {
        var interpreter = new TestTextInterpreter();
        var first = ReadGreediness(interpreter);
        var second = ReadGreediness(interpreter);
        Assert.AreEqual(("a-b-", "a-"), first);
        Assert.AreEqual(("a-b-", "a-"), second);
    }

    [TestMethod]
    [DataRow("[", "invalid")]
    [DataRow(@"(?=a)a", "unsupported")]
    public void ReadPatternMatch_InvalidPattern_RecreatesCurrentDiagnostic(string pattern, string diagnostic)
    {
        var interpreter = new TestTextInterpreter();
        var first = ReadPatternFailure(interpreter, pattern, 1);
        var second = ReadPatternFailure(interpreter, pattern, 2);
        Assert.AreEqual(("TestSchema", "Field1", 1), (first.SchemaName, first.FieldName, first.Position));
        Assert.AreEqual(("TestSchema", "Field2", 2), (second.SchemaName, second.FieldName, second.Position));
        Assert.Contains(diagnostic, first.Details);
        Assert.Contains(diagnostic, second.Details);
        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void ReadPatternMatch_CachedPatternMismatch_ReportsCurrentPosition()
    {
        var interpreter = new TestTextInterpreter();
        Assert.AreEqual("123", interpreter.TestReadPatternMatch("123".AsSpan(), @"\d+", "First").Value);
        interpreter.SetPosition(2);
        var exception = Assert.Throws<ParseException>(() =>
            interpreter.TestReadPatternMatch("--abc".AsSpan(), @"\d+", "Second"));
        Assert.AreEqual(ParseErrorCode.PatternMismatch, exception.ErrorCode);
        Assert.AreEqual("Second", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
    }

    private static void AssertRepeatedCaptures()
    {
        var interpreter = new TestTextInterpreter();
        for (var iteration = 0; iteration < 8; iteration++)
        {
            interpreter.SetPosition(2);
            var match = interpreter.TestReadPatternMatch("--abc:123!".AsSpan(),
                @"\G(?<word>[a-z]+):(?<number>\d+)", "Value");
            Assert.AreEqual("abc", match.Groups["word"].Value);
            Assert.AreEqual("123", match.Groups["number"].Value);
            Assert.AreEqual(9, interpreter.GetPosition());
        }
    }

    private static (string Greedy, string Lazy) ReadGreediness(TestTextInterpreter interpreter)
    {
        interpreter.SetPosition(0);
        var greedy = interpreter.TestReadPatternMatch("a-b-c".AsSpan(), ".*-", greedy: true).Value;
        interpreter.SetPosition(0);
        return (greedy, interpreter.TestReadPatternMatch("a-b-c".AsSpan(), ".*-", lazy: true).Value);
    }

    private static ParseException ReadPatternFailure(TestTextInterpreter interpreter, string pattern, int position)
    {
        interpreter.SetPosition(position);
        return Assert.Throws<ParseException>(() =>
            interpreter.TestReadPatternMatch("aaaa".AsSpan(), pattern, $"Field{position}"));
    }
}
