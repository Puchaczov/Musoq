namespace Musoq.Parser.Syntax;

/// <summary>Identifies a source-level grammar construct independently of semantic lowering.</summary>
public enum SyntaxKind
{
    Document, Statement, Query, SelectClause, FromClause, WhereClause, GroupByClause,
    HavingClause, OrderByClause, WindowClause, QualifyClause, PagingClause,
    Join, Apply, SetExpression, Cte, Expression, Predicate, ParenthesizedExpression,
    FunctionCall, ArgumentList, CaseExpression, ArrayLiteral, RecordLiteral, ValuesSource,
    ParameterBlock, VariableDeclaration, EnumDeclaration, TableDeclaration, CoupleDeclaration,
    BinarySchema, TextSchema, SchemaField, TypeSyntax, Identifier, Alias, UtilityStatement,
    WindowSpecification, WindowFrame, Subquery, Keyword, TypeArgumentList
}
