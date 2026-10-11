using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    public void DynamicSqlMembership_ShouldEvaluateOperandOnceAndStopAtFirstMatch()
        => AssertMembershipEvaluation("in", new MembershipRow(7, 7, 8), true, ["operand", "first"]);

    [TestMethod]
    public void DynamicSqlMembership_WithNullOperand_ShouldEvaluateMembersAndReturnUnknown()
        => AssertMembershipEvaluation("in", new MembershipRow(null, null, 8), null, ["operand", "first", "second"]);

    [TestMethod]
    public void DynamicSqlMembership_WithNullMemberAndNoMatch_ShouldReturnUnknown()
        => AssertMembershipEvaluation("in", new MembershipRow(7, null, 8), null, ["operand", "first", "second"]);

    [TestMethod]
    public void DynamicSqlMembership_WithNullBeforeMatch_ShouldReturnTrue()
        => AssertMembershipEvaluation("in", new MembershipRow(7, null, 7), true, ["operand", "first", "second"]);

    [TestMethod]
    public void DynamicLiteralContains_ShouldEvaluateAllMembersBeforeMatching()
        => AssertMembershipEvaluation("contains", new MembershipRow(7, 7, 8), true, ["operand", "first", "second"]);

    [TestMethod]
    public void DynamicLiteralContains_WithNullOperandAndMember_ShouldMatchAndEvaluateAllMembers()
        => AssertMembershipEvaluation("contains", new MembershipRow(null, null, 8), true, ["operand", "first", "second"]);

    private void AssertMembershipEvaluation(string operation, MembershipRow input, bool? expected, string[] expectedEvents)
    {
        var sql = $"select Record(Value, 'operand') {operation} (Record(First, 'first'), Record(Second, 'second')) as Matched from #schema.first()";
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"),
            LibraryExecutionFixture.CreateProvider<MembershipRow, MembershipLibrary>([input]), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        var columnType = operation == "in" ? typeof(bool?) : typeof(bool);
        TableMaterializationTestHelper.AssertColumns(table, ("Matched", columnType));
        bool? actual = operation == "in" ? rows[0].GetValue<bool?>(0) : rows[0].GetValue<bool>(0);
        Assert.AreEqual(expected, actual);
        CollectionAssert.AreEqual(expectedEvents, input.Events.ToArray());
    }

    public sealed class MembershipRow(int? value, int? first, int? second)
    {
        public int? Value { get; } = value;
        public int? First { get; } = first;
        public int? Second { get; } = second;
        public List<string> Events { get; } = [];
    }

    public sealed class MembershipLibrary : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public int? Record([InjectSpecificSource(typeof(MembershipRow))] MembershipRow row, int? value, string label)
        {
            row.Events.Add(label);
            return value;
        }
    }
}
