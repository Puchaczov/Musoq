-- Inventory test-query source with Musoq itself. No precomputed CSV or extractor is an input.
-- Execution/assertion columns describe source evidence, not proven argument-to-result data flow.
-- includeSourceOnly adds shared catalogs and unsupported-helper/without-literal review rows.
param(solution: string, includeSourceOnly: bool = false, projectPattern: string = '%.Tests')
let expressionPattern: string = r'(?s)//[^\r\n]*|/\*.*?\*/|\x27(?:\\.|[^\x27\\])\x27|(?<expression>\bTestMethodTemplate(?:<[^>]+>)?\s*\(\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")(?:\s*\+\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"))*|\bRunBinary\s*\(\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")(?:\s*\+\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"))*\s*,\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")(?:\s*\+\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"))*|\b(?:CreateAndRunVirtualMachine(?:WithValidation)?|CompileForExecution|CompileWithDiagnostics|CompileQuery|Compile|RunQuery|RunJoinQuery|Run)\s*(?:<[^>]+>)?\s*\(\s*[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?=\s*[,\)])|(?:(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")|[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?=\s*\+))(?:\s*\+\s*(?:(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")|[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*))*)'
let tokenPattern: string = r'(?s)(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")|[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*'
let binaryPattern: string = r'(?s)\bRunBinary\s*\(\s*((?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")(?:\s*\+\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"))*)\s*,\s*((?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*")(?:\s*\+\s*(?:\$*(?<quote>"{3,}).*?\k<quote>|\$?@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\])*"))*)'
let sqlPrefix: string = r'(?is)^\s*(?:(?:--[^\r\n]*(?:\r?\n|$)|/\*.*?\*/)\s*)*(?:select|with|from|desc|table|couple|binary|text|param|params|let|pivot|unpivot)\b'

