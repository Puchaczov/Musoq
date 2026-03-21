using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests derived directly from the Musoq Core Language Specification (musoq-core-language-spec.md)
///     and TABLE/COUPLE Specification (musoq-table-couple-spec.md).
///     These tests verify that queries constructed from the specs work correctly
///     and that malformed queries produce meaningful error messages.
/// </summary>
[TestClass]
public class SpecExplorationCoreLanguageTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region §6 FROM Clause

    [TestMethod]
    public void Spec_From_WithoutHashPrefix_ShouldBeEquivalent()
    {
        var query = "select Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    #endregion

    #region §Appendix G: Field Links (GROUP BY References)

    [TestMethod]
    public void Spec_FieldLinks_ShouldReferToGroupByColumnByPosition()
    {
        var query = "select ::1, Count(Name) from #A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);

        var countries = new HashSet<object> { table[0][0], table[1][0] };
        Assert.Contains("POLAND", countries);
        Assert.Contains("GERMANY", countries);
    }

    #endregion

    #region §5.6 RowNumber

    [TestMethod]
    public void Spec_RowNumber_Basic()
    {
        var query = "select RowNumber(), Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var rowNumbers = table.Select(r => (int)r.Values[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(1, rowNumbers[0]);
        Assert.AreEqual(2, rowNumbers[1]);
        Assert.AreEqual(3, rowNumbers[2]);
    }

    #endregion

    #region §19 String Comparison Semantics

    [TestMethod]
    public void Spec_StringComparison_EqualityIsOrdinal()
    {
        var query = "select Name from #A.Entities() where Name = 'alice'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("alice")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "= should be case-sensitive per spec");
        Assert.AreEqual("alice", table[0][0]);
    }

    #endregion

    #region §7.9 Implicit Boolean Conversion

    [TestMethod]
    public void Spec_ImplicitBoolConversion_InWhere()
    {
        var query = "select Name from #A.Entities() where Match('\\d+', Name)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test123"),
                    new BasicEntity("nope")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test123", table[0][0]);
    }

    #endregion

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

    #region §5 SELECT Clause

    [TestMethod]
    public void Spec_Select_LiteralValue()
    {
        TestMethodTemplate("1", 1);
    }

    [TestMethod]
    public void Spec_Select_ColumnReference()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice") { City = "NYC", Country = "USA", Population = 100m }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Select_QualifiedColumnReference()
    {
        var query = "select a.Name from #A.Entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Select_ArithmeticExpression()
    {
        TestMethodTemplate("1 + 2 * 3", 7);
    }

    [TestMethod]
    public void Spec_Select_ColumnAlias_WithAs()
    {
        var query = "select Name as FullName from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("FullName", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void Spec_Select_StringConcatenation()
    {
        TestMethodTemplate("'Hello' + ' ' + 'World'", "Hello World");
    }

    [TestMethod]
    public void Spec_Select_Distinct_ShouldRemoveDuplicates()
    {
        var query = "select distinct Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Select_Star_ShouldExpandAllColumns()
    {
        var query = "select * from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsGreaterThan(1, table.Columns.Count(), "Star should expand to multiple columns");
    }

    #endregion

    #region §7 WHERE Clause

    [TestMethod]
    public void Spec_Where_ComparisonGreaterThan()
    {
        var query = "select Name from #A.Entities() where Population > 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 100m },
                    new BasicEntity("b") { Population = 300m },
                    new BasicEntity("c") { Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_LogicalAnd()
    {
        var query = "select Name from #A.Entities() where Country = 'POLAND' and Population > 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND", Population = 100m },
                    new BasicEntity("b") { Country = "POLAND", Population = 300m },
                    new BasicEntity("c") { Country = "GERMANY", Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_LogicalOr()
    {
        var query = "select Name from #A.Entities() where City = 'WARSAW' or City = 'BERLIN'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "WARSAW" },
                    new BasicEntity("b") { City = "BERLIN" },
                    new BasicEntity("c") { City = "PARIS" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_IsNull()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { NullableValue = null },
                    new BasicEntity("b") { NullableValue = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_IsNotNull()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { NullableValue = null },
                    new BasicEntity("b") { NullableValue = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_Like_CaseInsensitive()
    {
        var query = "select Name from #A.Entities() where Name like '%lic%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("MALICE")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count, "LIKE should be case-insensitive, matching both 'Alice' and 'MALICE'");
    }

    [TestMethod]
    public void Spec_Where_In_SetMembership()
    {
        var query = "select Name from #A.Entities() where Population in (100, 300)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 100m },
                    new BasicEntity("b") { Population = 200m },
                    new BasicEntity("c") { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_AngleBracketForm()
    {
        var query = "select Name from #A.Entities() where Name <> 'Bob'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_ExclamationForm_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Name from #A.Entities() where Name != 'Bob'",
                sources));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ2019_InvalidOperator, DiagnosticPhase.Parse, "!=");
    }

    #endregion

    #region §8 JOIN Clause

    [TestMethod]
    public void Spec_Join_InnerJoin()
    {
        var query = @"
            select a.City, b.City
            from #A.Entities() a
            inner join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "NYC" },
                    new BasicEntity("b") { City = "LA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("c") { City = "NYC" },
                    new BasicEntity("d") { City = "SF" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NYC", table[0][0]);
    }

    [TestMethod]
    public void Spec_Join_LeftOuterJoin()
    {
        var query = @"
            select a.Name, b.Name
            from #A.Entities() a
            left outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("PersonA") { City = "NYC" },
                    new BasicEntity("PersonC") { City = "LA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("PersonB") { City = "NYC" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);


        var matchedRow = table[0][1] != null ? 0 : 1;
        var unmatchedRow = 1 - matchedRow;

        Assert.AreEqual("PersonA", table[matchedRow][0]);
        Assert.AreEqual("PersonB", table[matchedRow][1]);
        Assert.AreEqual("PersonC", table[unmatchedRow][0]);
        Assert.IsNull(table[unmatchedRow][1], "Unmatched right side should be null");
    }

    [TestMethod]
    public void Spec_Join_CrossJoinViaTautology()
    {
        var query = @"
            select a.Name, b.Name
            from #A.Entities() a
            inner join #B.Entities() b on 1 = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1"),
                    new BasicEntity("A2")
                ]
            },
            {
                "#B", [
                    new BasicEntity("B1"),
                    new BasicEntity("B2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count, "Cartesian product of 2x2 should be 4 rows");
    }

    #endregion

    #region §10 GROUP BY and Aggregation

    [TestMethod]
    public void Spec_GroupBy_Count()
    {
        var query = "select Country, Count(Country) from #A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_GroupBy_Sum()
    {
        var query = "select Country, Sum(Population) from #A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND", Population = 100m },
                    new BasicEntity("b") { Country = "POLAND", Population = 200m },
                    new BasicEntity("c") { Country = "GERMANY", Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_GroupBy_Having()
    {
        var query = @"
            select Name, Count(Name)
            from #A.Entities()
            group by Name
            having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void Spec_GroupBy_WithConstant_ShouldTreatAllRowsAsSingleGroup()
    {
        var query = "select Count(Country) from #A.Entities() group by 'fake'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "GERMANY" },
                    new BasicEntity("c") { Country = "FRANCE" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table[0][0]);
    }

    #endregion

    #region §11 Set Operations

    [TestMethod]
    public void Spec_SetOp_UnionAll()
    {
        var query = @"
            select Name from #A.Entities()
            union all (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count, "UNION ALL should preserve all rows including duplicates");
    }

    [TestMethod]
    public void Spec_SetOp_Union_ShouldDedup()
    {
        var query = @"
            select Name from #A.Entities()
            union (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count, "UNION should deduplicate: Alice, Bob, Charlie");
    }

    [TestMethod]
    public void Spec_SetOp_Except()
    {
        var query = @"
            select Name from #A.Entities()
            except (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "EXCEPT: Alice is in A but not in B");
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_SetOp_Intersect()
    {
        var query = @"
            select Name from #A.Entities()
            intersect (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] },
            { "#B", [new BasicEntity("Bob"), new BasicEntity("Charlie")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "INTERSECT: Only Bob appears in both");
        Assert.AreEqual("Bob", table[0][0]);
    }

    #endregion

    #region §12 ORDER BY, SKIP, TAKE

    [TestMethod]
    public void Spec_OrderBy_Ascending()
    {
        var query = "select Name from #A.Entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Bob", table[1][0]);
        Assert.AreEqual("Charlie", table[2][0]);
    }

    [TestMethod]
    public void Spec_OrderBy_Descending()
    {
        var query = "select Name from #A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Charlie", table[0][0]);
        Assert.AreEqual("Bob", table[1][0]);
        Assert.AreEqual("Alice", table[2][0]);
    }

    [TestMethod]
    public void Spec_Skip()
    {
        var query = "select Name from #A.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Spec_Take()
    {
        var query = "select Name from #A.Entities() take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Skip_ExceedsRowCount_ShouldReturnZeroRows()
    {
        var query = "select Name from #A.Entities() skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count, "SKIP exceeding row count should return 0 rows (no error per spec)");
    }

    [TestMethod]
    public void Spec_SkipTake_Pagination()
    {
        var query = "select Name from #A.Entities() order by Name skip 1 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Bob", table[0][0]);
    }

    #endregion

    #region §13 Common Table Expressions (CTEs)

    [TestMethod]
    public void Spec_CTE_SimpleQuery()
    {
        var query = @"
            with p as (
                select City, Country from #A.Entities()
            )
            select Country, City from p";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("POLAND", table[0][0]);
        Assert.AreEqual("WARSAW", table[0][1]);
    }

    [TestMethod]
    public void Spec_CTE_StarExpansion()
    {
        var query = @"
            with p as (
                select City, Country from #A.Entities()
            )
            select * from p";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count(), "Star expansion from CTE with 2 columns");
    }

    [TestMethod]
    public void Spec_CTE_WithJoin()
    {
        var query = @"
            with p as (select City, Country from #A.Entities())
            select p.City, b.Population
            from p
            inner join #B.Entities() b on p.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "NYC", Country = "USA" }] },
            { "#B", [new BasicEntity("b") { City = "NYC", Population = 8000000m }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NYC", table[0][0]);
        Assert.AreEqual(8000000m, table[0][1]);
    }

    [TestMethod]
    public void Spec_CTE_WithSetOperation()
    {
        var query = @"
            with combined as (
                select Name from #A.Entities()
                union all (Name)
                select Name from #B.Entities()
            )
            select * from combined";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] },
            { "#B", [new BasicEntity("Bob")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region §16 Reordered Query Syntax (FROM-first)

    [TestMethod]
    public void Spec_Reordered_SimpleFromSelect()
    {
        var query = "from #A.Entities() select City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a") { City = "WARSAW", Country = "POLAND" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0][0]);
        Assert.AreEqual("POLAND", table[0][1]);
    }

    [TestMethod]
    public void Spec_Reordered_WithWhere()
    {
        var query = "from #A.Entities() where Country = 'POLAND' select City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "WARSAW", Country = "POLAND" },
                    new BasicEntity("b") { City = "BERLIN", Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0][0]);
    }

    [TestMethod]
    public void Spec_Reordered_WithGroupBy()
    {
        var query = "from #A.Entities() group by Country select Country, Count(Country)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region §18 NULL Semantics

    [TestMethod]
    public void Spec_Null_Propagation_AdditionWithNull()
    {
        var query = "select null + 1 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_SubtractionWithNull()
    {
        var query = "select 10 - null from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_MultiplicationWithNull()
    {
        var query = "select null * 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_DivisionWithNull()
    {
        var query = "select null / 2 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_ModuloWithNull()
    {
        var query = "select null % 3 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void Spec_Null_Propagation_NullPlusNull()
    {
        var query = "select null + null from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region §20 Array and Property Access

    [TestMethod]
    public void Spec_ArrayIndexing_Basic()
    {
        var query = "select Array[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
    }

    [TestMethod]
    public void Spec_ArrayIndexing_Negative_ShouldWrap()
    {
        var query = "select Array[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0], "Array[-1] should return the last element");
    }

    [TestMethod]
    public void Spec_PropertyNavigation_SingleLevel()
    {
        var query = "select Self.Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_PropertyNavigation_TwoLevels()
    {
        var query = "select Self.Self.Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    #endregion

    #region §Appendix F: CASE WHEN

    [TestMethod]
    public void Spec_CaseWhen_MultiBranch_StringEquality()
    {
        var query = @"
            select
                City,
                case
                    when City = 'Warsaw' then 'capital'
                    when City = 'Katowice' then 'silesia'
                    else 'other'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 1000),
                    new BasicEntity("Katowice", "Poland", 200),
                    new BasicEntity("Radom", "Poland", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("capital", results["Warsaw"]);
        Assert.AreEqual("silesia", results["Katowice"]);
        Assert.AreEqual("other", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_SingleBranch_NumericComparison()
    {
        var query = @"
            select
                Name,
                case
                    when Id > 100 then 'large'
                    else 'small'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw") { Id = 1000 },
                    new BasicEntity("Radom") { Id = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("large", results["Warsaw"]);
        Assert.AreEqual("small", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_MultiBranch_DecimalComparison()
    {
        var query = @"
            select
                City,
                case
                    when Population >= 500d then 'large'
                    when Population >= 100d then 'medium'
                    else 'small'
                end
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 1000),
                    new BasicEntity("Katowice", "Poland", 200),
                    new BasicEntity("Radom", "Poland", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var results = table.ToDictionary(r => (string)r.Values[0], r => (string)r.Values[1]);
        Assert.AreEqual("large", results["Warsaw"]);
        Assert.AreEqual("medium", results["Katowice"]);
        Assert.AreEqual("small", results["Radom"]);
    }

    [TestMethod]
    public void Spec_CaseWhen_InArithmetic()
    {
        TestMethodTemplate("1 + (case when 2 > 1 then 1 else 0 end) - 1", 1);
    }

    #endregion
}
