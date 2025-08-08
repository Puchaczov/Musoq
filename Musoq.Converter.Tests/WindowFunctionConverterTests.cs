using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

/// <summary>
/// Comprehensive tests for window function conversion from AST to C# code.
/// Tests the converter layer's ability to handle WindowFunctionNode and WindowSpecificationNode.
/// </summary>
[TestClass]
public class WindowFunctionConverterTests
{
    [TestMethod]
    public void Convert_BasicRankFunction_ShouldCompile()
    {
        var query = "select RANK() as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithEmptyOverClause_ShouldCompile()
    {
        var query = "select RANK() OVER () as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithOrderByClause_ShouldCompile()
    {
        var query = "select RANK() OVER (ORDER BY Value DESC) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithPartitionByClause_ShouldCompile()
    {
        var query = "select RANK() OVER (PARTITION BY Value) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithPartitionAndOrderBy_ShouldCompile()
    {
        var query = "select RANK() OVER (PARTITION BY Value ORDER BY Value DESC) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_MultipleWindowFunctions_ShouldCompile()
    {
        var query = @"select 
            RANK() OVER (ORDER BY Value) as Rank1,
            DenseRank() OVER (PARTITION BY Value) as DenseRank1,
            LAG(Value, 1, 0) OVER (ORDER BY Value) as PrevValue,
            LEAD(Value, 2, 0) OVER (ORDER BY Value) as NextValue
            from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_WindowFunctionWithComplexExpressions_ShouldCompile()
    {
        var query = "select RANK() OVER (PARTITION BY Value ORDER BY Value * 2 DESC) as ComplexRank from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_MixedWindowAndRegularFunctions_ShouldCompile()
    {
        var query = @"select 
            Value,
            UPPER(ToString(Value)) as UpperValue,
            RANK() OVER (ORDER BY Value) as WindowRank,
            DenseRank() OVER (PARTITION BY Value) as RegionalDenseRank
            from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_AllWindowFunctionTypes_ShouldCompile()
    {
        var query = @"select 
            Value,
            RANK() OVER (ORDER BY Value) as RankCol,
            DENSE_RANK() OVER (ORDER BY Value) as DenseRankCol,
            LAG(Value, 1, 0) OVER (ORDER BY Value) as LagCol,
            LEAD(Value, 2, 0) OVER (ORDER BY Value) as LeadCol
            from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public async Task Convert_WindowFunctionsAsync_ShouldCompile()
    {
        var query = "select RANK() OVER (ORDER BY Value DESC) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = await CreateForStoreAsync(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_WindowFunctionTypeInference_ShouldWork()
    {
        // Test that window functions participate correctly in type inference
        var query = @"select 
            Value,
            RANK() OVER (ORDER BY Value DESC) as PopRank,
            CASE WHEN RANK() OVER (ORDER BY Value DESC) = 1 THEN 'Top' ELSE 'Other' END as Category
            from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_WindowFunctionWithArithmetic_ShouldCompile()
    {
        // Test window functions used in arithmetic expressions
        var query = @"select 
            Value,
            Value * 2 as DoublePop,
            RANK() OVER (ORDER BY Value * 2 DESC) as DoublePopRank,
            RANK() OVER (ORDER BY Value DESC) + 100 as AdjustedRank,
            (Value / RANK() OVER (ORDER BY Value DESC)) as PopPerRank
            from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_WindowFunctionWithWhere_ShouldCompile()
    {
        var query = @"select 
            Value,
            RANK() OVER (ORDER BY Value DESC) as Ranking
            from #system.dual()
            where Value > 0";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_WindowFunctionWithOrderBy_ShouldCompile()
    {
        var query = @"select 
            Value,
            RANK() OVER (ORDER BY Value DESC) as Ranking
            from #system.dual()
            order by Value";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_ComplexWindowFunction_ShouldCompile()
    {
        var query = @"select 
            Value,
            RANK() OVER (PARTITION BY Value ORDER BY Value DESC) as ComplexRank
            from #system.dual()
            where Value IS NOT NULL
            order by Value desc";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    private static (byte[] dllFile, byte[] pdbFile) CreateForStore(string query)
    {
        return InstanceCreator.CompileForStore(query, Guid.NewGuid().ToString(), new SystemSchemaProvider(), new TestsLoggerResolver());
    }

    private static async Task<(byte[] dllFile, byte[] pdbFile)> CreateForStoreAsync(string query)
    {
        var result = await InstanceCreator.CompileForStoreAsync(query, Guid.NewGuid().ToString(), new SystemSchemaProvider(), new TestsLoggerResolver());
        return (result.DllFile, result.PdbFile);
    }

    static WindowFunctionConverterTests()
    {
        Culture.ApplyWithDefaultCulture();
    }
}