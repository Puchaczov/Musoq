using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Characterization tests that document the current behavior of null handling
/// across various operators in Musoq SQL expressions.
/// 
/// These tests capture "what the system actually does" rather than "what it should do".
/// They serve as a regression safety net and documentation of null semantics.
/// 
/// SUMMARY OF SURPRISING FINDINGS (differences from SQL standard):
/// 
/// 1. ARITHMETIC OPERATORS (ALL THROW EXCEPTIONS):
///    - All arithmetic operations (+, -, *, /, %) with nullable int throw
///      InvalidOperationException when any operand is null
///    - This differs from SQL standard where null propagates (null + 5 = null)
/// 
/// 2. EQUALITY COMPARISONS:
///    - null = null → returns true (row included) - differs from SQL (unknown)
///    - null = value → filters row (correct SQL behavior)
///    - null <> value → returns true (row included) - differs from SQL (unknown)
/// 
/// 3. STRING CONCATENATION:
///    - null + 'string' → returns 'string' (null treated as empty string)
///    - 'string' + null → returns 'string' (null treated as empty string)
///    - This differs from SQL standard where null + string = null
/// 
/// 4. NOT IN OPERATOR:
///    - null NOT IN (1, 2, 3) → returns true (row included)
///    - This differs from SQL standard where result is unknown
/// 
/// 5. NOT LIKE OPERATOR:
///    - null NOT LIKE 'pattern' → returns true (row included)
///    - NOT of false = true, but semantically should be unknown for null
/// 
/// STANDARD-COMPLIANT BEHAVIORS:
/// - IS NULL / IS NOT NULL work correctly
/// - null = value filters row (returns unknown/false)
/// - LIKE with null content/pattern returns false
/// - IN with null value filters row
/// - Comparison operators (>, <, >=, <=) with null filter row
/// - AND/OR logical operators follow three-valued logic
/// - COALESCE works correctly
/// - CASE WHEN with null checks works correctly
/// </summary>
[TestClass]
public class NullOperatorCharacterizationTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Arithmetic Operators with Nullable Int (NullableValue)

    /// <summary>
    /// Tests arithmetic addition with null on left side: null + value
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// NOTE: Arithmetic operations with nullable int throw exceptions when null
    /// </summary>
    [TestMethod]
    public void Arithmetic_Addition_NullPlusValue_ThrowsException()
    {
        var query = "select NullableValue + 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Arithmetic with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic addition with null on right side: value + null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Addition_ValuePlusNull_ThrowsException()
    {
        var query = "select 5 + NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Arithmetic with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic addition with both sides null: null + null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Addition_NullPlusNull_ThrowsException()
    {
        // Using two separate nullable fields both set to null
        var query = "select NullableValue + NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Arithmetic with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic subtraction with null: null - value
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// NOTE: All arithmetic operators with null throw exceptions
    /// </summary>
    [TestMethod]
    public void Arithmetic_Subtraction_NullMinusValue_ThrowsException()
    {
        var query = "select NullableValue - 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // All arithmetic operations with null throw exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic subtraction with null: value - null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Subtraction_ValueMinusNull_ThrowsException()
    {
        var query = "select 5 - NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Subtraction with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic multiplication with null: null * value
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Multiplication_NullTimesValue_ThrowsException()
    {
        var query = "select NullableValue * 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Multiplication with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic multiplication with null: value * null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Multiplication_ValueTimesNull_ThrowsException()
    {
        var query = "select 5 * NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Multiplication with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic division with null dividend: null / value
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Division_NullDividedByValue_ThrowsException()
    {
        var query = "select NullableValue / 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Division with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic division with null divisor: value / null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Division_ValueDividedByNull_ThrowsException()
    {
        var query = "select 5 / NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Division with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic modulo with null: null % value
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Modulo_NullModValue_ThrowsException()
    {
        var query = "select NullableValue % 3 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Modulo with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Tests arithmetic modulo with null: value % null
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public void Arithmetic_Modulo_ValueModNull_ThrowsException()
    {
        var query = "select 10 % NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // SURPRISING: Modulo with null throws exception
        Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    /// Verifies arithmetic operations work correctly with non-null values
    /// </summary>
    [TestMethod]
    public void Arithmetic_Addition_WithNonNullValue_ReturnsCorrectResult()
    {
        var query = "select NullableValue + 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = 10 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15, table[0][0], "10 + 5 should return 15");
    }

    #endregion

    #region Comparison Operators with Null

    /// <summary>
    /// Tests equality comparison: null = value
    /// SQL standard: null = anything returns unknown (treated as false in WHERE)
    /// </summary>
    [TestMethod]
    public void Comparison_Equals_NullEqualsValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue = 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null = 5 should filter out the row (SQL: unknown/false)");
    }

    /// <summary>
    /// Tests inequality comparison: null <> value
    /// CURRENT BEHAVIOR: null <> value returns true (row is included)
    /// NOTE: This differs from SQL standard where null <> anything returns unknown/false
    /// </summary>
    [TestMethod]
    public void Comparison_NotEquals_NullNotEqualsValue_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue <> 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // SURPRISING: Musoq treats null <> 5 as true, returning the row
        Assert.AreEqual(1, table.Count, "CURRENT: null <> 5 returns the row (differs from SQL standard)");
    }

    /// <summary>
    /// Tests greater than comparison: null > value
    /// </summary>
    [TestMethod]
    public void Comparison_GreaterThan_NullGreaterThanValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue > 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null > 5 should filter out the row");
    }

    /// <summary>
    /// Tests less than comparison: null < value
    /// </summary>
    [TestMethod]
    public void Comparison_LessThan_NullLessThanValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue < 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null < 5 should filter out the row");
    }

    /// <summary>
    /// Tests greater than or equal comparison: null >= value
    /// </summary>
    [TestMethod]
    public void Comparison_GreaterOrEqual_NullGreaterOrEqualValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue >= 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null >= 5 should filter out the row");
    }

    /// <summary>
    /// Tests less than or equal comparison: null <= value
    /// </summary>
    [TestMethod]
    public void Comparison_LessOrEqual_NullLessOrEqualValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue <= 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null <= 5 should filter out the row");
    }

    /// <summary>
    /// Tests equality comparison: null = null
    /// CURRENT BEHAVIOR: null = null returns true (row is included)
    /// NOTE: This differs from SQL standard where null = null returns unknown
    /// </summary>
    [TestMethod]
    public void Comparison_Equals_NullEqualsNull_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue = NullableValue";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // SURPRISING: Musoq treats null = null as true (C# behavior)
        Assert.AreEqual(1, table.Count, "CURRENT: null = null returns the row (differs from SQL standard)");
    }

    /// <summary>
    /// Verifies comparison with non-null values works correctly
    /// </summary>
    [TestMethod]
    public void Comparison_Equals_NonNullEqualsValue_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue = 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = 5 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "5 = 5 should return the row");
        Assert.AreEqual("Test", table[0][0]);
    }

    #endregion

    #region String Comparison with Null

    /// <summary>
    /// Tests string equality with null: null string = 'value'
    /// </summary>
    [TestMethod]
    public void Comparison_StringEquals_NullEqualsValue_FiltersRow()
    {
        var query = "select City from #A.Entities() where Name = 'Test'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null, City = "City1" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null = 'Test' should filter out the row");
    }

    /// <summary>
    /// Tests string equality: 'value' = null string
    /// </summary>
    [TestMethod]
    public void Comparison_StringEquals_ValueEqualsNull_FiltersRow()
    {
        var query = "select Name from #A.Entities() where 'Test' = City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Name1", City = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "'Test' = null should filter out the row");
    }

    #endregion

    #region Logical Operators (AND/OR) with Null

    /// <summary>
    /// Tests AND with true condition and null comparison
    /// true AND unknown = unknown (false)
    /// </summary>
    [TestMethod]
    public void Logical_And_TrueAndNullComparison_FiltersRow()
    {
        var query = "select Name from #A.Entities() where 1 = 1 and NullableValue = 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "true AND (null = 5) should filter the row");
    }

    /// <summary>
    /// Tests AND with null comparison and true condition
    /// unknown AND true = unknown (false)
    /// </summary>
    [TestMethod]
    public void Logical_And_NullComparisonAndTrue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue = 5 and 1 = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "(null = 5) AND true should filter the row");
    }

    /// <summary>
    /// Tests OR with true condition and null comparison
    /// true OR unknown = true
    /// </summary>
    [TestMethod]
    public void Logical_Or_TrueOrNullComparison_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where 1 = 1 or NullableValue = 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "true OR (null = 5) should return the row");
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    /// Tests OR with null comparison and true condition
    /// unknown OR true = true
    /// </summary>
    [TestMethod]
    public void Logical_Or_NullComparisonOrTrue_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue = 5 or 1 = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "(null = 5) OR true should return the row");
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    /// Tests OR with false condition and null comparison
    /// false OR unknown = unknown (false)
    /// </summary>
    [TestMethod]
    public void Logical_Or_FalseOrNullComparison_FiltersRow()
    {
        var query = "select Name from #A.Entities() where 1 = 2 or NullableValue = 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "false OR (null = 5) should filter the row");
    }

    #endregion

    #region String Operators with Null

    /// <summary>
    /// Tests string concatenation with null: null + 'value'
    /// CURRENT BEHAVIOR: null + string = string (null is treated as empty string)
    /// NOTE: This differs from SQL standard where null + string = null
    /// </summary>
    [TestMethod]
    public void String_Concatenation_NullPlusString_ReturnsString()
    {
        var query = "select Name + ' suffix' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        // SURPRISING: Musoq treats null as empty string for concatenation
        Assert.AreEqual(" suffix", table[0][0], "CURRENT: null + ' suffix' returns ' suffix' (null treated as empty)");
    }

    /// <summary>
    /// Tests string concatenation with null: 'value' + null
    /// CURRENT BEHAVIOR: string + null = string (null is treated as empty string)
    /// NOTE: This differs from SQL standard where string + null = null
    /// </summary>
    [TestMethod]
    public void String_Concatenation_StringPlusNull_ReturnsString()
    {
        var query = "select 'prefix ' + Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        // SURPRISING: Musoq treats null as empty string for concatenation
        Assert.AreEqual("prefix ", table[0][0], "CURRENT: 'prefix ' + null returns 'prefix ' (null treated as empty)");
    }

    /// <summary>
    /// Tests LIKE with null content: null LIKE 'pattern'
    /// </summary>
    [TestMethod]
    public void String_Like_NullLikePattern_FiltersRow()
    {
        var query = "select City from #A.Entities() where Name like '%test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null, City = "City1" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null LIKE '%test%' should filter the row");
    }

    /// <summary>
    /// Tests LIKE with null pattern: 'content' LIKE null
    /// </summary>
    [TestMethod]
    public void String_Like_ContentLikeNullPattern_FiltersRow()
    {
        var query = "select Name from #A.Entities() where Name like City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "'Test' LIKE null should filter the row");
    }

    /// <summary>
    /// Tests NOT LIKE with null content: null NOT LIKE 'pattern'
    /// NOT false = true, but null NOT LIKE x should also be unknown
    /// </summary>
    [TestMethod]
    public void String_NotLike_NullNotLikePattern_ReturnsRow()
    {
        var query = "select City from #A.Entities() where Name not like '%test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null, City = "City1" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // NOT (null LIKE pattern) = NOT false = true
        // This is interesting behavior - "not like" with null may return the row
        Assert.AreEqual(1, table.Count, "null NOT LIKE '%test%' should return the row (NOT false = true)");
    }

    #endregion

    #region Special Operators: IS NULL / IS NOT NULL

    /// <summary>
    /// Tests IS NULL with null value
    /// </summary>
    [TestMethod]
    public void Special_IsNull_WithNullValue_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "null IS NULL should return the row");
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    /// Tests IS NULL with non-null value
    /// </summary>
    [TestMethod]
    public void Special_IsNull_WithNonNullValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = 5 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "5 IS NULL should filter the row");
    }

    /// <summary>
    /// Tests IS NOT NULL with null value
    /// </summary>
    [TestMethod]
    public void Special_IsNotNull_WithNullValue_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null IS NOT NULL should filter the row");
    }

    /// <summary>
    /// Tests IS NOT NULL with non-null value
    /// </summary>
    [TestMethod]
    public void Special_IsNotNull_WithNonNullValue_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = 5 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "5 IS NOT NULL should return the row");
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    /// Tests IS NULL with null string
    /// </summary>
    [TestMethod]
    public void Special_IsNull_StringWithNull_ReturnsRow()
    {
        var query = "select City from #A.Entities() where Name is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null, City = "City1" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "null string IS NULL should return the row");
        Assert.AreEqual("City1", table[0][0]);
    }

    #endregion

    #region IN / NOT IN Operators with Null

    /// <summary>
    /// Tests IN with null value: null IN (1, 2, 3)
    /// </summary>
    [TestMethod]
    public void Special_In_NullInList_FiltersRow()
    {
        var query = "select Name from #A.Entities() where NullableValue in (1, 2, 3)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null IN (1, 2, 3) should filter the row");
    }

    /// <summary>
    /// Tests NOT IN with null value: null NOT IN (1, 2, 3)
    /// CURRENT BEHAVIOR: null NOT IN list returns true (row is included)
    /// NOTE: This differs from SQL standard where null NOT IN list is unknown
    /// </summary>
    [TestMethod]
    public void Special_NotIn_NullNotInList_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue not in (1, 2, 3)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // SURPRISING: Musoq treats null NOT IN list as true
        Assert.AreEqual(1, table.Count, "CURRENT: null NOT IN (1, 2, 3) returns the row (differs from SQL standard)");
    }

    /// <summary>
    /// Tests IN with matching value
    /// </summary>
    [TestMethod]
    public void Special_In_ValueInList_ReturnsRow()
    {
        var query = "select Name from #A.Entities() where NullableValue in (1, 2, 3)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { NullableValue = 2 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "2 IN (1, 2, 3) should return the row");
    }

    /// <summary>
    /// Tests IN with null string
    /// </summary>
    [TestMethod]
    public void Special_In_NullStringInList_FiltersRow()
    {
        var query = "select City from #A.Entities() where Name in ('a', 'b', 'c')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null, City = "City1" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null string IN ('a', 'b', 'c') should filter the row");
    }

    #endregion

    #region COALESCE Function Behavior

    /// <summary>
    /// Tests COALESCE with null first argument
    /// </summary>
    [TestMethod]
    public void Coalesce_NullFirst_ReturnsSecond()
    {
        var query = "select Coalesce(NullableValue, 99) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(99, table[0][0], "COALESCE(null, 99) should return 99");
    }

    /// <summary>
    /// Tests COALESCE with non-null first argument
    /// </summary>
    [TestMethod]
    public void Coalesce_NonNullFirst_ReturnsFirst()
    {
        var query = "select Coalesce(NullableValue, 99) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = 42 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0], "COALESCE(42, 99) should return 42");
    }

    /// <summary>
    /// Tests COALESCE with null string
    /// </summary>
    [TestMethod]
    public void Coalesce_NullString_ReturnsDefault()
    {
        var query = "select Coalesce(Name, 'default') from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("default", table[0][0], "COALESCE(null, 'default') should return 'default'");
    }

    #endregion

    #region CASE WHEN with Null

    /// <summary>
    /// Tests CASE WHEN with null check and null result
    /// </summary>
    [TestMethod]
    public void CaseWhen_WhenNullThenValue_ReturnsValue()
    {
        var query = "select (case when NullableValue is null then 'was null' else 'not null' end) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("was null", table[0][0]);
    }

    /// <summary>
    /// Tests CASE WHEN returning null in THEN clause
    /// </summary>
    [TestMethod]
    public void CaseWhen_ThenNull_ReturnsNull()
    {
        var query = "select (case when NullableValue is not null then null else 0 end) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = 5 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0], "CASE WHEN true THEN null should return null");
    }

    /// <summary>
    /// Tests CASE WHEN returning null in ELSE clause
    /// </summary>
    [TestMethod]
    public void CaseWhen_ElseNull_ReturnsNull()
    {
        var query = "select (case when NullableValue is null then 0 else null end) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = 5 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0], "CASE WHEN false ELSE null should return null");
    }

    #endregion

    #region Mixed Null Scenarios

    /// <summary>
    /// Tests complex expression with multiple nulls and arithmetic
    /// CURRENT BEHAVIOR: Throws InvalidOperationException
    /// NOTE: All arithmetic operations with null throw exceptions (simple and complex)
    /// </summary>
    [TestMethod]
    public void Mixed_ComplexExpressionWithMultipleNulls_ThrowsException()
    {
        var query = "select NullableValue + 10 - 5 * 2 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { NullableValue = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        // All arithmetic operations with null throw exception
        var exception = Assert.Throws<InvalidOperationException>(
            () => vm.Run(TestContext.CancellationToken));
        
        Assert.IsTrue(exception.Message.Contains("Nullable object must have a value"),
            "Exception should mention 'Nullable object must have a value'");
    }

    /// <summary>
    /// Tests that null rows are properly included when selecting null values
    /// </summary>
    [TestMethod]
    public void Mixed_SelectNullValue_ReturnsNullInResult()
    {
        var query = "select NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity { NullableValue = 1 },
                new BasicEntity { NullableValue = null },
                new BasicEntity { NullableValue = 3 }
            ] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "All three rows should be returned");
        Assert.IsTrue(table.Any(r => r[0] != null && (int)r[0] == 1), "Should have row with value 1");
        Assert.IsTrue(table.Any(r => r[0] == null), "Should have row with null");
        Assert.IsTrue(table.Any(r => r[0] != null && (int)r[0] == 3), "Should have row with value 3");
    }

    /// <summary>
    /// Tests filtering with IS NULL combined with other conditions
    /// </summary>
    [TestMethod]
    public void Mixed_IsNullWithOtherConditions_WorksCorrectly()
    {
        var query = "select Name from #A.Entities() where NullableValue is null or NullableValue > 5";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Row1") { NullableValue = null },
                new BasicEntity("Row2") { NullableValue = 3 },
                new BasicEntity("Row3") { NullableValue = 10 }
            ] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should return rows where null or > 5");
        Assert.IsTrue(table.Any(r => (string)r[0] == "Row1"), "Should include null row");
        Assert.IsTrue(table.Any(r => (string)r[0] == "Row3"), "Should include row with value 10");
    }

    #endregion

    #region Decimal Type with Null

    /// <summary>
    /// Tests arithmetic with decimal type (Population is decimal) and comparing with null value
    /// Note: Population is non-nullable decimal, so we test arithmetic behavior
    /// </summary>
    [TestMethod]
    public void Arithmetic_Decimal_WithValue_ReturnsCorrectResult()
    {
        var query = "select Population + 100 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test", 500)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(600m, table[0][0], "500 + 100 should return 600");
    }

    #endregion

    #region Boolean/Logical Type with Null Simulation

    /// <summary>
    /// Tests CASE WHEN simulating null boolean behavior
    /// </summary>
    [TestMethod]
    public void Logical_CaseWhenSimulatesNullBoolean_WorksCorrectly()
    {
        // Using CASE WHEN to simulate null boolean scenarios
        var query = @"select 
            (case when NullableValue is null then 'unknown' 
                  when NullableValue > 5 then 'true' 
                  else 'false' end) 
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Row1") { NullableValue = null },
                new BasicEntity("Row2") { NullableValue = 3 },
                new BasicEntity("Row3") { NullableValue = 10 }
            ] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(r => (string)r[0] == "unknown"), "Null should map to 'unknown'");
        Assert.IsTrue(table.Any(r => (string)r[0] == "false"), "3 > 5 is false");
        Assert.IsTrue(table.Any(r => (string)r[0] == "true"), "10 > 5 is true");
    }

    #endregion
}
