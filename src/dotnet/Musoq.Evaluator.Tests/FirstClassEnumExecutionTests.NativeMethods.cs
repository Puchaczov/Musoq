using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class FirstClassEnumExecutionTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0079")]
    public void NativeMethodEnumName_ShouldReturnDeclaredName()
    {
        const string query = "select EnumName(e.GetStatus()) from #schema.first() e";
        using var compiled = CreateAndRunVirtualMachine<NativeMethodEntity, NativeMethodLibrary>(query,
            [new NativeMethodEntity { Id = 1 }]);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(typeof(string), result.Columns.ElementAt(0).ColumnType);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { "Ready" });
    }

    [TestMethod]
    public void NullableAndFlagsNativeMethods_ShouldUsePrimitiveCarriers()
    {
        const string query = "select EnumName(e.GetNullableStatus()) as Name, EnumName(e.GetNullStatus()) as NullName, IsDefined(e.GetNullStatus()) as NullDefined, IsDefined(e.GetUndefinedStatus()) as Undefined, EnumValue(e.GetStatus()) as Value, HasAllFlags(e.GetAccess(), 'Read', 'Write') as Both from #schema.first() e";
        using var compiled = CreateAndRunVirtualMachine<NativeMethodEntity, NativeMethodLibrary>(query,
            [new NativeMethodEntity { Id = 1 }]);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("NullName", typeof(string)),
            ("NullDefined", typeof(bool)), ("Undefined", typeof(bool)), ("Value", typeof(short)), ("Both", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { "Ready", null, false, false, (short)20, true });
    }

    public sealed class NativeMethodEntity
    {
        public int Id { get; init; }
    }

    public enum NativeMethodStatus : short
    {
        Ready = 20
    }

    [Flags]
    public enum NativeMethodAccess : byte
    {
        Read = 1,
        Write = 2
    }

    public sealed class NativeMethodLibrary : LibraryBase
    {
        [BindableMethod]
        public NativeMethodStatus GetStatus() => NativeMethodStatus.Ready;

        [BindableMethod]
        public NativeMethodStatus? GetNullableStatus() => NativeMethodStatus.Ready;

        [BindableMethod]
        public NativeMethodStatus? GetNullStatus() => null;

        [BindableMethod]
        public NativeMethodStatus GetUndefinedStatus() => (NativeMethodStatus)21;

        [BindableMethod]
        public NativeMethodAccess GetAccess() => NativeMethodAccess.Read | NativeMethodAccess.Write;
    }

    public enum MinimalNativeMethodStatus
    {
        Ready
    }

    public sealed class MinimalNativeMethodEntity;

    public sealed class MinimalNativeMethodLibrary : LibraryBase
    {
        [BindableMethod]
        public MinimalNativeMethodStatus GetStatus() => MinimalNativeMethodStatus.Ready;
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-EnumNameReturnsTheDeclaredNameForNativeClrEnumMethodResults")]
    public void EnumNameReturnsTheDeclaredNameForNativeClrEnumMethodResults()
    {
        const string query = "select EnumName(GetStatus()) from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<MinimalNativeMethodEntity, MinimalNativeMethodLibrary>(
            query,
            [new MinimalNativeMethodEntity()]);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ready", table[0].ReadValue(0));
        Assert.AreEqual(typeof(string), table[0].ReadValue(0)?.GetType());
        var column = System.Linq.Enumerable.Single(table.Columns);
        Assert.AreEqual(typeof(string), column.ColumnType);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-EnumNameCompilesForNativeClrEnumMethodWithEmptySource")]
    public void EnumNameCompilesForNativeClrEnumMethodWithEmptySource()
    {
        const string query = "select EnumName(GetStatus()) from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<MinimalNativeMethodEntity, MinimalNativeMethodLibrary>(
            query,
            System.Array.Empty<MinimalNativeMethodEntity>());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        var column = System.Linq.Enumerable.Single(table.Columns);
        Assert.AreEqual(typeof(string), column.ColumnType);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-IsDefinedReturnsBooleanForNativeClrEnumMethodWithEmptySource")]
    public void IsDefinedReturnsBooleanForNativeClrEnumMethodWithEmptySource()
    {
        const string query = "select IsDefined(GetStatus()) from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<MinimalNativeMethodEntity, MinimalNativeMethodLibrary>(
            query,
            System.Array.Empty<MinimalNativeMethodEntity>());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        var column = System.Linq.Enumerable.Single(table.Columns);
        Assert.AreEqual(typeof(bool), column.ColumnType);
    }
}
