using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TableSymbolTransformationTests
{
    [TestMethod]
    public void MakeNullableIfPossible_ShouldKeepProviderSchemaAndMetadata()
    {
        var fixture = CreateSymbol("left", hasAlias: false);

        var transformed = fixture.Symbol.MarkAliasesAsMaybeMissing(["left"]).MakeNullableIfPossible();
        var binding = transformed.GetTableByAlias("left");

        Assert.AreSame(fixture.Schema, binding.Schema);
        Assert.AreNotSame(fixture.Table, binding.Table);
        Assert.AreEqual(typeof(int?), binding.Table.Columns.Single(column => column.ColumnName == "Value").ColumnType);
        Assert.AreEqual(typeof(TestEntity), binding.Table.Metadata?.TableEntityType);
        Assert.IsFalse(transformed.HasAlias);
        Assert.IsTrue(transformed.CanAliasBeMissing("left"));
    }

    [TestMethod]
    public void WithAdditionalColumn_ShouldKeepProviderSchemaAndEntityMetadata()
    {
        var fixture = CreateSymbol("left");

        var transformed = fixture.Symbol.WithAdditionalColumn("left", new SchemaColumn("Ordinality", 1, typeof(long)));
        var binding = transformed.GetTableByAlias("left");

        Assert.AreSame(fixture.Schema, binding.Schema);
        Assert.AreEqual(typeof(TestEntity), binding.Table.Metadata?.TableEntityType);
        CollectionAssert.AreEqual(new[] { "Value", "Name", "Ordinality" }, binding.Table.Columns.Select(column => column.ColumnName).ToArray());
    }

    [TestMethod]
    public void LimitColumnsTo_ShouldKeepProviderSchemaAndHasAlias()
    {
        var fixture = CreateSymbol("left", hasAlias: false);

        var transformed = fixture.Symbol.LimitColumnsTo(new Dictionary<string, string[]> { ["left"] = ["Value"] });
        var binding = transformed.GetTableByAlias("left");

        Assert.AreSame(fixture.Schema, binding.Schema);
        Assert.AreEqual(typeof(TestEntity), binding.Table.Metadata?.TableEntityType);
        Assert.AreEqual("Value", binding.Table.Columns.Single().ColumnName);
        Assert.IsFalse(transformed.HasAlias);
    }

    [TestMethod]
    public void WithFullTableName_ShouldUseProviderSchemaWhenIdentityRemainsTheAlias()
    {
        var fixture = CreateSymbol("left");

        var transformed = fixture.Symbol.WithFullTableName("left");
        var binding = transformed.GetTableByAlias("left");

        Assert.AreSame(fixture.Schema, binding.Schema);
    }

    [TestMethod]
    public void WithFullTableName_ShouldUseTransitionSchemaForSyntheticIdentity()
    {
        var fixture = CreateSymbol("left");

        var transformed = fixture.Symbol.WithFullTableName("renamed");
        var binding = transformed.GetTableByAlias("renamed");

        Assert.IsInstanceOfType(binding.Schema, typeof(TransitionSchema));
        Assert.AreSame(fixture.Schema, transformed.GetTableByAlias("left").Schema);
    }

    [TestMethod]
    public void MergeSymbols_ShouldRetainBothProviderSchemaOwnersInOrder()
    {
        var left = CreateSymbol("left");
        var right = CreateSymbol("right");

        var merged = left.Symbol.MergeSymbols(right.Symbol);

        CollectionAssert.AreEqual(new[] { "left", "right" }, merged.CompoundTables);
        Assert.AreSame(left.Schema, merged.GetTableByAlias("left").Schema);
        Assert.AreSame(right.Schema, merged.GetTableByAlias("right").Schema);
    }

    [TestMethod]
    public void MergeSymbols_ShouldExpandACompoundRightOperand()
    {
        var left = CreateSymbol("left");
        var right = CreateSymbol("right").Symbol.MergeSymbols(CreateSymbol("third").Symbol);

        var merged = left.Symbol.MergeSymbols(right);

        CollectionAssert.AreEqual(new[] { "left", "right", "third" }, merged.CompoundTables);
        Assert.IsTrue(merged.ContainsAlias("right"));
        Assert.IsTrue(merged.ContainsAlias("third"));
    }

    [TestMethod]
    public void MergeSymbols_ShouldBeAssociativeForAliasOrderAndOwners()
    {
        var a = CreateSymbol("a");
        var b = CreateSymbol("b");
        var c = CreateSymbol("c");

        var leftAssociated = a.Symbol.MergeSymbols(b.Symbol).MergeSymbols(c.Symbol);
        var rightAssociated = a.Symbol.MergeSymbols(b.Symbol.MergeSymbols(c.Symbol));

        CollectionAssert.AreEqual(leftAssociated.CompoundTables, rightAssociated.CompoundTables);
        Assert.AreSame(a.Schema, rightAssociated.GetTableByAlias("a").Schema);
        Assert.AreSame(b.Schema, rightAssociated.GetTableByAlias("b").Schema);
        Assert.AreSame(c.Schema, rightAssociated.GetTableByAlias("c").Schema);
    }

    [TestMethod]
    public void MergeSymbols_ShouldCopyMissingFlagsFromBothOperands()
    {
        var left = CreateSymbol("left").Symbol.MarkAliasesAsMaybeMissing(["left"]);
        var right = CreateSymbol("right").Symbol.MarkAliasesAsMaybeMissing(["right"]);

        var merged = left.MergeSymbols(right);

        Assert.IsTrue(merged.CanAliasBeMissing("left"));
        Assert.IsTrue(merged.CanAliasBeMissing("right"));
    }

    private static (TableSymbol Symbol, ISchema Schema, ISchemaTable Table) CreateSymbol(
        string alias,
        bool hasAlias = true)
    {
        var table = new ProviderTable(
            [
                new SchemaColumn("Value", 0, typeof(int)),
                new SchemaColumn("Name", 1, typeof(string))
            ],
            typeof(TestEntity));
        var schema = new ProviderSchema(alias, table);
        return (new TableSymbol(alias, schema, table, hasAlias), schema, table);
    }

    private sealed class TestEntity;

    private sealed class ProviderSchema(string name, ISchemaTable table)
        : SchemaBase(name, new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => table;
    }

    private sealed class ProviderTable(ISchemaColumn[] columns, Type entityType) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns;

        public SchemaTableMetadata Metadata { get; } = new(entityType);

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
