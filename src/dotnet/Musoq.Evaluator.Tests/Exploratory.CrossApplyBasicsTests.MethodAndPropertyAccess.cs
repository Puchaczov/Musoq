using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 1: Multiple Cross Applies with Method Calls

    [TestMethod]
    public void Explore1_CrossApply_MethodCallThenProperty_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Split(p.Name, ' ') t";

        var source = new List<Person>
        {
            new() { Name = "John Doe", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John Doe", "John"], ["John Doe", "Doe"]);
    }

    [TestMethod]
    public void Explore1_CrossApply_PropertyThenMethodCall_ShouldWork()
    {
        const string query = @"
            select t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Split(t.Value, '-') s";

        var source = new List<Person>
        {
            new() { Name = "Test", Age = 30, Tags = ["a-b", "c-d"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("t.Value", typeof(string)), ("s.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["a-b", "a"], ["a-b", "b"], ["c-d", "c"], ["c-d", "d"]);
    }

    [TestMethod]
    public void Explore1_ThreeCrossApplies_DifferentTypes_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "Test", Age = 30, Tags = ["a", "b"], Scores = [1, 2, 3] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("t.Value", typeof(string)), ("s.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Test", "a", 1], ["Test", "a", 2], ["Test", "a", 3],
            ["Test", "b", 1], ["Test", "b", 2], ["Test", "b", 3]);
    }

    #endregion

    #region Exploration 2: Nested Object Property Access

    [TestMethod]
    public void Explore2_CrossApply_NestedObjectProperty_ShouldWork()
    {
        const string query = @"
            select p.Name, a.City
            from #schema.first() p
            cross apply p.Addresses a";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Addresses = [new Address { City = "NYC", Street = "Broadway" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "NYC"]);
    }

    [TestMethod]
    public void Explore2_CrossApply_NestedObjectThenArray_ShouldWork()
    {
        const string query = @"
            select p.Name, a.City, ph.Value
            from #schema.first() p
            cross apply p.Addresses a
            cross apply a.PhoneNumbers ph";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Addresses =
                [
                    new Address { City = "NYC", Street = "Broadway", PhoneNumbers = ["111", "222"] },
                    new Address { City = "LA", Street = "Hollywood", PhoneNumbers = ["333"] }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("a.City", typeof(string)), ("ph.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "NYC", "111"], ["John", "NYC", "222"], ["John", "LA", "333"]);
    }

    [TestMethod]
    public void Explore2_CrossApply_ThreeLevelNesting_ShouldWork()
    {
        const string query = @"
            select root.Value, c.Value, gc.Value
            from #schema.first() root
            cross apply root.Children c
            cross apply c.Children gc";

        var source = new List<TreeNode>
        {
            new()
            {
                Id = 1,
                Value = "Root",
                Children =
                [
                    new TreeNode
                    {
                        Id = 2,
                        Value = "Child1",
                        Children = [new TreeNode { Id = 3, Value = "Grandchild1" }]
                    }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("root.Value", typeof(string)), ("c.Value", typeof(string)), ("gc.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Root", "Child1", "Grandchild1"]);
    }

    #endregion

    #region Exploration 10: Complex Expressions in Select

    [TestMethod]
    public void Explore10_CrossApply_WithComplexSelectExpressions_ShouldWork()
    {
        const string query = @"
            select
                p.Name + ' - ' + t.Value as Combined,
                Length(t.Value) as TagLength,
                p.Age * 2 as DoubleAge
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["hello"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Combined", typeof(string)), ("TagLength", typeof(int?)), ("DoubleAge", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John - hello", 5, 60]);
    }

    [TestMethod]
    public void Explore10_CrossApply_WithCaseWhen_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when s.Value > 50 then 'High' else 'Low' end as ScoreLevel
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [25, 75] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("ScoreLevel", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "Low"], ["John", "High"]);
    }

    [TestMethod]
    public void Explore10_CrossApply_WithCoalesce_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Coalesce(t.Value, 'NoTag') as TagOrDefault
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("TagOrDefault", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", null]);
    }

    #endregion
}
