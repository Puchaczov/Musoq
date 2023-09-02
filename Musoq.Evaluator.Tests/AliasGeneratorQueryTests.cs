using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AliasGeneratorQueryTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBuildMultipleTimes_AliasesShouldStayTheSame()
    {
        const string query = "select 1 from #A.entities()";
        
        var firstBuild = CreateBuildItems<BasicEntity>(query);
        var secondBuild = CreateBuildItems<BasicEntity>(query);

        var firstFromNodes = firstBuild.UsedColumns.Keys.ToArray();
        var secondFromNodes = secondBuild.UsedColumns.Keys.ToArray();
        
        Assert.AreEqual(1, firstFromNodes.Length);
        Assert.AreEqual(1, secondFromNodes.Length);
        
        Assert.AreEqual(firstFromNodes[0].Alias, secondFromNodes[0].Alias);
    }
}