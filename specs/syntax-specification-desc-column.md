# Syntax Specification: DESC COLUMN Command

## 1. Overview

This document specifies the syntax for a new `desc` command variant that describes a specific column from a schema method's output table.

## 2. Grammar

### 2.1 BNF Notation

```bnf
<desc-statement>     ::= "DESC" <desc-target> [<column-clause>] [";"]

<desc-target>        ::= <schema-ref>
                       | <schema-ref> "." <method-name>
                       | <schema-ref> "." <method-call>
                       | "FUNCTIONS" <schema-ref>
                       | "FUNCTIONS" <schema-ref> "." <method-name>
                       | "FUNCTIONS" <schema-ref> "." <method-call>

<schema-ref>         ::= "#" <identifier>
                       | <identifier>

<method-name>        ::= <identifier>

<method-call>        ::= <identifier> "(" [<argument-list>] ")"

<argument-list>      ::= <argument> ["," <argument>]*

<argument>           ::= <string-literal>
                       | <numeric-literal>
                       | <boolean-literal>

<column-clause>      ::= "COLUMN" <column-name>

<column-name>        ::= <identifier>
                       | <string-literal>
                       | <quoted-identifier>

<identifier>         ::= [a-zA-Z_][a-zA-Z0-9_]*

<string-literal>     ::= "'" <characters> "'"

<quoted-identifier>  ::= "[" <characters> "]"

<boolean-literal>    ::= "TRUE" | "FALSE"

<numeric-literal>    ::= <integer> | <decimal>
```

### 2.2 Extended Grammar for Column Clause

The `<column-clause>` is only valid after a `<method-call>` (method with parentheses):

```bnf
<desc-with-column>   ::= "DESC" <schema-ref> "." <method-call> "COLUMN" <column-name> [";"]
```

## 3. Syntax Examples

### 3.1 Valid Syntax

```sql
-- Basic column description
desc #git.commits() column AuthorName

-- With method arguments
desc #git.commits('/path/to/repo') column CommitDate

-- With multiple arguments
desc #files.scan('/path', '*.cs', true) column FileName

-- Case insensitive keywords
DESC #git.commits() COLUMN AuthorName
Desc #git.commits() Column AuthorName

-- Without hash prefix (hash-optional syntax)
desc git.commits() column AuthorName

-- With semicolon
desc #git.commits() column AuthorName;

-- Column name as string literal (for special characters)
desc #data.table() column 'Column With Spaces'

-- Column name as quoted identifier
desc #data.table() column [Column-Name]

-- With whitespace
desc    #git.commits()    column    AuthorName

-- Multi-line
desc 
    #git.commits() 
    column 
        AuthorName
```

### 3.2 Invalid Syntax

```sql
-- Missing column name (ERROR)
desc #git.commits() column

-- Column clause without method call parentheses (ERROR)
desc #git.commits column AuthorName

-- Column clause on schema-only desc (ERROR)
desc #git column AuthorName

-- Column clause with functions keyword (ERROR - not applicable)
desc functions #git column AuthorName

-- Multiple column clauses (ERROR)
desc #git.commits() column Name column Date
```

## 4. Semantic Rules

### 4.1 Column Name Matching

1. Column names are matched **case-insensitively**
2. Exact name match is performed first
3. If no exact match, partial matching is NOT performed (avoid ambiguity)

### 4.2 Column Clause Applicability

The `column` clause is only valid when:
1. The desc target is a method call with parentheses: `method()`
2. The method returns a table with columns

The `column` clause is NOT valid with:
1. Schema-only desc: `desc #schema`
2. Method-only desc (without parentheses): `desc #schema.method`
3. Functions desc: `desc functions #schema`

### 4.3 Output Table

When `column` clause is used, the output table has the following schema:

| Column | Type | Description |
|--------|------|-------------|
| Name | string | Full property path (e.g., `Name`, `Author.Email`) |
| Index | int | Column index in parent table (0 for nested properties) |
| Type | string | Full .NET type name |

### 4.4 Complex Type Expansion

For columns with complex (non-primitive) types:
1. The column itself is listed first
2. Nested properties are expanded up to 3 levels deep
3. Property paths use dot notation: `Column.Property.SubProperty`

**Example Output for `desc #git.commits() column Author`:**

| Name | Index | Type |
|------|-------|------|
| Author | 3 | Git.AuthorInfo |
| Author.Name | 0 | System.String |
| Author.Email | 0 | System.String |
| Author.Date | 0 | System.DateTime |

### 4.5 Column Not Found Behavior

