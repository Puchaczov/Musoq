using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForStore_WhenQueryIsValid_ShouldStillReturnDllBytes()
    {
        var result = InstanceCreator.CompileForStore(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.IsGreaterThan(0, result.DllFile.Length);
    }
}
