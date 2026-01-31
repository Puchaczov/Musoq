using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Diagnostic tests to verify CTE parallelization is actually working.
///     These tests verify:
///     1. The execution plan correctly identifies parallelizable CTEs
///     2. The generated code contains Parallel.ForEach
///     3. CTEs actually execute in parallel (measured by timing and thread tracking)
/// </summary>
[TestClass]
public class CteParallelizationDiagnosticsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Helper Methods

    private string CompileAndGetGeneratedCode(
        string query,
        Dictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions compilationOptions)
    {
        var vm = CreateAndRunVirtualMachine(query, sources, compilationOptions);


        _ = vm.Run(TestContext.CancellationToken);


        var tempPath = Path.Combine(Path.GetTempPath(), "Musoq");
        if (!Directory.Exists(tempPath))
            return "(No generated code files found)";


        var csFiles = Directory.GetFiles(tempPath, "*.cs")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();

        if (csFiles.Count == 0)
            return "(No .cs files found)";

        return File.ReadAllText(csFiles.First());
    }

    #endregion

    #region Execution Plan Tests

    [TestMethod]
    public void ExecutionPlan_FourIndependentCtes_ShouldHaveCanParallelizeTrue()
    {
        // Arrange: Parse query with 4 independent CTEs
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from #A.entities()),
                 cte3 as (select City from #A.entities()),
                 cte4 as (select City from #A.entities())
            select a.City, b.City, c.City, d.City 
            from cte1 a 
            inner join cte2 b on a.City = b.City
            inner join cte3 c on a.City = c.City
            inner join cte4 d on a.City = d.City";

        // Act: Parse and analyze
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var rootNode = parser.ComposeAll();

        // Navigate: RootNode -> StatementsArrayNode -> StatementNode -> CteExpressionNode
        var statementsArray = rootNode.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray, "Expected StatementsArrayNode");
        var statementNode = statementsArray.Statements[0];
        var cteExpression = statementNode.Node as CteExpressionNode;
        Assert.IsNotNull(cteExpression, "Expected CteExpressionNode");

        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true for 4 independent CTEs");
        Assert.AreEqual(4, plan.TotalCteCount, "Should have 4 CTEs");
        Assert.AreEqual(1, plan.LevelCount, "All 4 CTEs should be at level 0");
        Assert.AreEqual(4, plan.MaxParallelism, "Max parallelism should be 4");
    }

    [TestMethod]
    public void ExecutionPlan_DependentCtes_ShouldHaveCanParallelizeFalse()
    {
        // Arrange: Parse query with dependent CTEs (chain: cte1 -> cte2 -> cte3 -> cte4)
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from cte1),
                 cte3 as (select City from cte2),
                 cte4 as (select City from cte3)
            select City from cte4";

        // Act: Parse and analyze
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var rootNode = parser.ComposeAll();

        // Navigate: RootNode -> StatementsArrayNode -> StatementNode -> CteExpressionNode
        var statementsArray = rootNode.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray, "Expected StatementsArrayNode");
        var statementNode = statementsArray.Statements[0];
        var cteExpression = statementNode.Node as CteExpressionNode;
        Assert.IsNotNull(cteExpression, "Expected CteExpressionNode");

        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.IsFalse(plan.CanParallelize, "CanParallelize should be false for chain dependencies");
        Assert.AreEqual(4, plan.TotalCteCount, "Should have 4 CTEs");
        Assert.AreEqual(4, plan.LevelCount, "Each CTE should be at a different level");
        Assert.AreEqual(1, plan.MaxParallelism, "Max parallelism should be 1 (no parallel execution)");
    }

    [TestMethod]
    public void ExecutionPlan_MixedDependencies_ShouldIdentifyParallelizableLevels()
    {
        // Arrange: Two independent CTEs, then one that depends on both
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from #A.entities()),
                 cte3 as (select a.City from cte1 a inner join cte2 b on a.City = b.City)
            select City from cte3";

        // Act: Parse and analyze
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var rootNode = parser.ComposeAll();

        // Navigate: RootNode -> StatementsArrayNode -> StatementNode -> CteExpressionNode
        var statementsArray = rootNode.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray, "Expected StatementsArrayNode");
        var statementNode = statementsArray.Statements[0];
        var cteExpression = statementNode.Node as CteExpressionNode;
        Assert.IsNotNull(cteExpression, "Expected CteExpressionNode");

        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true - cte1 and cte2 can run in parallel");
        Assert.AreEqual(3, plan.TotalCteCount, "Should have 3 CTEs");
        Assert.AreEqual(2, plan.LevelCount, "Should have 2 levels: [cte1, cte2] at level 0, [cte3] at level 1");
        Assert.AreEqual(2, plan.MaxParallelism, "Max parallelism should be 2");
    }

    #endregion

    #region Generated Code Tests

    [TestMethod]
    [Ignore("Generated code file reading is unreliable - code is not always written to temp files")]
    public void GeneratedCode_WithParallelization_ShouldContainParallelForEach()
    {
        // Arrange
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from #A.entities()),
                 cte3 as (select City from #A.entities()),
                 cte4 as (select City from #A.entities())
            select a.City, b.City, c.City, d.City 
            from cte1 a 
            inner join cte2 b on a.City = b.City
            inner join cte3 c on a.City = c.City
            inner join cte4 d on a.City = d.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] { new BasicEntity("WARSAW", "POLAND", 500) } }
        };

        // Act: Compile with parallelization enabled and get generated code
        var compilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: true);
        var generatedCode = CompileAndGetGeneratedCode(query, sources, compilationOptions);

        // Assert
        Assert.IsTrue(generatedCode.Contains("Parallel.ForEach"),
            $"Generated code should contain Parallel.ForEach when parallelization is enabled.\n\n" +
            $"Generated code:\n{generatedCode}");
    }

    [TestMethod]
    [Ignore("Generated code file reading is unreliable - code is not always written to temp files")]
    public void GeneratedCode_WithoutParallelization_ShouldNotContainParallelForEach()
    {
        // Arrange
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from #A.entities()),
                 cte3 as (select City from #A.entities()),
                 cte4 as (select City from #A.entities())
            select a.City, b.City, c.City, d.City 
            from cte1 a 
            inner join cte2 b on a.City = b.City
            inner join cte3 c on a.City = c.City
            inner join cte4 d on a.City = d.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] { new BasicEntity("WARSAW", "POLAND", 500) } }
        };

        // Act: Compile with parallelization DISABLED
        var compilationOptions =
            new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: false);
        var generatedCode = CompileAndGetGeneratedCode(query, sources, compilationOptions);

        // Assert
        Assert.IsFalse(generatedCode.Contains("Parallel.ForEach"),
            "Generated code should NOT contain Parallel.ForEach when parallelization is disabled");
    }

    #endregion

    #region Extended Execution Plan Tests

    [TestMethod]
    public void ExecutionPlan_EightIndependentCtes_ShouldHaveMaxParallelism8()
    {
        // Arrange: 8 independent CTEs
        const string query = @"
            with cte1 as (select City from #A.entities()),
                 cte2 as (select City from #A.entities()),
                 cte3 as (select City from #A.entities()),
                 cte4 as (select City from #A.entities()),
                 cte5 as (select City from #A.entities()),
                 cte6 as (select City from #A.entities()),
                 cte7 as (select City from #A.entities()),
                 cte8 as (select City from #A.entities())
            select a.City, b.City, c.City, d.City, e.City, f.City, g.City, h.City
            from cte1 a 
            inner join cte2 b on a.City = b.City
            inner join cte3 c on a.City = c.City
            inner join cte4 d on a.City = d.City
            inner join cte5 e on a.City = e.City
            inner join cte6 f on a.City = f.City
            inner join cte7 g on a.City = g.City
            inner join cte8 h on a.City = h.City";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true for 8 independent CTEs");
        Assert.AreEqual(8, plan.TotalCteCount, "Should have 8 CTEs");
        Assert.AreEqual(1, plan.LevelCount, "All 8 CTEs should be at level 0");
        Assert.AreEqual(8, plan.MaxParallelism, "Max parallelism should be 8");
    }

    [TestMethod]
    public void ExecutionPlan_DoubleDiamond_ShouldHaveFiveLevels()
    {
        // Arrange: Double diamond pattern
        // a → [b, c] → d → [e, f] → g
        const string query = @"
            with a as (select City from #A.entities()),
                 b as (select City from a where City like 'A%'),
                 c as (select City from a where City like 'B%'),
                 d as (select b.City from b inner join c on b.City = c.City),
                 e as (select City from d where City like 'C%'),
                 f as (select City from d where City like 'D%'),
                 g as (select e.City from e inner join f on e.City = f.City)
            select City from g";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true - [b,c] and [e,f] can run in parallel");
        Assert.AreEqual(7, plan.TotalCteCount, "Should have 7 CTEs");
        Assert.AreEqual(5, plan.LevelCount, "Should have 5 levels");
        Assert.AreEqual(2, plan.MaxParallelism, "Max parallelism should be 2");

        // Verify level structure
        Assert.AreEqual(1, plan.Levels[0].Count, "Level 0 should have 1 CTE (a)");
        Assert.AreEqual(2, plan.Levels[1].Count, "Level 1 should have 2 CTEs (b, c)");
        Assert.AreEqual(1, plan.Levels[2].Count, "Level 2 should have 1 CTE (d)");
        Assert.AreEqual(2, plan.Levels[3].Count, "Level 3 should have 2 CTEs (e, f)");
        Assert.AreEqual(1, plan.Levels[4].Count, "Level 4 should have 1 CTE (g)");
    }

    [TestMethod]
    public void ExecutionPlan_EtlPipeline_ShouldHaveThreeParallelAtExtractAndClean()
    {
        // Arrange: ETL pipeline pattern
        const string query = @"
            with raw_orders as (select City as OrderCity from #A.entities()),
                 raw_customers as (select City as CustomerCity from #A.entities()),
                 raw_products as (select City as ProductCity from #A.entities()),
                 clean_orders as (select OrderCity from raw_orders where OrderCity is not null),
                 clean_customers as (select CustomerCity from raw_customers where CustomerCity is not null),
                 clean_products as (select ProductCity from raw_products where ProductCity is not null),
                 enriched_orders as (select o.OrderCity, c.CustomerCity from clean_orders o inner join clean_customers c on 1=1),
                 order_summary as (select e.OrderCity, p.ProductCity from enriched_orders e inner join clean_products p on 1=1)
            select OrderCity, ProductCity from order_summary";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true");
        Assert.AreEqual(8, plan.TotalCteCount, "Should have 8 CTEs");
        Assert.AreEqual(4, plan.LevelCount, "Should have 4 levels");
        Assert.AreEqual(3, plan.MaxParallelism, "Max parallelism should be 3");

        // Level 0: 3 raw extracts
        Assert.AreEqual(3, plan.Levels[0].Count, "Level 0 should have 3 CTEs");
        // Level 1: 3 clean transforms
        Assert.AreEqual(3, plan.Levels[1].Count, "Level 1 should have 3 CTEs");
        // Level 2: 1 enrichment
        Assert.AreEqual(1, plan.Levels[2].Count, "Level 2 should have 1 CTE");
        // Level 3: 1 summary
        Assert.AreEqual(1, plan.Levels[3].Count, "Level 3 should have 1 CTE");
    }

    [TestMethod]
    public void ExecutionPlan_IndependentBranchWithJoin_ShouldParallelizeThreeCtesAtLevel0()
    {
        // Arrange: a, b independent; c depends on a,b; d independent
        // Expected: Level 0: [a, b, d], Level 1: [c]
        const string query = @"
            with a as (select City from #A.entities()),
                 b as (select City from #B.entities()),
                 c as (select a.City from a inner join b on a.City = b.City),
                 d as (select City from #C.entities())
            select c.City, d.City from c inner join d on c.City = d.City";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true");
        Assert.AreEqual(4, plan.TotalCteCount, "Should have 4 CTEs");
        Assert.AreEqual(2, plan.LevelCount, "Should have 2 levels");
        Assert.AreEqual(3, plan.MaxParallelism, "Max parallelism should be 3 (a, b, d at level 0)");

        // Level 0: a, b, d (all independent)
        Assert.AreEqual(3, plan.Levels[0].Count, "Level 0 should have 3 CTEs");
        // Level 1: c
        Assert.AreEqual(1, plan.Levels[1].Count, "Level 1 should have 1 CTE");
    }

    [TestMethod]
    public void ExecutionPlan_WithUnion_ShouldIdentifyDependencies()
    {
        // Arrange: c is UNION of a and b
        const string query = @"
            with a as (select City from #A.entities() where City like 'A%'),
                 b as (select City from #A.entities() where City like 'B%'),
                 c as (select City from a union select City from b)
            select City from c";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true - a and b can run in parallel");
        Assert.AreEqual(3, plan.TotalCteCount, "Should have 3 CTEs");
        Assert.AreEqual(2, plan.LevelCount, "Should have 2 levels");
        Assert.AreEqual(2, plan.MaxParallelism, "Max parallelism should be 2");

        // Level 0: a, b (both independent)
        Assert.AreEqual(2, plan.Levels[0].Count, "Level 0 should have 2 CTEs");
        // Level 1: c (depends on a and b via UNION)
        Assert.AreEqual(1, plan.Levels[1].Count, "Level 1 should have 1 CTE");
    }

    [TestMethod]
    public void ExecutionPlan_WithExcept_ShouldIdentifyDependencies()
    {
        // Arrange: c is a EXCEPT b
        const string query = @"
            with a as (select City from #A.entities()),
                 b as (select City from #A.entities() where Population > 1000),
                 c as (select City from a except select City from b)
            select City from c";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true");
        Assert.AreEqual(3, plan.TotalCteCount, "Should have 3 CTEs");
        Assert.AreEqual(2, plan.LevelCount, "Should have 2 levels: [a,b] at 0, [c] at 1");
        Assert.AreEqual(2, plan.MaxParallelism, "Max parallelism should be 2");
    }

    [TestMethod]
    public void ExecutionPlan_MultipleBranches_ShouldHaveMaxParallelism4()
    {
        // Arrange: Two independent branches that merge
        // a, b → c; d, e → f; c, f → g
        const string query = @"
            with a as (select City from #A.entities()),
                 b as (select City from #A.entities()),
                 c as (select a.City from a inner join b on a.City = b.City),
                 d as (select City from #A.entities()),
                 e as (select City from #A.entities()),
                 f as (select d.City from d inner join e on d.City = e.City),
                 g as (select c.City from c inner join f on c.City = f.City)
            select City from g";

        // Act
        var plan = CreatePlanFromQuery(query);

        // Assert
        Assert.IsTrue(plan.CanParallelize, "CanParallelize should be true");
        Assert.AreEqual(7, plan.TotalCteCount, "Should have 7 CTEs");
        Assert.AreEqual(3, plan.LevelCount, "Should have 3 levels");
        Assert.AreEqual(4, plan.MaxParallelism, "Max parallelism should be 4 (a, b, d, e at level 0)");

        // Level 0: a, b, d, e (all independent)
        Assert.AreEqual(4, plan.Levels[0].Count, "Level 0 should have 4 CTEs");
        // Level 1: c, f (both depend on level 0)
        Assert.AreEqual(2, plan.Levels[1].Count, "Level 1 should have 2 CTEs");
        // Level 2: g (depends on c and f)
        Assert.AreEqual(1, plan.Levels[2].Count, "Level 2 should have 1 CTE");
    }

    private static CteExecutionPlan CreatePlanFromQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var rootNode = parser.ComposeAll();

        var statementsArray = rootNode.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray, "Expected StatementsArrayNode");
        var statementNode = statementsArray.Statements[0];
        var cteExpression = statementNode.Node as CteExpressionNode;
        Assert.IsNotNull(cteExpression, "Expected CteExpressionNode");

        return CteParallelizationAnalyzer.CreatePlan(cteExpression);
    }

    #endregion

    #region Actual Parallelization Tests

    [TestMethod]
    public void ActualParallelization_FourExpensiveCtes_ShouldRunInParallel()
    {
        // Arrange: Use a schema that tracks thread IDs for each CTE
        ThreadTrackingRowSource.ResetCallCounts();
        var threadTracker = new ConcurrentDictionary<string, int>();
        var executionOrder = new ConcurrentBag<(string CteName, DateTime StartTime, int ThreadId)>();

        var schemaProvider = new ThreadTrackingSchemaProvider(threadTracker, executionOrder, 100);

        // Use inner join query (original) - we need to understand why joins add so much time
        const string query = @"
            with cte1 as (select Id, Name from #test.slowEntities('cte1')),
                 cte2 as (select Id, Name from #test.slowEntities('cte2')),
                 cte3 as (select Id, Name from #test.slowEntities('cte3')),
                 cte4 as (select Id, Name from #test.slowEntities('cte4'))
            select a.Id, b.Id, c.Id, d.Id 
            from cte1 a 
            inner join cte2 b on a.Id = b.Id
            inner join cte3 c on a.Id = c.Id
            inner join cte4 d on a.Id = d.Id";

        var compilationOptions = new CompilationOptions(
            ParallelizationMode.Full,
            true,
            true,
            true,
            true,
            true);

        // Act - time compilation separately from execution
        var swCompile = Stopwatch.StartNew();
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new TestLoggerResolver(),
            compilationOptions);
        swCompile.Stop();

        var swRun = Stopwatch.StartNew();
        var table = compiled.Run();
        swRun.Stop();

        // Assert: Verify multiple threads were used
        var uniqueThreads = threadTracker.Values.Distinct().Count();
        var callCounts = ThreadTrackingRowSource.GetCallCounts();

        Console.WriteLine($"Compilation time: {swCompile.ElapsedMilliseconds}ms");
        Console.WriteLine($"Execution time: {swRun.ElapsedMilliseconds}ms");
        Console.WriteLine($"Total time: {swCompile.ElapsedMilliseconds + swRun.ElapsedMilliseconds}ms");
        Console.WriteLine($"Unique threads used: {uniqueThreads}");
        Console.WriteLine($"Total event count: {executionOrder.Count}");
        Console.WriteLine("Rows getter call counts:");
        foreach (var (cteName, count) in callCounts.OrderBy(x => x.Key))
            Console.WriteLine($"  {cteName}: called {count} times");
        Console.WriteLine("Thread IDs by CTE (first call):");
        foreach (var (cteName, threadId) in threadTracker.OrderBy(x => x.Key))
            Console.WriteLine($"  {cteName}: Thread {threadId}");
        Console.WriteLine("All CTE calls (in order recorded):");
        foreach (var (cteName, startTime, threadId) in executionOrder.OrderBy(x => x.StartTime))
            Console.WriteLine($"  {cteName}: Thread {threadId} at {startTime:HH:mm:ss.fff}");

        // With parallelization, we expect more than 1 thread to be used
        // (unless the machine is single-core)
        if (Environment.ProcessorCount > 1)
            Assert.IsTrue(uniqueThreads > 1,
                $"Expected multiple threads to be used for parallel CTE execution. " +
                $"Found {uniqueThreads} unique thread(s). ProcessorCount={Environment.ProcessorCount}");

        // The total time should be less than 4x100ms (sequential would be ~400ms)
        // Parallel should be closer to 100ms (+ overhead)
        Assert.IsTrue(swRun.ElapsedMilliseconds < 350,
            $"Execution took {swRun.ElapsedMilliseconds}ms which suggests sequential execution. " +
            $"Expected < 350ms for parallel execution of 4x100ms CTEs.");
    }

    [TestMethod]
    public void ActualParallelization_SequentialMode_ShouldBeSingleThreaded()
    {
        // Arrange: Use same schema but with parallelization DISABLED
        var threadTracker = new ConcurrentDictionary<string, int>();
        var executionOrder = new ConcurrentBag<(string CteName, DateTime StartTime, int ThreadId)>();

        var schemaProvider = new ThreadTrackingSchemaProvider(threadTracker, executionOrder, 50);

        const string query = @"
            with cte1 as (select Id, Name from #test.slowEntities('cte1')),
                 cte2 as (select Id, Name from #test.slowEntities('cte2')),
                 cte3 as (select Id, Name from #test.slowEntities('cte3')),
                 cte4 as (select Id, Name from #test.slowEntities('cte4'))
            select a.Id, b.Id, c.Id, d.Id 
            from cte1 a 
            inner join cte2 b on a.Id = b.Id
            inner join cte3 c on a.Id = c.Id
            inner join cte4 d on a.Id = d.Id";

        var compilationOptions = new CompilationOptions(
            ParallelizationMode.Full,
            true,
            true,
            true,
            true,
            false); // DISABLED

        // Act
        var sw = Stopwatch.StartNew();
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new TestLoggerResolver(),
            compilationOptions);
        var table = compiled.Run();
        sw.Stop();

        // Assert: Verify only one thread was used
        var uniqueThreads = threadTracker.Values.Distinct().Count();

        Console.WriteLine($"Sequential execution time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Unique threads used: {uniqueThreads}");

        Assert.AreEqual(1, uniqueThreads,
            "Expected single thread for sequential CTE execution");

        // Sequential should take at least 4x50ms = 200ms
        Assert.IsTrue(sw.ElapsedMilliseconds >= 180,
            $"Execution took {sw.ElapsedMilliseconds}ms which is too fast for sequential 4x50ms CTEs.");
    }

    #endregion

    #region Test Schema for Thread Tracking

    private class ThreadTrackingSchemaProvider : ISchemaProvider
    {
        private readonly ConcurrentBag<(string, DateTime, int)> _executionOrder;
        private readonly ConcurrentDictionary<string, int> _threadTracker;
        private readonly int _workDurationMs;

        public ThreadTrackingSchemaProvider(
            ConcurrentDictionary<string, int> threadTracker,
            ConcurrentBag<(string, DateTime, int)> executionOrder,
            int workDurationMs)
        {
            _threadTracker = threadTracker;
            _executionOrder = executionOrder;
            _workDurationMs = workDurationMs;
        }

        public ISchema GetSchema(string schema)
        {
            return new ThreadTrackingSchema(_threadTracker, _executionOrder, _workDurationMs);
        }
    }

    private class ThreadTrackingSchema : SchemaBase
    {
        private readonly ConcurrentBag<(string, DateTime, int)> _executionOrder;
        private readonly ConcurrentDictionary<string, int> _threadTracker;
        private readonly int _workDurationMs;

        public ThreadTrackingSchema(
            ConcurrentDictionary<string, int> threadTracker,
            ConcurrentBag<(string, DateTime, int)> executionOrder,
            int workDurationMs)
            : base("test", CreateLibrary())
        {
            _threadTracker = threadTracker;
            _executionOrder = executionOrder;
            _workDurationMs = workDurationMs;
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodsManager);
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new ThreadTrackingTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            var cteName = parameters.Length > 0 ? parameters[0]?.ToString() ?? "unknown" : "unknown";
            return new ThreadTrackingRowSource(_threadTracker, _executionOrder, _workDurationMs, cteName);
        }
    }

    private class ThreadTrackingTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn("Id", 0, typeof(int)),
            new SchemaColumn("Name", 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(SimpleEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.FirstOrDefault(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }
    }

    private class SimpleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ThreadTrackingRowSource : RowSource
    {
        private static readonly ConcurrentDictionary<string, int> _callCounts = new();
        private readonly string _cteName;
        private readonly ConcurrentBag<(string, DateTime, int)> _executionOrder;
        private readonly ConcurrentDictionary<string, int> _threadTracker;
        private readonly int _workDurationMs;

        public ThreadTrackingRowSource(
            ConcurrentDictionary<string, int> threadTracker,
            ConcurrentBag<(string, DateTime, int)> executionOrder,
            int workDurationMs,
            string cteName)
        {
            _threadTracker = threadTracker;
            _executionOrder = executionOrder;
            _workDurationMs = workDurationMs;
            _cteName = cteName;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                // Count how many times Rows getter is called
                var callNum = _callCounts.AddOrUpdate(_cteName, 1, (_, count) => count + 1);

                // Record thread info when iteration starts
                var threadId = Thread.CurrentThread.ManagedThreadId;
                _threadTracker[_cteName] = threadId;
                _executionOrder.Add(($"{_cteName}_CALL{callNum}_START", DateTime.UtcNow, threadId));

                // Simulate work - this is the expensive operation
                Thread.Sleep(_workDurationMs);

                // Record when work finishes
                _executionOrder.Add(($"{_cteName}_CALL{callNum}_END", DateTime.UtcNow, threadId));

                // Return a single row
                yield return new SimpleEntityResolver(new SimpleEntity { Id = 1, Name = _cteName });
            }
        }

        public static void ResetCallCounts()
        {
            _callCounts.Clear();
        }

        public static ConcurrentDictionary<string, int> GetCallCounts()
        {
            return _callCounts;
        }
    }

    private class SimpleEntityResolver : IObjectResolver
    {
        private readonly SimpleEntity _entity;

        public SimpleEntityResolver(SimpleEntity entity)
        {
            _entity = entity;
            Contexts = [entity];
        }

        public object[] Contexts { get; }

        public object this[string name] => name switch
        {
            "Id" => _entity.Id,
            "Name" => _entity.Name,
            _ => throw new ArgumentException($"Unknown column: {name}")
        };

        public object this[int index] => index switch
        {
            0 => _entity.Id,
            1 => _entity.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public bool HasColumn(string name)
        {
            return name is "Id" or "Name";
        }
    }

    private class TestLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger()
        {
            return null!;
        }

        public ILogger<T> ResolveLogger<T>()
        {
            return null!;
        }
    }

    #endregion
}
