using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SchemaErrorTests : NegativeTestsBase
{
    #region 2.1 Unknown Schema / Table / Column

    [TestMethod]
    public void SE001_NonexistentSchema_ShouldThrowSchemaError()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CompileQuery("SELECT * FROM #nonexistent.table()"));
    }

    [TestMethod]
    public void SE002_NonexistentTableInValidSchema_ShouldThrowSchemaError()
    {
        
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT * FROM #test.nonexistent()"));
    }

    [TestMethod]
    public void SE003_NonexistentColumn_ShouldThrowUnknownColumnError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT NonExistentColumn FROM #test.people()"));
    }

    [TestMethod]
    public void SE004_NonexistentColumnInWhere_ShouldThrowUnknownColumnError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE FakeColumn > 10"));
    }

    [TestMethod]
    public void SE005_NonexistentColumnInGroupBy_ShouldThrowUnknownColumnError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT Count(1) FROM #test.people() GROUP BY Nonexistent"));
    }

    [TestMethod]
    public void SE006_NonexistentColumnInOrderBy_ShouldThrowUnknownColumnError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY Nonexistent"));
    }

    [TestMethod]
    public void SE007_NonexistentColumnInHaving_ShouldThrowUnknownColumnError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City HAVING Sum(Nonexistent) > 100"));
    }

    [TestMethod]
    public void SE008_NonexistentColumnInJoinCondition_ShouldThrowError()
    {
        
        Assert.Throws<VisitorException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() o ON p.FakeId = o.PersonId"));
    }

    [TestMethod]
    public void SE009_NonexistentColumnInCaseExpression_ShouldThrowError()
    {
        
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT CASE NonExistent WHEN 1 THEN 'a' ELSE 'b' END FROM #test.people()"));
    }

    #endregion

    #region 2.2 Alias Errors

    [TestMethod]
    public void SE010_ReferenceToUndefinedAlias_ShouldThrowError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT x.Name FROM #test.people() p"));
    }

    [TestMethod]
    public void SE013_DuplicateCteNames_ShouldThrowError()
    {
        
        Assert.Throws<ArgumentException>(() =>
            CompileQuery("WITH A AS (SELECT 1 AS X FROM #test.single()), A AS (SELECT 2 AS X FROM #test.single()) SELECT * FROM A a"));
    }

    [TestMethod]
    public void SE014_DuplicateTableAliases_ShouldThrowError()
    {
        Assert.Throws<AliasAlreadyUsedException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() p ON p.Id = p.PersonId"));
    }

    [TestMethod]
    public void SE016_WrongAliasPrefixForColumn_ShouldThrowError()
    {
        
        Assert.Throws<VisitorException>(() =>
            CompileQuery("SELECT p.Amount FROM #test.people() p INNER JOIN #test.orders() o ON p.Id = o.PersonId"));
    }

    #endregion

    #region 2.4 Property Access Errors

    [TestMethod]
    public void SE031_DeepPropertyAccessWithInvalidIntermediate_ShouldThrowError()
    {
        Assert.Throws<UnknownPropertyException>(() =>
            CompileQuery("SELECT Info.Label.Nonexistent FROM #test.nested()"));
    }

    #endregion

    #region 2.6 CTE Forward Reference / Scope Errors

    [TestMethod]
    public void SE050_CteForwardReference_ShouldThrowError()
    {
        
        Assert.Throws<Exception>(() =>
            CompileQuery("WITH First AS (SELECT * FROM Second s), Second AS (SELECT Name FROM #test.people()) SELECT * FROM First f"));
    }

    #endregion
}
