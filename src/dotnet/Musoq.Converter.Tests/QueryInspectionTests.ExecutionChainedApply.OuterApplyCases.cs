using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenOuterApplyCaseBranchResultDependsOnRightAlias_ShouldFilterUnmatchedBranchAsUnknown()
    {
        var compiled = CompileForExecution("select i.Name, n.Value from #apply.items() i outer apply i.Numbers n where case when i.Name = 'empty' then n.Value = 7 else true end", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "empty",
                    Line = "INFO empty",
                    Numbers = []
                },
                new ApplyCandidateEntity
                {
                    Name = "filled",
                    Line = "INFO filled",
                    Numbers = [7]
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("filled", table[0][0]);
        Assert.AreEqual(7, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenOuterApplyCaseElseResultDependsOnRightAlias_ShouldFilterUnmatchedElseAsUnknown()
    {
        var compiled = CompileForExecution("select i.Name, n.Value from #apply.items() i outer apply i.Numbers n where case when i.Name = 'filled' then true else n.Value = 7 end", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "empty",
                    Line = "INFO empty",
                    Numbers = []
                },
                new ApplyCandidateEntity
                {
                    Name = "filled",
                    Line = "INFO filled",
                    Numbers = [7]
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("filled", table[0][0]);
        Assert.AreEqual(7, table[0][1]);
    }

}
