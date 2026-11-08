using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    #region Scope Tests

    [TestMethod]
    public void Scope_AddScope_CreatesChildScope()
    {
        var parent = new Scope(null, 0, "parent");

        var child = parent.AddScope("child");

        Assert.HasCount(1, parent.Child);
        Assert.AreEqual("child", child.Name);
        Assert.AreSame(parent, child.Parent);
    }

    [TestMethod]
    public void Scope_AddScope_MultipleChildren()
    {
        var parent = new Scope(null, 0, "parent");

        var child1 = parent.AddScope("child1");
        var child2 = parent.AddScope("child2");

        Assert.HasCount(2, parent.Child);
        Assert.AreEqual(0, child1.SelfIndex);
        Assert.AreEqual(1, child2.SelfIndex);
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsTrueForLocalAttribute()
    {
        var scope = new Scope(null, 0) { ["test"] = "value" };

        Assert.IsTrue(scope.ContainsAttribute("test"));
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsTrueForParentAttribute()
    {
        var parent = new Scope(null, 0) { ["test"] = "value" };
        var child = parent.AddScope();

        Assert.IsTrue(child.ContainsAttribute("test"));
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsFalseForMissingAttribute()
    {
        var scope = new Scope(null, 0);

        Assert.IsFalse(scope.ContainsAttribute("nonexistent"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsTrueForSelf()
    {
        var scope = new Scope(null, 0, "myScope");

        Assert.IsTrue(scope.IsInsideNamedScope("myScope"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsTrueForAncestor()
    {
        var grandparent = new Scope(null, 0, "grandparent");
        var parent = grandparent.AddScope("parent");
        var child = parent.AddScope("child");

        Assert.IsTrue(child.IsInsideNamedScope("grandparent"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsFalseForMissingScope()
    {
        var scope = new Scope(null, 0, "myScope");

        Assert.IsFalse(scope.IsInsideNamedScope("otherScope"));
    }

    [TestMethod]
    public void Scope_Indexer_ReturnsLocalValue()
    {
        var scope = new Scope(null, 0) { ["key"] = "value" };

        Assert.AreEqual("value", scope["key"]);
    }

    [TestMethod]
    public void Scope_Indexer_ReturnsParentValue()
    {
        var parent = new Scope(null, 0) { ["key"] = "parentValue" };
        var child = parent.AddScope();

        Assert.AreEqual("parentValue", child["key"]);
    }

    [TestMethod]
    public void Scope_ScopeSymbolTable_IsNotNull()
    {
        var scope = new Scope(null, 0);

        Assert.IsNotNull(scope.ScopeSymbolTable);
    }

    #endregion

    #region SymbolTable Tests

    [TestMethod]
    public void SymbolTable_AddSymbol_CanBeRetrieved()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();

        table.AddSymbol("key", symbol);
        var result = table.GetSymbol("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_GetSymbolGeneric_ReturnsTypedSymbol()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.GetSymbol<AliasesSymbol>("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsTrue_WhenFound()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.TryGetSymbol<AliasesSymbol>("key", out var foundSymbol);

        Assert.IsTrue(result);
        Assert.AreSame(symbol, foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsFalse_WhenNotFound()
    {
        var table = new SymbolTable();

        var result = table.TryGetSymbol<AliasesSymbol>("key", out var foundSymbol);

        Assert.IsFalse(result);
        Assert.IsNull(foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsFalse_WhenWrongType()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.TryGetSymbol<FieldsNamesSymbol>("key", out var foundSymbol);

        Assert.IsFalse(result);
        Assert.IsNull(foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_AddOrGetSymbol_CreatesNewWhenNotExists()
    {
        var table = new SymbolTable();

        var result = table.AddOrGetSymbol<AliasesSymbol>("key");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SymbolTable_AddOrGetSymbol_ReturnsExistingWhenExists()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.AddOrGetSymbol<AliasesSymbol>("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_AddSymbolIfNotExist_DoesNotOverwrite()
    {
        var table = new SymbolTable();
        var symbol1 = new AliasesSymbol();
        var symbol2 = new AliasesSymbol();
        table.AddSymbol("key", symbol1);

        table.AddSymbolIfNotExist("key", symbol2);

        Assert.AreSame(symbol1, table.GetSymbol("key"));
    }

    [TestMethod]
    public void SymbolTable_MoveSymbol_MovesToNewKey()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("oldKey", symbol);

        table.MoveSymbol("oldKey", "newKey");

        Assert.AreSame(symbol, table.GetSymbol("newKey"));
    }

    [TestMethod]
    public void SymbolTable_UpdateSymbol_ReplacesSymbol()
    {
        var table = new SymbolTable();
        var symbol1 = new AliasesSymbol();
        var symbol2 = new AliasesSymbol();
        table.AddSymbol("key", symbol1);

        table.UpdateSymbol("key", symbol2);

        Assert.AreSame(symbol2, table.GetSymbol("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsTrue_WhenMatches()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        Assert.IsTrue(table.SymbolIsOfType<AliasesSymbol>("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsFalse_WhenNotMatches()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        Assert.IsFalse(table.SymbolIsOfType<FieldsNamesSymbol>("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsFalse_WhenKeyNotFound()
    {
        var table = new SymbolTable();

        Assert.IsFalse(table.SymbolIsOfType<AliasesSymbol>("key"));
    }

    #endregion

    #region AliasesSymbol Tests

    [TestMethod]
    public void AliasesSymbol_AddAlias_CanBeFound()
    {
        var symbol = new AliasesSymbol();

        symbol.AddAlias("test");

        Assert.IsTrue(symbol.ContainsAlias("test"));
    }

    [TestMethod]
    public void AliasesSymbol_ContainsAlias_ReturnsFalse_WhenNotFound()
    {
        var symbol = new AliasesSymbol();

        Assert.IsFalse(symbol.ContainsAlias("nonexistent"));
    }

    #endregion
}