When the specified column does not exist:
1. Return an empty table (no rows)
2. The table schema remains the same (Name, Index, Type columns)
3. No exception is thrown (consistent with SQL NULL semantics)

## 5. Token Definition

### 5.1 New Token: COLUMN

```
Token Name: Column
Token Type: TokenType.Column
Token Text: "column"
Case Sensitivity: Case-insensitive
```

### 5.2 Token Recognition

The `column` keyword should be recognized:
1. After a method call with parentheses in a desc statement
2. Only in the context of a desc statement
3. Not as a reserved word in other contexts (to avoid breaking existing queries)

## 6. AST Node Changes

### 6.1 DescNode Extensions

```csharp
public class DescNode : Node
{
    // Existing properties
    public DescForType Type { get; set; }
    public FromNode From { get; }
    
    // New property
    public string? ColumnName { get; }
    
    // New constructor
    public DescNode(FromNode from, DescForType type, string columnName)
    {
        From = from;
        Type = type;
        ColumnName = columnName;
        Id = $"{nameof(DescNode)}{from.Id}_{columnName}";
    }
}
```

### 6.2 DescForType Enum Extension

```csharp
public enum DescForType
{
    None,
    SpecificConstructor,
    Constructors,
    Schema,
    FunctionsForSchema,
    SpecificColumn          // NEW
}
```

## 7. Parser Logic

### 7.1 ComposeDesc() Extension

```
ALGORITHM: ComposeDesc with Column Support

1. Parse "DESC" keyword
2. Parse desc target (schema, method, or method call)
3. IF target is method call (has parentheses):
   a. Check if Current.TokenType == TokenType.Column
   b. IF true:
      i.   Consume TokenType.Column
      ii.  Parse column name (identifier, string literal, or quoted identifier)
      iii. Return DescNode(fromNode, DescForType.SpecificColumn, columnName)
   c. ELSE:
      Return DescNode(fromNode, DescForType.SpecificConstructor)
4. ELSE:
   Continue with existing logic
```

### 7.2 Column Name Parsing

```
ALGORITHM: ParseColumnName

1. IF Current.TokenType == TokenType.Word OR TokenType.Identifier:
   Return token value
2. ELSE IF Current.TokenType == TokenType.LBracket:
   Consume LBracket
   Collect characters until RBracket
   Consume RBracket
   Return collected value
3. ELSE IF Current is string literal:
   Return string value without quotes
4. ELSE:
   Throw SyntaxException("Expected column name")
```

## 8. Code Generation

### 8.1 Generated Code Pattern

For `desc #schema.method() column Name`:

```csharp
private Table GetTableDesc(ISchemaProvider provider, ...)
{
    var desc = provider.GetSchema("#schema");
    var schemaTable = desc.GetTableByName("method", runtimeContext, new object[] { });
    return EvaluationHelper.GetSpecificColumnDescription(schemaTable, "Name");
}
```

## 9. Integration Points

### 9.1 Agent Discovery Flow

```
Agent Request                     SQL Query                           Returns
--------------                    ---------                           -------
"What schemas are available?"     (external discovery)                [git, files, ...]
"What methods does git have?"     desc #git                           [commits, branches, ...]
"What overloads for commits?"     desc #git.commits                   [commits(string), ...]
"What columns in commits()?"      desc #git.commits()                 [Name, Date, Author, ...]
"Details about Author column?"    desc #git.commits() column Author   [Author, Author.Name, ...]
```

### 9.2 Compatibility

- Fully backward compatible with existing desc syntax
- New keyword `column` only recognized after method call with parentheses
- No changes to existing behavior

## 10. Error Messages

### 10.1 Missing Column Name

```
Syntax Error: Expected column name after 'column' keyword
Query: desc #git.commits() column
                                  ^
```

### 10.2 Column Clause on Invalid Target

```
Syntax Error: 'column' clause is only valid with method calls (e.g., desc #schema.method() column Name)
Query: desc #git column Name
              ^
```

### 10.3 Invalid Column Name Character

```
Syntax Error: Invalid column name. Use quotes for column names with special characters: column 'Name With Spaces'
Query: desc #git.commits() column Name With Spaces
                                        ^
```

## 11. Future Considerations

### 11.1 Potential Extensions

1. **Multiple Columns**: `desc #git.commits() column Name, Date`
2. **Wildcard**: `desc #git.commits() column Author.*`
3. **Type Exploration**: `desc type System.DateTime`

### 11.2 Reserved for Future

The following syntax patterns are reserved for potential future use:
- `column` followed by `*`
- `columns` (plural form)
- `column` in other statement types
