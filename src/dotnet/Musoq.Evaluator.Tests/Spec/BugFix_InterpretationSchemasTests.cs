using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Minimal reproducible tests for three bugs discovered during round-two stress testing:
///     1. Parser does not support when+check on the same field (wrong modifier parse order)
///     2. Computed fields referencing null conditional fields crash at runtime
///     3. Schema field names that clash with SQL keywords cause parse errors in expressions
/// </summary>
[TestClass]
public class BugFix_InterpretationSchemasTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Bug 1: when+check modifier order — parser rejects valid grammar

    /// <summary>
    ///     Spec grammar: FieldModifiers ::= PositionMod? ConditionMod? ValidationMod?
    ///     i.e. at? when? check? — but the parser parsed check before when,
    ///     so when both are present in the correct spec order, it fails.
    /// </summary>
    [TestMethod]
    public void Bug1_WhenAndCheck_OnSameField_ConditionTrue_ShouldParseAndValidate()
    {
        var query = @"
            binary Rec {
                Flag: byte,
                Val: short le when Flag <> 0 check Val >= 10
            };
            select r.Flag, r.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        using var ms = new MemoryStream();
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)42));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((short)42, table[0][1]);
    }

    /// <summary>
    ///     When condition is false, check is not evaluated — field is null.
    /// </summary>
    [TestMethod]
    public void Bug1_WhenAndCheck_OnSameField_ConditionFalse_ShouldBeNull()
    {
        var query = @"
            binary Rec {
                Flag: byte,
                Val: short le when Flag <> 0 check Val >= 10
            };
            select r.Flag, r.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     When condition true but check constraint fails → TryInterpret returns nothing.
    /// </summary>
    [TestMethod]
    public void Bug1_WhenAndCheck_CheckFails_TryInterpretReturnsNull()
    {
        var query = @"
            binary Rec {
                Flag: byte,
                Val: short le when Flag <> 0 check Val >= 100
            };
            select r.Flag, r.Val
            from #test.files() f
            outer apply TryInterpret(f.Content, 'Rec') r";

        using var ms = new MemoryStream();
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)5)); // 5 < 100, check fails

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]); // Val is null because TryInterpret yields null for failed check
    }

    #endregion

    #region Bug 2: Computed field crash when referencing null conditional field

    /// <summary>
    ///     Computed field referencing a conditional field that is null should
    ///     produce null, not crash with "Nullable object must have a value."
    /// </summary>
    [TestMethod]
    public void Bug2_ComputedFromNullConditional_ShouldBeNull_NotCrash()
    {
        var query = @"
            binary Msg {
                HasData: byte,
                Len: int le when HasData <> 0,
                Doubled: Len * 2
            };
            select m.HasData, m.Len, m.Doubled
            from #test.files() f
            cross apply Interpret(f.Content, 'Msg') m";

        var data = new byte[] { 0x00 }; // HasData=0 → Len=null → Doubled should be null
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    /// <summary>
    ///     Same scenario but condition is true — computes normally.
    /// </summary>
    [TestMethod]
    public void Bug2_ComputedFromPresentConditional_ShouldCompute()
    {
        var query = @"
            binary Msg {
                HasData: byte,
                Len: int le when HasData <> 0,
                Doubled: Len * 2
            };
            select m.HasData, m.Len, m.Doubled
            from #test.files() f
            cross apply Interpret(f.Content, 'Msg') m";

        using var ms = new MemoryStream();
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(10));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, Convert.ToInt32(table[0][2]));
    }

    /// <summary>
    ///     Computed field with addition referencing nullable conditional — both operands.
    /// </summary>
    [TestMethod]
    public void Bug2_ComputedAddition_BothOperandsConditional_ShouldBeNull()
    {
        var query = @"
            binary Rec {
                Flag: byte,
                A: short le when Flag <> 0,
                B: short le when Flag <> 0,
                Total: A + B
            };
            select r.Flag, r.Total
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    #endregion

    #region Bug 3: Schema field names clashing with SQL keywords in expressions

    /// <summary>
    ///     Field named "End" (a SQL keyword) should be usable as a schema field name
    ///     and in expressions referencing it.
    /// </summary>
    [TestMethod]
    public void Bug3_FieldNamedEnd_ShouldWorkInExpression()
    {
        var query = @"
            binary Span {
                Start: int le,
                End: int le,
                Length: End - Start
            };
            select s.Start, s.[End], s.Length
            from #test.files() f
            cross apply Interpret(f.Content, 'Span') s";

        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(10));
        ms.Write(BitConverter.GetBytes(30));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(30, table[0][1]);
        Assert.AreEqual(20, Convert.ToInt32(table[0][2]));
    }

    /// <summary>
    ///     Field named "Case" used in a when condition expression.
    /// </summary>
    [TestMethod]
    public void Bug3_FieldNamedCase_UsedInWhenCondition()
    {
        var query = @"
            binary Rec {
                Case: byte,
                Val: int le when Case <> 0
            };
            select r.[Case], r.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        using var ms = new MemoryStream();
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(42));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(42, table[0][1]);
    }

    /// <summary>
    ///     Field named "Select" used in a computed expression.
    /// </summary>
    [TestMethod]
    public void Bug3_FieldNamedSelect_InComputedExpression()
    {
        var query = @"
            binary Rec {
                Select: short le,
                Double: Select * 2
            };
            select r.[Select], r.[Double]
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = BitConverter.GetBytes((short)25);
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)25, table[0][0]);
        Assert.AreEqual(50, Convert.ToInt32(table[0][1]));
    }

    #endregion
}
