using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class ParserErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "The parser encountered a token that does not fit the expected SQL grammar at this position.",
            [
                "Check for missing keywords, commas, or parentheses near this location.",
                "Verify the query follows Musoq SQL syntax."
            ],
            "Core Spec - Statement Structure");

        yield return Entry(
            DiagnosticCode.MQ2002_MissingToken,
            "A required keyword, delimiter, or closing token is missing at this position.",
            [
                "Insert the missing keyword or delimiter near the highlighted location.",
                "Check for a missing FROM clause, comma, or closing parenthesis."
            ],
            "Core Spec - Statement Structure");

        yield return Entry(
            DiagnosticCode.MQ2003_InvalidExpression,
            "The parser could not form a valid expression from the tokens at this location.",
            [
                "Check that operators have operands on both sides.",
                "Wrap nested expressions in parentheses when precedence is unclear."
            ],
            "Core Spec - Expressions");

        yield return Entry(
            DiagnosticCode.MQ2004_MissingFromClause,
            "Every SELECT query in Musoq requires a FROM clause specifying a data source.",
            [
                "Add a FROM clause: SELECT ... FROM #schema.method() alias.",
                "For constant expressions, use: SELECT 1 FROM #system.dual() d."
            ],
            "Core Spec - FROM Clause");

        yield return Entry(
            DiagnosticCode.MQ2005_InvalidSelectList,
            "The SELECT list is empty, malformed, or contains expressions that cannot be separated into projections.",
            [
                "Add at least one projection after SELECT.",
                "Separate projection expressions with commas."
            ],
            "Core Spec - SELECT Clause");

        yield return Entry(
            DiagnosticCode.MQ2006_MissingGroupByColumn,
            "The GROUP BY clause is missing a column or expression after a comma or keyword.",
            [
                "Add the missing GROUP BY expression.",
                "Remove the dangling comma if no additional grouping expression is needed."
            ],
            "Core Spec - GROUP BY Clause");

        yield return Entry(
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            "The JOIN condition is missing or is not a valid boolean expression.",
            [
                "Add an ON clause with a comparison between the joined sources.",
                "Make sure the ON expression evaluates to a boolean value."
            ],
            "Core Spec - JOIN Clause");

        yield return Entry(
            DiagnosticCode.MQ2008_DuplicateAlias,
            "The same alias was declared more than once in the parsed query scope.",
            [
                "Rename one of the aliases.",
                "Remove the duplicate alias if it was introduced accidentally."
            ],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "The ORDER BY clause contains an expression that the parser cannot accept in this position.",
            [
                "Use a projection alias, column reference, or valid expression in ORDER BY.",
                "Move DESC or ASC after the expression it modifies."
            ],
            "Core Spec - ORDER BY Clause");

        yield return Entry(
            DiagnosticCode.MQ2010_MissingClosingParenthesis,
            "An opening parenthesis does not have a matching closing parenthesis.",
            [
                "Add the missing closing parenthesis.",
                "Check nested function calls and subqueries near the highlighted span."
            ],
            "Core Spec - Expressions");

        yield return Entry(
            DiagnosticCode.MQ2011_MissingClosingBracket,
            "An opening bracket does not have a matching closing bracket.",
            [
                "Add the missing closing bracket.",
                "Check array indexing and bracketed path expressions."
            ],
            "Core Spec - Array Access");

        yield return Entry(
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            "A DEFINE SCHEMA block does not match the expected schema grammar.",
            [
                "Check the schema name, field list, and field type syntax.",
                "Verify nested schema references use defined schema names."
            ],
            "Binary/Text Spec - Schema Definitions");

        yield return Entry(
            DiagnosticCode.MQ2013_InvalidCTE,
            "The Common Table Expression syntax is invalid or incomplete.",
            [
                "Verify CTE format: WITH name AS (SELECT ...) SELECT ... FROM name.",
                "Ensure the CTE body is a valid SELECT statement."
            ],
            "Core Spec - CTE");

        yield return Entry(
            DiagnosticCode.MQ2014_TrailingComma,
            "A comma appears after the last item in a list where another item was expected.",
            [
                "Remove the trailing comma.",
                "Add the missing item after the comma."
            ],
            "Core Spec - Lists");

        yield return Entry(
            DiagnosticCode.MQ2015_LeadingComma,
            "A comma appears before the first item in a list.",
            [
                "Remove the leading comma.",
                "Move the comma between two list items."
            ],
            "Core Spec - Lists");

        yield return Entry(
            DiagnosticCode.MQ2016_IncompleteStatement,
            "The query ended before Musoq could form a complete statement.",
            [
                "Complete the statement with the missing clause or expression.",
                "Start with a full query shape such as: SELECT ... FROM #schema.method() alias."
            ],
            "Core Spec - Statement Structure");

        yield return Entry(
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            "The parser reached the end of the script while more tokens were still required.",
            [
                "Complete the current clause or expression.",
                "Check for an unclosed parenthesis, bracket, CASE expression, or subquery."
            ],
            "Core Spec - Statement Structure");

        yield return Entry(
            DiagnosticCode.MQ2018_MissingOperator,
            "Two expressions appear next to each other without an operator between them.",
            [
                "Insert the intended operator such as =, AND, OR, +, or -.",
                "Add a comma if these were meant to be separate list items."
            ],
            "Core Spec - Operators");

        yield return Entry(
            DiagnosticCode.MQ2019_InvalidOperator,
            "An operator was used in a position or form that is not valid for Musoq SQL.",
            [
                "Check that the operator is supported by Musoq.",
                "Verify the operator appears between compatible operands."
            ],
            "Core Spec - Operators");

        yield return Entry(
            DiagnosticCode.MQ2020_MissingOperand,
            "An operator is missing the expression it should operate on.",
            [
                "Add the missing expression before or after the operator.",
                "Remove the operator if it is not needed."
            ],
            "Core Spec - Operators");

        yield return Entry(
            DiagnosticCode.MQ2021_UnclosedFunctionCall,
            "A function call was opened but not closed before the statement ended.",
            [
                "Add the closing parenthesis for the function call.",
                "Check that all function arguments are separated with commas."
            ],
            "Core Spec - Functions");

        yield return Entry(
            DiagnosticCode.MQ2022_InvalidAlias,
            "The alias syntax is malformed or uses a token that cannot be an alias.",
            [
                "Use an identifier for the alias.",
                "If the alias follows an expression, write it after AS or immediately after the expression where supported."
            ],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ2023_MissingAsKeyword,
            "The parser expected an AS keyword before an alias in this position.",
            [
                "Insert AS before the alias.",
                "Remove the alias if it was not intended."
            ],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ2024_InvalidSubquery,
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            [
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            ],
            "Core Spec - Subqueries");

        yield return Entry(
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            "A query statement is missing the SELECT keyword.",
            [
                "Start the query with SELECT.",
                "If this is a CTE, place the SELECT statement after the WITH definitions."
            ],
            "Core Spec - SELECT Clause");

        yield return Entry(
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            "Musoq supports searched CASE only (CASE WHEN ... THEN ... END), not simple CASE (CASE expr WHEN value ...).",
            ["Rewrite as: CASE WHEN expr = value THEN result ELSE default END."],
            "Core Spec - CASE Expressions");

        yield return Entry(
            DiagnosticCode.MQ2027_MissingWhenClause,
            "A CASE expression is missing a WHEN clause.",
            [
                "Add at least one WHEN condition.",
                "Use searched CASE syntax: CASE WHEN condition THEN value END."
            ],
            "Core Spec - CASE Expressions");

        yield return Entry(
            DiagnosticCode.MQ2028_MissingThenClause,
            "A CASE WHEN branch is missing the THEN result expression.",
            [
                "Add THEN followed by the value for that branch.",
                "Check that the WHEN condition is complete before THEN."
            ],
            "Core Spec - CASE Expressions");

        yield return Entry(
            DiagnosticCode.MQ2029_MissingEndKeyword,
            "A CASE expression is missing its closing END keyword.",
            [
                "Add END after the final CASE branch.",
                "Check nested CASE expressions for balanced END keywords."
            ],
            "Core Spec - CASE Expressions");

        yield return Entry(
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "The query uses syntax that Musoq does not support or that is not valid in this position.",
            [
                "Rewrite the clause using Musoq SQL syntax.",
                "If this came from another SQL dialect, check the Musoq equivalent keywords."
            ],
            "Core Spec - Statement Structure");

        yield return Entry(
            DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
            "The script parameter declaration does not match Musoq's param(name: type) syntax.",
            [
                "Write parameters as: param(author: string, limit: int = 10).",
                "Put the parameter name before the type and separate it from the type with a colon."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
            "The query uses a parameter declaration style from another language that Musoq does not support.",
            [
                "Use Musoq syntax: param(author: string).",
                "Reference declared parameters inside the query as $author."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
            "The script variable declaration does not match Musoq's let name: type = value syntax.",
            [
                "Write script variables as: let topic: string = 'important'.",
                "Declare script variables before the first expression that references them."
            ],
            "Core Spec - Script Variables");
    }
}
