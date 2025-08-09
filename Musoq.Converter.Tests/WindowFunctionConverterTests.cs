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
        var query = "select RANK() OVER (ORDER BY Dummy DESC) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithPartitionByClause_ShouldCompile()
    {
        var query = "select RANK() OVER (PARTITION BY Dummy) as Ranking from #system.dual()";
        
        var (dllFile, pdbFile) = CreateForStore(query);
        
        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);
        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public void Convert_RankWithPartitionAndOrderBy_ShouldCompile()
    {
        var query = "select RANK() OVER (PARTITION BY Dummy ORDER BY Dummy DESC) as Ranking from #system.dual()";
        
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
            RANK() OVER (ORDER BY Dummy) as Rank1,
            DenseRank() OVER (PARTITION BY Dummy) as DenseRank1,
            LAG(Dummy, 1, 'default') OVER (ORDER BY Dummy) as PrevValue,
            LEAD(Dummy, 2, 'default') OVER (ORDER BY Dummy) as NextValue
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
        var query = "select RANK() OVER (PARTITION BY Dummy ORDER BY Length(Dummy) * 2 DESC) as ComplexRank from #system.dual()";
        
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
            Dummy,
            UPPER(ToString(Dummy)) as UpperValue,
            RANK() OVER (ORDER BY Dummy) as WindowRank,
            DenseRank() OVER (PARTITION BY Dummy) as RegionalDenseRank
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
            Dummy,
            RANK() OVER (ORDER BY Dummy) as RankCol,
            DENSE_RANK() OVER (ORDER BY Dummy) as DenseRankCol,
            LAG(Dummy, 1, 'default') OVER (ORDER BY Dummy) as LagCol,
            LEAD(Dummy, 2, 'default') OVER (ORDER BY Dummy) as LeadCol
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
        var query = "select RANK() OVER (ORDER BY Dummy DESC) as Ranking from #system.dual()";
        
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
            Dummy,
            RANK() OVER (ORDER BY Dummy DESC) as PopRank,
            CASE WHEN RANK() OVER (ORDER BY Dummy DESC) = 1 THEN 'Top' ELSE 'Other' END as Category
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
            Dummy,
            Length(Dummy) as DoublePop,
            RANK() OVER (ORDER BY Length(Dummy) DESC) as DoublePopRank,
            RANK() OVER (ORDER BY Dummy DESC) + 100 as AdjustedRank,
            (Length(Dummy) / RANK() OVER (ORDER BY Dummy DESC)) as PopPerRank
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
            Dummy,
            RANK() OVER (ORDER BY Dummy DESC) as Ranking
            from #system.dual()
            where Length(Dummy) > 0";
        
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
            Dummy,
            RANK() OVER (ORDER BY Dummy DESC) as Ranking
            from #system.dual()
            order by Dummy";
        
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
            Dummy,
            RANK() OVER (PARTITION BY Dummy ORDER BY Dummy DESC) as ComplexRank
            from #system.dual()
            where Dummy IS NOT NULL
            order by Dummy desc";
        
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