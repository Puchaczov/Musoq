using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

[TestClass]
public class BuildTests
{
    [TestMethod]
    public void CompileForStoreTest()
    {
        var query = "select 1 from @system.dual()";

        var (dllFile, pdbFile) = CreateForStore(query);

        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);

        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public async Task CompileForStoreAsyncTest()
    {
        var query = "select 1 from @system.dual()";

        var arrays = await InstanceCreator.CompileForStoreAsync(query, Guid.NewGuid().ToString(), new SystemSchemaProvider(), new TestsLoggerResolver());

        Assert.IsNotNull(arrays.DllFile);
        Assert.IsNotNull(arrays.PdbFile);

        Assert.AreNotEqual(0, arrays.DllFile.Length);
        Assert.AreNotEqual(0, arrays.PdbFile.Length);
    }

    private (byte[] DllFile, byte[] PdbFile) CreateForStore(string script)
    {
        return InstanceCreator.CompileForStore(script, Guid.NewGuid().ToString(), new SystemSchemaProvider(), new TestsLoggerResolver());
    }

    static BuildTests()
    {
        Culture.ApplyWithDefaultCulture();
    }
}