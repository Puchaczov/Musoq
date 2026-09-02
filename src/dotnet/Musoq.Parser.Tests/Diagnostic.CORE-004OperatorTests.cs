using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore004OperatorTests
{
    [TestMethod]
    public void LogicalOperators_ShouldGiveAndHigherPrecedenceThanOr()
    {
        var expression = ParseWhereExpression("true or false and false");

        var or = Assert.IsInstanceOfType<OrNode>(expression);
        Assert.IsInstanceOfType<BooleanNode>(or.Left);
        Assert.IsInstanceOfType<AndNode>(or.Right);
    }

    [TestMethod]
    public void Parentheses_ShouldOverrideLogicalPrecedence()
    {
        var expression = ParseWhereExpression("(true or false) and false");

        var and = Assert.IsInstanceOfType<AndNode>(expression);
        Assert.IsInstanceOfType<OrNode>(and.Left);
        Assert.IsInstanceOfType<BooleanNode>(and.Right);
    }

    [TestMethod]
    [DataRow("not false")]
    [DataRow("not (true or false)")]
    [DataRow("not 1 = 2")]
    public void PrefixNot_ShouldAcceptBooleanAndPredicateOperands(string expression)
    {
        var parsed = ParseWithDiagnostics($"select 1 from #some.a() where {expression}");

        Assert.IsTrue(parsed.Success, parsed.FormatDiagnostics());
        Assert.IsEmpty(parsed.Diagnostics, parsed.FormatDiagnostics());
        Assert.IsInstanceOfType<NotNode>(ParseWhereExpression(expression));
    }

    [TestMethod]
    public void ArithmeticAndBitwiseOperators_ShouldPreserveDocumentedPrecedenceAndAssociativity()
    {
        var shift = ParseSelectExpression("1 << 2 + 1");
        var shiftNode = Assert.IsInstanceOfType<LeftShiftNode>(shift);
        Assert.IsInstanceOfType<AddNode>(shiftNode.Right);

        var bitwise = ParseSelectExpression("1 & 2 | 3");
        var bitwiseOr = Assert.IsInstanceOfType<BitwiseOrNode>(bitwise);
        Assert.IsInstanceOfType<BitwiseAndNode>(bitwiseOr.Left);

        var coalesce = ParseSelectExpression("null ?? null ?? 'fallback'");
        var coalesceNode = Assert.IsInstanceOfType<CoalesceNode>(coalesce);
        Assert.IsInstanceOfType<CoalesceNode>(coalesceNode.Right);
    }

    [TestMethod]
    [DataRow("select 1 from #some.a() where 1 + * 2", DiagnosticCode.MQ2019_InvalidOperator)]
    [DataRow("select 1 from #some.a() where 1 + )", DiagnosticCode.MQ2020_MissingOperand)]
    public void InvalidOperatorForms_ShouldReportTypedParseDiagnostics(string query, DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static Node ParseWhereExpression(string expression)
    {
        var result = ParseWithDiagnostics($"select 1 from #some.a() where {expression}");
        Assert.IsTrue(result.Success, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Where?.Expression ??
               throw new InvalidOperationException("Expected the parsed query to contain a WHERE clause.");
    }

    private static Node ParseSelectExpression(string expression)
    {
        var result = ParseWithDiagnostics($"select {expression} from #some.a()");
        Assert.IsTrue(result.Success, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Select.Fields.Single().Expression;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