WITH methods AS (
    SELECT p.Name AS Project, d.FilePath AS File, c.Name AS Class, c.FullName AS DeclaringType,
           m.Name AS TestMethod, m.StartLine AS MethodLine, m.Text AS MethodText,
           m.Text RLIKE r'\[(?:TestMethod|DataTestMethod)\b' AS IsTest
    FROM csharp.solution($solution) s CROSS APPLY s.Projects p CROSS APPLY p.Documents d
    CROSS APPLY d.Classes c CROSS APPLY c.Methods m
    WHERE p.Name LIKE $projectPattern
), parameterRows AS (
    SELECT p.Name AS Project, d.FilePath AS File, m.StartLine AS MethodLine, arg.Ordinal AS Position,
           arg.Name AS ParameterName, p.RegexReplace(arg.FullTypeName, r'\s+', '') AS ParameterType
    FROM csharp.solution($solution) s CROSS APPLY s.Projects p CROSS APPLY p.Documents d
    CROSS APPLY d.Classes c CROSS APPLY c.Methods m CROSS APPLY m.Parameters arg
    WHERE p.Name LIKE $projectPattern
    ORDER BY p.Name, d.FilePath, m.StartLine, arg.Ordinal
), signatures AS (
    SELECT p.Project, p.File, p.MethodLine, Count(*) AS ParameterCount,
           AggregateValues(p.ParameterName, ',') AS ParameterNames,
           AggregateValues(p.ParameterType, ',') AS ParameterTypes
    FROM parameterRows p GROUP BY p.Project, p.File, p.MethodLine
), helperRegistry AS (
    SELECT r.DeclaringType, r.Method, r.ParameterTypes
    FROM values {
        (DeclaringType: 'Musoq.Evaluator.Tests.JoinSemiAntiCrossJoinTests',
         Method: 'RunJoinQuery', ParameterTypes: 'string,Musoq.Evaluator.CompilationOptions?'),
        (DeclaringType: 'Musoq.Evaluator.Tests.Core017SemiAntiPresenceTests',
         Method: 'Run', ParameterTypes: 'string,System.Collections.Generic.IDictionary<string,System.Collections.Generic.IEnumerable<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>')
    } r
), registeredMatches AS (
    SELECT m.Project, m.DeclaringType, m.TestMethod AS Method, p.ParameterTypes
    FROM methods m
    INNER JOIN signatures p ON m.Project = p.Project AND m.File = p.File AND m.MethodLine = p.MethodLine
    INNER JOIN helperRegistry r ON m.DeclaringType = r.DeclaringType AND m.TestMethod = r.Method
                                AND p.ParameterTypes = r.ParameterTypes
    WHERE m.MethodText RLIKE r'\.Run\s*\('
), registeredDeclarations AS (
    SELECT h.Project, h.DeclaringType, h.Method, h.ParameterTypes, Count(*) AS Declarations
    FROM registeredMatches h
    GROUP BY h.Project, h.DeclaringType, h.Method, h.ParameterTypes
), evidence AS (
    SELECT m.Project, m.File, m.Class, m.DeclaringType, m.TestMethod, m.MethodLine, m.MethodText,
           m.IsTest,
           p.ParameterCount, p.ParameterNames, p.ParameterTypes,
           CASE
               WHEN r.Declarations = 1 AND m.MethodText RLIKE m.Concat(r'\b', r.Method, r'\s*\(')
                   THEN m.Concat('registered-wrapper:', r.DeclaringType, '.', r.Method, '(', r.ParameterTypes, ')')
               WHEN m.MethodText RLIKE r'\.(?:Run|RunAsync|RunWithProfile|Execute|ExecuteAsync)\s*\('
                   THEN 'direct-run-syntax'
               WHEN m.MethodText RLIKE r'\b(?:RunQuery|RunBinaryQuery|RunBinary|RunNative\w*Query|ExecuteSingleRow|ExecuteQuery|Execute|ExecuteAsync)\s*(?:<[^>]+>)?\s*\('
                   THEN 'helper-name-pattern'
               WHEN m.MethodText RLIKE r'\bTestMethodTemplate(?:<[^>]+>)?\s*\('
                   THEN 'basic-projection-template-convention'
               ELSE ''
           END AS ExecutionEvidence,
           m.RegexExtract(m.MethodText,
               r'\b(?:Assert\w*\s*\(|(?:Assert|CollectionAssert|StringAssert)\.(?:AreEqual|AreNotEqual|IsTrue|IsFalse|HasCount|IsEmpty|IsNotEmpty|IsNull|IsNotNull|Contains|DoesNotContain|Throws\w*)\s*\()', 0)
               AS AssertionEvidence
    FROM methods m
    LEFT JOIN signatures p ON m.Project = p.Project AND m.File = p.File AND m.MethodLine = p.MethodLine
    LEFT JOIN registeredDeclarations r ON m.Project = r.Project AND m.DeclaringType = r.DeclaringType
                                      AND r.Declarations = 1
), selected AS (
    SELECT e.*,
           CASE WHEN e.IsTest AND e.ExecutionEvidence <> '' AND e.AssertionEvidence IS NOT NULL
               THEN 'execution-and-assertion-source' ELSE 'source-only' END AS EvidenceKind
    FROM evidence e
    WHERE (e.IsTest AND e.ExecutionEvidence <> '' AND e.AssertionEvidence IS NOT NULL) OR $includeSourceOnly
), fieldConstants AS (
    SELECT p.Name AS Project, d.FilePath AS File, c.Name AS Class, c.FullName AS DeclaringType, c.StartLine AS ClassLine, f.Name AS Name, f.InitializerText AS Value
    FROM csharp.solution($solution) s CROSS APPLY s.Projects p CROSS APPLY p.Documents d
    CROSS APPLY d.Classes c CROSS APPLY c.Fields f
    WHERE p.Name LIKE $projectPattern AND f.IsConst AND f.Type = 'String' AND f.HasInitializer
), localDefinitions AS (
    SELECT p.Name AS Project, d.FilePath AS File, m.StartLine AS MethodLine,
           v.Name AS Name, v.InitializerText AS Value
    FROM csharp.solution($solution) s CROSS APPLY s.Projects p CROSS APPLY p.Documents d
    CROSS APPLY d.Classes c CROSS APPLY c.Methods m CROSS APPLY m.LocalVariables v
    WHERE p.Name LIKE $projectPattern AND v.IsConst AND v.Type = 'String' AND v.HasInitializer
), localConstants AS (
    SELECT l.Project, l.File, l.MethodLine, l.Name, Min(l.Value) AS Value
    FROM localDefinitions l GROUP BY l.Project, l.File, l.MethodLine, l.Name
    HAVING Count(*) = 1
), scanned AS (
    SELECT e.Project, e.File, e.Class, e.DeclaringType, e.TestMethod, e.MethodLine, e.MethodText,
           e.ExecutionEvidence, e.AssertionEvidence, e.EvidenceKind,
           e.ParameterCount, e.ParameterNames,
           x.Ordinal AS ExpressionOrdinal, x.Value AS CSharpExpression,
           (e.MethodText RLIKE e.Concat(e.EscapeRegex(x.Value), r'\s*\+')
            OR e.MethodText RLIKE e.Concat(r'\+\s*', e.EscapeRegex(x.Value))
            OR e.MethodText RLIKE r'\b(?:query|sql|script)\.(?:Replace|Insert|Remove|Substring)\s*\(') AS PossiblePartial,
           CASE WHEN x.Value RLIKE r'^TestMethodTemplate\b' THEN 'basic-projection-template'
                WHEN x.Value RLIKE r'^RunBinary\b' AND e.Class = 'BinarySpecificationRegressionTests'
                    THEN 'binary-record-template'
                WHEN x.Value RLIKE r'^[A-Za-z_]\w*(?:<[^>]+>)?\s*\(' THEN 'constant-argument-reference'
                ELSE 'sql-expression' END AS Extraction
    FROM selected e CROSS APPLY e.RegexExtractAll(e.MethodText, $expressionPattern, 1) x WITH ORDINALITY
    WHERE x.Value <> ''
    UNION ALL (Project, File, Class, TestMethod, MethodLine, ExpressionOrdinal)
    SELECT f.Project, f.File, f.Class, f.DeclaringType,
           f.Concat('<field>.', f.Name) AS TestMethod, f.ClassLine AS MethodLine, '' AS MethodText,
           '' AS ExecutionEvidence, '' AS AssertionEvidence, 'source-only' AS EvidenceKind,
           0::long AS ParameterCount, '' AS ParameterNames, 0 AS ExpressionOrdinal,
           f.Value AS CSharpExpression, false AS PossiblePartial, 'shared-field-source' AS Extraction
    FROM fieldConstants f WHERE $includeSourceOnly
), expressions AS (
    SELECT e.*,
           CASE WHEN e.Extraction = 'binary-record-template'
               THEN e.Concat(e.RegexExtract(e.CSharpExpression, $binaryPattern, 1),
                    ' + " select " + ', e.RegexExtract(e.CSharpExpression, $binaryPattern, 2),
                    ' + " from #test.files() f cross apply Interpret<Rec>(f.Content) r"')
               WHEN e.Extraction = 'constant-argument-reference'
                   THEN e.RegexReplace(e.CSharpExpression, r'^[A-Za-z_]\w*(?:<[^>]+>)?\s*\(\s*', '')
               WHEN e.Extraction = 'basic-projection-template'
                   THEN e.RegexReplace(e.CSharpExpression, r'^TestMethodTemplate(?:<[^>]+>)?\s*\(\s*', '')
               ELSE e.CSharpExpression END AS DecodeExpression,
           0 AS DataRowOrdinal, '' AS BoundParameter, '' AS BoundValue
    FROM scanned e
    UNION ALL (Project, File, Class, DeclaringType, TestMethod, MethodLine, ExpressionOrdinal, DataRowOrdinal)
    SELECT e.Project, e.File, e.Class, e.DeclaringType, e.TestMethod, e.MethodLine, e.MethodText,
           e.ExecutionEvidence, e.AssertionEvidence, e.EvidenceKind, e.ParameterCount, e.ParameterNames,
           e.ExpressionOrdinal, e.CSharpExpression, e.PossiblePartial, e.Extraction, e.CSharpExpression AS DecodeExpression,
           a.Ordinal + 1 AS DataRowOrdinal, e.ParameterNames AS BoundParameter,
           e.Substring(a.Value, 1, e.Length(a.Value) - 2) AS BoundValue
    FROM scanned e
    CROSS APPLY e.RegexExtractAll(e.MethodText, r'\[DataRow\s*\(\s*("[^"\\]*")\s*\)\]', 1) a WITH ORDINALITY
    WHERE e.ParameterCount = 1 AND e.CSharpExpression RLIKE r'^\s*(?:\$|@\$)'
), tokens AS (
    SELECT e.*, t.Ordinal AS TokenOrdinal, t.Value AS Token,
           t.Value RLIKE r'^(?:\$|@|"|@\$)' AS IsLiteral
    FROM expressions e CROSS APPLY e.RegexMatches($tokenPattern, e.DecodeExpression) t WITH ORDINALITY
), constantTokens AS (
    SELECT t.*,
           t.Coalesce(l.Value, f.Value) AS ConstantValue,
           l.Name IS NOT NULL OR f.Name IS NOT NULL AS ConstantKnown
    FROM tokens t
    LEFT JOIN localConstants l ON t.Project = l.Project AND t.File = l.File AND t.MethodLine = l.MethodLine AND t.Token = l.Name
    LEFT JOIN fieldConstants f ON t.Project = f.Project AND t.DeclaringType = f.DeclaringType AND t.Token = f.Name
), expansionSources AS (
    SELECT t.*, t.Coalesce(t.ConstantValue, t.Token) AS ExpansionText
    FROM constantTokens t
), expandedTokens AS (
    SELECT t.*, x.Ordinal AS ExpansionOrdinal, x.Value AS BodyToken,
           x.Value RLIKE r'^(?:\$|@|"|@\$)' AS BodyIsLiteral
    FROM expansionSources t
    OUTER APPLY t.RegexMatches($tokenPattern, t.ExpansionText) x WITH ORDINALITY
), bodies AS (
    SELECT t.*,
           CASE WHEN t.BodyIsLiteral = false THEN 'constant'
                WHEN t.BodyToken RLIKE r'^\$*"{3,}' THEN 'raw'
                WHEN t.BodyToken RLIKE r'^(?:\$?@|@\$)"' THEN 'verbatim' ELSE 'regular' END AS LiteralKind,
           t.BodyToken RLIKE r'^(?:\$|@\$)' AS IsInterpolated,
           CASE WHEN t.BodyIsLiteral = false THEN ''
                WHEN t.BodyToken RLIKE r'^\$*"{3,}' THEN t.RegexExtract(t.BodyToken, r'(?s)^\$*(?<q>"{3,})(.*?)\k<q>$', 1)
                WHEN t.BodyToken RLIKE r'^(?:\$?@|@\$)"' THEN t.Replace(t.RegexExtract(t.BodyToken, r'(?s)^(?:\$?@|@\$)"(.*)"$', 1), '""', '"')
                ELSE t.RegexExtract(t.BodyToken, r'(?s)^\$?"(.*)"$', 1) END AS LiteralBody
    FROM expandedTokens t
), rawLayout AS (
    SELECT b.*,
           CASE WHEN b.LiteralKind = 'raw' AND b.LiteralBody RLIKE r'^\r?\n'
               THEN b.RegexReplace(b.RegexReplace(b.LiteralBody, r'^\r?\n|\r?\n[ \t]*$', ''),
                    b.Concat('(?m)^', b.RegexExtract(b.LiteralBody, r'\r?\n([ \t]*)$', 1)), '')
               ELSE b.LiteralBody END AS Body
    FROM bodies b
), fragmentInputs AS (
    SELECT DISTINCT b.Project, b.File, b.Class, b.DeclaringType, b.TestMethod, b.MethodLine, b.ExpressionOrdinal, b.Extraction, b.CSharpExpression, b.ExecutionEvidence, b.AssertionEvidence, b.EvidenceKind, b.DataRowOrdinal, b.BoundParameter, b.BoundValue, b.TokenOrdinal, b.ExpansionOrdinal, b.BodyToken, b.BodyIsLiteral, b.LiteralKind, b.IsInterpolated, b.Body, b.PossiblePartial,
        CASE
            WHEN b.IsInterpolated AND b.LiteralKind = 'regular'
                THEN r'\\(?:u[0-9a-fA-F]{4}|x[0-9a-fA-F]{1,4}|U[0-9a-fA-F]{8}|.)|\{\{|\}\}|\{[^{}]*\}|[^\\{}]+|[{}]'
            WHEN b.IsInterpolated THEN r'\{\{|\}\}|\{[^{}]*\}|[^{}]+|[{}]'
            WHEN b.LiteralKind = 'regular'
                THEN r'\\(?:u[0-9a-fA-F]{4}|x[0-9a-fA-F]{1,4}|U[0-9a-fA-F]{8}|.)|[^\\]+'
            ELSE r'(?s).+'
        END AS FragmentPattern
    FROM rawLayout b
), fragments AS (
    SELECT b.*, piece.Ordinal AS FragmentOrdinal, piece.Value AS Fragment,
           CASE WHEN b.IsInterpolated AND piece.Value RLIKE r'^\{[A-Za-z_]\w*\}$'
               THEN b.Substring(piece.Value, 1, b.Length(piece.Value) - 2) ELSE '' END AS Hole
    FROM fragmentInputs b
    OUTER APPLY b.RegexMatches(b.FragmentPattern, b.Body) piece WITH ORDINALITY
), decodedFragments AS (
    SELECT b.*,
           CASE
               WHEN b.BodyIsLiteral = false THEN 1
               WHEN b.BodyToken RLIKE r'^\$\$' THEN 1
               WHEN b.IsInterpolated AND b.Fragment RLIKE r'^\{' AND b.Fragment <> '{{'
                    AND (b.Hole = '' OR b.Hole <> b.BoundParameter) THEN 1
               ELSE 0 END AS Unresolved,
           CASE
               WHEN b.BodyIsLiteral = false THEN b.Concat('/* unresolved:', b.BodyToken, ' */')
               WHEN b.Hole <> '' AND b.Hole = b.BoundParameter THEN b.Coalesce(b.BoundValue, '')
               WHEN b.IsInterpolated AND b.Fragment = '{{' THEN '{'
               WHEN b.IsInterpolated AND b.Fragment = '}}' THEN '}'
               WHEN b.LiteralKind <> 'regular' THEN b.Coalesce(b.Fragment, '')
               WHEN b.Fragment = r'\n' THEN '\n' WHEN b.Fragment = r'\r' THEN '\r'
               WHEN b.Fragment = r'\t' THEN '\t' WHEN b.Fragment = r'\"' THEN '"'
               WHEN b.Fragment = b.Concat(r'\', b.ToString(b.ToChar(39))) THEN b.ToString(b.ToChar(39))
               WHEN b.Fragment = r'\\' THEN r'\' WHEN b.Fragment = r'\0' THEN b.ToString(b.ToChar(0))
               WHEN b.Fragment = r'\a' THEN b.ToString(b.ToChar(7)) WHEN b.Fragment = r'\b' THEN b.ToString(b.ToChar(8))
               WHEN b.Fragment = r'\f' THEN b.ToString(b.ToChar(12)) WHEN b.Fragment = r'\v' THEN b.ToString(b.ToChar(11))
               WHEN b.Fragment RLIKE r'^\\u[0-9a-fA-F]{4}$' THEN b.FromUnicodeEscape(b.Fragment)
               WHEN b.Fragment RLIKE r'^\\x[0-9a-fA-F]{1,4}$'
                   THEN b.ToString(b.ToChar(b.FromHex(b.Substring(b.Fragment, 2, b.Length(b.Fragment) - 2))))
               WHEN b.Fragment RLIKE r'^\\U[0-9a-fA-F]{8}$' AND b.FromHex(b.Substring(b.Fragment, 2, b.Length(b.Fragment) - 2)) <= 65535
                   THEN b.ToString(b.ToChar(b.FromHex(b.Substring(b.Fragment, 2, b.Length(b.Fragment) - 2))))
               WHEN b.Fragment RLIKE r'^\\U[0-9a-fA-F]{8}$'
                   THEN b.Concat(b.ToString(b.ToChar(55296 + (b.FromHex(b.Substring(b.Fragment, 2, b.Length(b.Fragment) - 2)) - 65536) / 1024)),
                                 b.ToString(b.ToChar(56320 + (b.FromHex(b.Substring(b.Fragment, 2, b.Length(b.Fragment) - 2)) - 65536) % 1024)))
               ELSE b.Coalesce(b.Fragment, '') END AS Text
    FROM fragments b
), orderedFragments AS (
    SELECT f.* FROM decodedFragments f
    ORDER BY f.Project, f.File, f.MethodLine, f.ExpressionOrdinal, f.DataRowOrdinal, f.TokenOrdinal, f.ExpansionOrdinal, f.FragmentOrdinal
), decoded AS (
    SELECT f.Project, f.File, f.Class, f.DeclaringType, f.TestMethod, f.MethodLine, f.ExpressionOrdinal,
           f.Extraction, f.CSharpExpression, f.PossiblePartial, f.ExecutionEvidence, f.AssertionEvidence, f.EvidenceKind, f.DataRowOrdinal,
           Sum(f.Unresolved) AS UnresolvedParts,
           AggregateValues(f.Text, '') AS Text
    FROM orderedFragments f
    GROUP BY f.Project, f.File, f.Class, f.DeclaringType, f.TestMethod, f.MethodLine, f.ExpressionOrdinal,
             f.Extraction, f.CSharpExpression, f.PossiblePartial, f.ExecutionEvidence, f.AssertionEvidence, f.EvidenceKind, f.DataRowOrdinal
), queryRows AS (
    SELECT d.Project, d.File, d.Class, d.TestMethod, d.MethodLine, d.ExpressionOrdinal,
           d.Extraction,
           CASE WHEN d.UnresolvedParts > 0 OR d.PossiblePartial = true THEN 'unresolved-source-expression'
                WHEN d.DataRowOrdinal > 0 THEN 'datarow-substituted'
                WHEN d.Extraction = 'constant-argument-reference' THEN 'constant-reference-resolved'
                WHEN d.Extraction IN ('basic-projection-template', 'binary-record-template') THEN 'reconstructed-helper-query'
                ELSE 'decoded-source-expression' END AS QueryStatus,
           CASE WHEN d.Extraction = 'basic-projection-template'
               THEN d.Concat('select ', d.Text, ' from #A.Entities()') ELSE d.Text END AS Query,
           d.CSharpExpression, d.ExecutionEvidence, d.AssertionEvidence,
           d.EvidenceKind, d.DataRowOrdinal,
           CASE WHEN d.UnresolvedParts > 0 OR d.PossiblePartial = true THEN 'Unsupported identifier/interpolation/adjacent expression or query transform remains in source; not an executable resolved query.'
                WHEN d.Extraction = 'shared-field-source' THEN 'Shared constant source only; MethodLine points to the declaring class, not an executed test.'
                WHEN d.EvidenceKind = 'source-only' THEN 'Shared catalog or unsupported helper; no recognized execution/assertion relationship.' ELSE '' END AS ReviewReason
    FROM decoded d
    WHERE d.Extraction IN ('basic-projection-template', 'binary-record-template') OR d.Text RLIKE $sqlPrefix
       OR ((d.UnresolvedParts > 0 OR d.PossiblePartial = true) AND d.CSharpExpression RLIKE r'(?i)\b(?:select|pivot|unpivot|with recursive)\b')
)
SELECT q.Project, q.File, q.Class, q.TestMethod, q.MethodLine, q.ExpressionOrdinal,
       q.Extraction, q.QueryStatus, q.Query, q.CSharpExpression, q.ExecutionEvidence, q.AssertionEvidence,
       q.EvidenceKind, q.DataRowOrdinal, q.ReviewReason
FROM queryRows q
ORDER BY q.Project, q.File, q.MethodLine, q.ExpressionOrdinal, q.DataRowOrdinal
