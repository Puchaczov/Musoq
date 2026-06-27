using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 16: Empty source scenarios

    [TestMethod]
    public void Explore16_CrossApply_EmptySource_ShouldReturnEmpty()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>().ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Explore16_CrossApply_AllEmptyArrays_ShouldReturnEmpty()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] },
            new() { Name = "Jane", Age = 25, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count);
    }

    #endregion

    #region Exploration 17: Aliasing edge cases

    [TestMethod]
    public void Explore17_CrossApply_SameAliasAsColumn_ShouldWork()
    {
        const string query = @"
            select p.Name, Name.Value
            from #schema.first() p
            cross apply p.Tags Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore17_CrossApply_LongAlias_ShouldWork()
    {
        const string query = @"
            select p.Name, ThisIsAVeryLongAliasNameForTheCrossAppliedTags.Value
            from #schema.first() p
            cross apply p.Tags ThisIsAVeryLongAliasNameForTheCrossAppliedTags";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion
}
