using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

namespace Musoq.Evaluator.Tests;

public sealed partial class ScriptVariableExecutionTests
{
    [TestMethod]
    public void WhenScriptVariableIsUsedInProjectionAliasAndMethodArgument_ShouldResolveEveryUse()
    {
        const string query =
            "let key: string = 'KEY_1'; " +
            "let word: string = 'value_1'; " +
            "select ToUpper($word) as UpperWord, $word + '_' + Key as Label " +
            "from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("VALUE_1", table[0][0]);
        Assert.AreEqual("value_1_KEY_1", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInGroupByAndHaving_ShouldFilterAggregatedGroups()
    {
        const string query =
            "let suffix: string = '_group'; " +
            "let minCount: int = 2; " +
            "select Value + $suffix as GroupKey, Count(Key) as KeyCount " +
            "from #EnvironmentVariables.All() " +
            "group by Value + $suffix " +
            "having Count(Key) >= $minCount";
        var vm = CreateAndRunVirtualMachine(query, CreateGroupedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("VALUE_A_group", table[0][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[0][1]));
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInJoinCondition_ShouldConstrainJoinedRows()
    {
        const string query =
            "let value: string = 'VALUE_2'; " +
            "select a.Key, b.Value " +
            "from #EnvironmentVariables.All() a " +
            "inner join #EnvironmentVariables.All() b on a.Key = b.Key and b.Value = $value";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("VALUE_2", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInCtePredicate_ShouldApplyInsideCte()
    {
        const string query =
            "let key: string = 'KEY_3'; " +
            "with filtered as (" +
            "select Key, Value from #EnvironmentVariables.All() where Key = $key" +
            ") select Key, Value from filtered";
        var vm = CreateAndRunVirtualMachine(query, CreateUnionEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_3", table[0][0]);
        Assert.AreEqual("VALUE_3", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInUnionBranches_ShouldApplyToBothQueries()
    {
        const string query =
            "let firstKey: string = 'KEY_1'; " +
            "let secondValue: string = 'VALUE_3'; " +
            "select Key from #EnvironmentVariables.All() where Key = $firstKey " +
            "union all (Key) " +
            "select Key from #EnvironmentVariables.All() where Value = $secondValue";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);
        var keys = table.Select(row => (string)row[0]).OrderBy(key => key).ToArray();

        CollectionAssert.AreEqual(new[] { "KEY_1", "KEY_3" }, keys);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInOrderByExpression_ShouldSortByComputedKey()
    {
        const string query =
            "let suffix: string = '_ordered'; " +
            "select Key from #EnvironmentVariables.All() order by Key + $suffix desc";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("KEY_3", table[0][0]);
        Assert.AreEqual("KEY_2", table[1][0]);
        Assert.AreEqual("KEY_1", table[2][0]);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInCaseExpression_ShouldEvaluateBranchesCorrectly()
    {
        const string query =
            "let selected: string = 'KEY_2'; " +
            "select Key, case when Key = $selected then 'hit' else 'miss' end as Status " +
            "from #EnvironmentVariables.All() order by Key";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("miss", table[0][1]);
        Assert.AreEqual("hit", table[1][1]);
        Assert.AreEqual("miss", table[2][1]);
    }

    [TestMethod]
    public void WhenScriptVariableIsUsedInWindowPartitionAndOrder_ShouldCaptureVariableInWindowHelper()
    {
        const string query =
            "let groupValue: string = 'VALUE_A'; " +
            "let suffix: string = '_rank'; " +
            "select Key, RowNumber() over (" +
            "partition by case when Value = $groupValue then $groupValue else Value end " +
            "order by Key + $suffix) as RowNumber " +
            "from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateGroupedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);
        var rows = table
            .Select(row => new KeyValuePair<string, int>((string)row[0], Convert.ToInt32(row[1])))
            .OrderBy(row => row.Key)
            .ToArray();

        Assert.AreEqual(1, rows[0].Value);
        Assert.AreEqual(2, rows[1].Value);
        Assert.AreEqual(1, rows[2].Value);
    }

    [TestMethod]
    public void WhenScriptVariableCoexistsWithParameter_ShouldExposeOnlyParameterMetadata()
    {
        const string query =
            "param(key: string = 'KEY_1') " +
            "let label: string = 'constant'; " +
            "select $key, $label from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        Assert.HasCount(1, vm.ParameterDefinitions);
        Assert.AreEqual("key", vm.ParameterDefinitions[0].Name);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("constant", table[0][1]);
    }

    [TestMethod]
    public void WhenNullableScriptVariableIsNull_ShouldProjectNullValues()
    {
        const string query = "let value: int? = null; select $value from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.IsNull(table[1][0]);
        Assert.IsNull(table[2][0]);
    }

    [TestMethod]
    public void WhenManyScriptVariablesAreDeclared_ShouldResolveEachValueConsistently()
    {
        const string query =
            "let first: int = 1; " +
            "let second: int = $first + 1; " +
            "let third: int = $second + 1; " +
            "let fourth: int = $third + 1; " +
            "let fifth: int = $fourth + 1; " +
            "select $first, $second, $third, $fourth, $fifth, $fifth + $first " +
            "from #EnvironmentVariables.All() where Key = 'KEY_1'";
        var vm = CreateAndRunVirtualMachine(query, CreateExtendedEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual(4, table[0][3]);
        Assert.AreEqual(5, table[0][4]);
        Assert.AreEqual(6, Convert.ToInt32(table[0][5]));
    }

    [TestMethod]
    public void WhenDirectScriptVariableIsUsedAsSourceArgument_ShouldOpenSourceWithVariableValue()
    {
        const string query = "let key: string = 'KEY_2'; select Key, Value from #Parameterized.Items($key)";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var table = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        Assert.AreEqual(1, provider.OpenCount);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("VALUE_2", table[0][1]);
    }

    private static Dictionary<string, IEnumerable<EnvironmentVariableEntity>> CreateExtendedEnvironmentVariableSources()
    {
        var rows = new[]
        {
            new EnvironmentVariableEntity("KEY_1", "VALUE_1"),
            new EnvironmentVariableEntity("KEY_2", "VALUE_2"),
            new EnvironmentVariableEntity("KEY_3", "VALUE_3")
        };

        return new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            { "*", rows }
        };
    }

    private static Dictionary<string, IEnumerable<EnvironmentVariableEntity>> CreateGroupedEnvironmentVariableSources()
    {
        return new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "*",
                [
                    new EnvironmentVariableEntity("KEY_1", "VALUE_A"),
                    new EnvironmentVariableEntity("KEY_2", "VALUE_A"),
                    new EnvironmentVariableEntity("KEY_3", "VALUE_B")
                ]
            }
        };
    }

    private static Dictionary<string, IEnumerable<EnvironmentVariableEntity>> CreateUnionEnvironmentVariableSources()
    {
        return CreateExtendedEnvironmentVariableSources();
    }
}
