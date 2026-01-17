using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class NumberFormatsIntegrationTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInSelectClause()
    {
        var query = @"
                SELECT 
                    0xFF as HexValue,
                    0b1010 as BinaryValue,
                    0o77 as OctalValue,
                    0xFF + 0b1010 + 0o77 as Total
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(255L, result[0][0]);
        Assert.AreEqual(10L, result[0][1]);
        Assert.AreEqual(63L, result[0][2]);
        Assert.AreEqual(328L, result[0][3]);
    }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInWhereClause()
    {
        var query = @"
                SELECT 1
                FROM #A.Entities()
                WHERE 0xFF = 255";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInArithmeticExpressions()
    {
        var query = @"
                SELECT 
                    (0xFF + 0b1010) * 0o2 as ComplexCalc
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(530L, result[0][0]);
    }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInCaseStatement()
    {
        var query = @"
                SELECT 
                    CASE 
                        WHEN 0xFF > 0x80 THEN 'Large'
                        WHEN 0xFF > 0x10 THEN 'Medium'
                        ELSE 'Small'
                    END as Category
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Large", result[0][0]);
    }

    [TestMethod]
    public void ComplexQuery_AllNumberFormatsInSingleExpression()
    {
        var query = @"
                SELECT 
                    0xFF + 0b11111111 + 0o377 + 255 as AllFormatsSum,
                    (0xFF * 0b10) - (0o100 / 0x4) as ComplexCalculation
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1020L, result[0][0]);
        Assert.AreEqual(494L, result[0][1]);
    }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInNestedExpressions()
    {
        var query = @"
                SELECT 
                    ((0xFF + 0x1) * 0b10) / (0o77 + 0x1) as NestedCalculation
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(8L, result[0][0]);
    }

    [TestMethod]
    public void ComplexQuery_NumberFormatsInComplexConditionals()
    {
        var query = @"
                SELECT 
                    CASE 
                        WHEN 0xFF >= 0x10 AND 0xFF <= 0x200 THEN 
                            CASE 
                                WHEN 0xFF % 0b10 = 0x1 THEN 'Odd in range'
                                ELSE 'Even in range'
                            END
                        WHEN 0xFF > 0x200 THEN 'Above range'
                        ELSE 'Below range'
                    END as ComplexCategory
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Odd in range", result[0][0]);
    }

    [TestMethod]
    public void PerformanceTest_LargeNumberOfOperations()
    {
        var query = @"
                SELECT 
                    0xFF + 0xFF + 0xFF + 0xFF + 0xFF + 0xFF + 0xFF + 0xFF + 
                    0b1111 + 0b1111 + 0b1111 + 0b1111 + 0b1111 + 0b1111 + 
                    0o77 + 0o77 + 0o77 + 0o77 + 0o77 + 0o77 as LargeSum
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2508L, result[0][0]);
    }

    [TestMethod]
    public void EdgeCase_VeryLargeNumbers()
    {
        var query = @"
                SELECT 
                    0xFFFF as LargeHex,
                    0b1111111111111111 as LargeBinary,
                    0o177777 as LargeOctal
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(65535L, result[0][0]);
        Assert.AreEqual(65535L, result[0][1]);
        Assert.AreEqual(65535L, result[0][2]);
    }

    [TestMethod]
    public void ValidationTest_CrossFormatEquivalence()
    {
        var query = @"
                SELECT 
                    CASE WHEN 0xFF = 0b11111111 AND 0xFF = 0o377 THEN 'Equal' ELSE 'Not Equal' END as EquivalenceTest,
                    CASE WHEN 0x10 = 0b10000 AND 0x10 = 0o20 THEN 'Equal' ELSE 'Not Equal' END as EquivalenceTest2
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Equal", result[0][0]);
        Assert.AreEqual("Equal", result[0][1]);
    }

    [TestMethod]
    public void IntegrationTest_NumberFormatsWithFunctionCalls()
    {
        var query = @"
                SELECT 
                    Abs(0x10 - 0xFF) as AbsDifference,
                    0xFF % 0x10 as Modulo
                FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(239L, result[0][0]);
        Assert.AreEqual(15L, result[0][1]);
    }

    [TestMethod]
    public void IntegrationTest_ParenthesesAndPrecedence()
    {
        var query =
            "SELECT 0xFF + 0x2 * 0x3 as WithoutParens, (0xFF + 0x2) * 0x3 as WithParens, 0xFF * 0x2 + 0x3 as MultiplyFirst, 0xFF + 0x2 * 0x3 + 0x1 as ChainedOps FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(261L, result[0][0]);
        Assert.AreEqual(771L, result[0][1]);
        Assert.AreEqual(513L, result[0][2]);
        Assert.AreEqual(262L, result[0][3]);
    }

    [TestMethod]
    public void IntegrationTest_SimpleHex()
    {
        var query = "select 0xFF from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(255L, result[0][0]);
    }

    [TestMethod]
    public void IntegrationTest_MixedWithRegularIntegers()
    {
        var query =
            "SELECT 0xFF + 1 as HexPlusInt, 100 - 0x10 as IntMinusHex, 0b1010 * 5 as BinaryTimesInt, 1000 / 0o10 as IntDivideOctal FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(256L, result[0][0]);
        Assert.AreEqual(84L, result[0][1]);
        Assert.AreEqual(50L, result[0][2]);
        Assert.AreEqual(125L, result[0][3]);
    }
}