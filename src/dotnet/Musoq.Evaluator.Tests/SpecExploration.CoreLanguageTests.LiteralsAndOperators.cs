using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    #region §2.5 String Literals and Escape Sequences

    [TestMethod]
    public void Spec_StringLiteral_BackslashEscape_ShouldReturnBackslash()
    {
        var query = @"select '\\' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("\\", table[0][0]);
    }

    [TestMethod]
    public void Spec_StringLiteral_SingleQuoteEscape_ShouldReturnSingleQuote()
    {
        var query = @"select '\'' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("'", table[0][0]);
    }

    [TestMethod]
    public void Spec_StringLiteral_NewlineEscape_ShouldReturnNewline()
    {
        var query = @"select '\n' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("\n", table[0][0]);
    }

    [TestMethod]
    public void Spec_StringLiteral_UnicodeEscape_ShouldReturnCorrectCharacter()
    {
        var query = @"select '\u0041' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
    }

    [TestMethod]
    public void Spec_StringLiteral_HexEscape_ShouldReturnCorrectCharacter()
    {
        var query = @"select '\x41' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
    }

    [TestMethod]
    public void Spec_StringLiteral_CombinedEscapes_ShouldReturnCorrectString()
    {
        var query = @"select 'Hello\nWorld\t\u0394\\test' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello\nWorld\t\u0394\\test", table[0][0]);
    }

    #endregion

    #region §2.6 Numeric Literals

    [TestMethod]
    public void Spec_NumericLiteral_HexLiteral_ShouldReturn255()
    {
        TestMethodTemplate("0xFF", 255L);
    }

    [TestMethod]
    public void Spec_NumericLiteral_BinaryLiteral_ShouldReturn10()
    {
        TestMethodTemplate("0b1010", 10L);
    }

    [TestMethod]
    public void Spec_NumericLiteral_OctalLiteral_ShouldReturn63()
    {
        TestMethodTemplate("0o77", 63L);
    }

    [TestMethod]
    public void Spec_NumericLiteral_MixedBases_ShouldComputeCorrectly()
    {
        TestMethodTemplate("0xFF + 0b1010 + 0o77 + 42", 370L);
    }

    [TestMethod]
    public void Spec_NumericLiteral_DecimalWithDot_ShouldBeDecimalType()
    {
        TestMethodTemplate("3.14", 3.14m);
    }

    [TestMethod]
    public void Spec_NumericLiteral_LeadingDot_ShouldBeDecimalType()
    {
        TestMethodTemplate(".5", 0.5m);
    }

    #endregion

    #region §2.9 Arithmetic Operators and Precedence

    [TestMethod]
    public void Spec_Arithmetic_DivisionBeforeAddition()
    {
        TestMethodTemplate("256 + 256 / 2", 384);
    }

    [TestMethod]
    public void Spec_Arithmetic_ParenthesesOverridePrecedence()
    {
        TestMethodTemplate("(256 + 256) / 2", 256);
    }

    [TestMethod]
    public void Spec_Arithmetic_ComplexExpression()
    {
        TestMethodTemplate("1 + 2 * 3 * (7 * 8) - (45 - 10)", 302);
    }

    [TestMethod]
    public void Spec_Arithmetic_UnaryMinus()
    {
        TestMethodTemplate("1 - -1", 2);
    }

    [TestMethod]
    public void Spec_Arithmetic_UnaryMinusWithGroupedExpression()
    {
        TestMethodTemplate("1 - -(1 + 2)", 4);
    }

    [TestMethod]
    public void Spec_Arithmetic_NegativeInParentheses()
    {
        TestMethodTemplate("1 + (-2)", -1);
    }

    [TestMethod]
    public void Spec_Arithmetic_Modulo()
    {
        TestMethodTemplate("10 % 3", 1);
    }

    #endregion
}
