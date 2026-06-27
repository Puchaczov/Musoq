using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderForEach(ExecutionForEach forEach)
    {
        if (TryCreateStoredGeneratedRowsIndexedLoop(forEach, out var storedGeneratedRowsLoop))
        {
            foreach (var statement in storedGeneratedRowsLoop)
                yield return statement;
            yield break;
        }

        var cacheDeclaration = TryCreateStoredRowsCacheDeclaration(forEach.Source);
        if (cacheDeclaration != null)
            yield return cacheDeclaration;

        var bodyStatements = new List<StatementSyntax>();
        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body).Statements);

        yield return StatementEmitter.CreateForeach(
            EscapeIdentifier(forEach.Item.Name),
            RenderExpression(forEach.Source),
            StatementEmitter.CreateBlock(bodyStatements));
    }

    private IEnumerable<StatementSyntax> RenderForEachWithOrdinality(ExecutionForEachWithOrdinality forEach)
    {
        var rowsVariableName = CreateIdentifierCandidate($"{forEach.Ordinal.Name}Rows", 0);
        var listVariableName = CreateIdentifierCandidate($"{forEach.Ordinal.Name}List", 0);
        var itemType = CreateVariableTypeSyntax(forEach.Item);

        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            rowsVariableName,
            RenderExpression(forEach.Source));

        yield return StatementEmitter.CreateIf(
            SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName(rowsVariableName),
                SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.ParseTypeName($"IReadOnlyList<{itemType}>"),
                    SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(listVariableName)))),
            RenderOrdinalityIndexedLoop(forEach, listVariableName, itemType),
            RenderOrdinalityForeachLoop(forEach, rowsVariableName));
    }

    private ForStatementSyntax RenderOrdinalityIndexedLoop(
        ExecutionForEachWithOrdinality forEach,
        string listVariableName,
        TypeSyntax itemType)
    {
        var bodyStatements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                itemType,
                forEach.Item.Name,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(listVariableName),
                    SyntaxFactory.IdentifierName(forEach.Ordinal.Name)))
        };
        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Insert(0, CreatePeriodicCancellationCheck(forEach.Ordinal.Name));

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body).Statements);

        return StatementEmitter.CreateForLoop(
            forEach.Ordinal.Name,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(forEach.Ordinal.Name),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(listVariableName),
                    SyntaxFactory.IdentifierName("Count"))),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(forEach.Ordinal.Name)),
            StatementEmitter.CreateBlock(bodyStatements));
    }

    private StatementSyntax RenderOrdinalityForeachLoop(
        ExecutionForEachWithOrdinality forEach,
        string rowsVariableName)
    {
        var bodyStatements = new List<StatementSyntax>();
        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body).Statements);
        bodyStatements.Add(SyntaxFactory.ExpressionStatement(
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(forEach.Ordinal.Name))));

        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(int)),
                forEach.Ordinal.Name,
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))),
            StatementEmitter.CreateForeach(
                EscapeIdentifier(forEach.Item.Name),
                SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(bodyStatements)));
    }

    private bool TryCreateStoredGeneratedRowsIndexedLoop(
        ExecutionForEach forEach,
        out IReadOnlyList<StatementSyntax> statements)
    {
        statements = [];

        if (!TryCreateGeneratedRowsLoopSource(
                forEach.Source,
                out var rowsVariable,
                out var rowsDeclaration,
                out var sourceAlreadyTyped))
        {
            return false;
        }

        var indexVariable = CreateGeneratedRowsIndexVariable(rowsVariable.Name);
        var rowAccess = CreateElementAccess(
            SyntaxFactory.IdentifierName(rowsVariable.Name),
            SyntaxFactory.IdentifierName(indexVariable.Name));
        var itemType = sourceAlreadyTyped && !string.IsNullOrWhiteSpace(rowsVariable.GeneratedRowTypeName)
            ? SyntaxFactory.ParseTypeName(rowsVariable.GeneratedRowTypeName)
            : CreateVariableTypeSyntax(forEach.Item);
        ExpressionSyntax itemInitializer = sourceAlreadyTyped
            ? rowAccess
            : SyntaxFactory.CastExpression(itemType, rowAccess);
        var itemDeclaration = CreateLocalDeclaration(
            itemType,
            forEach.Item.Name,
            itemInitializer);
        var bodyStatements = new List<StatementSyntax> { itemDeclaration };
        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Insert(0, CreatePeriodicCancellationCheck(indexVariable.Name));

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body).Statements);

        var loop = CreateIndexedForLoop(
            indexVariable.Name,
            rowsVariable,
            StatementEmitter.CreateBlock(bodyStatements));

        statements = rowsDeclaration == null ? [loop] : [rowsDeclaration, loop];
        return true;
    }

    private bool TryCreateGeneratedRowsLoopSource(
        ExecutionExpression source,
        out ExecutionVariable rowsVariable,
        out StatementSyntax? rowsDeclaration,
        out bool sourceAlreadyTyped)
    {
        rowsVariable = null!;
        rowsDeclaration = null;
        sourceAlreadyTyped = false;

        if (source is ExecutionStoredTableRows storedRows &&
            !_storedRowsCacheNames.ContainsKey(storedRows.TableIndex) &&
            TryResolveStoredGeneratedRowsShape(storedRows, out var rowShape))
        {
            var nameDisambiguator = GetStoredGeneratedRowsLoopNameDisambiguator(storedRows.TableIndex);
            rowsVariable = CreateStoredGeneratedRowsVariable(storedRows.TableIndex, nameDisambiguator, rowShape.TypeName);
            sourceAlreadyTyped = TryGetTypedStoredTableResult(storedRows.TableIndex, rowShape, out _);
            rowsDeclaration = CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rowsVariable.Name,
                CreateStoredTableRowsRead(storedRows));
            return true;
        }

        if (source is ExecutionVariableRead { Variable: { GeneratedRowTypeName: not null } variable } &&
            variable.Type == typeof(IReadOnlyList<Row>))
        {
            rowsVariable = variable;
            return true;
        }

        return false;
    }

    private bool TryResolveStoredGeneratedRowsShape(
        ExecutionStoredTableRows storedRows,
        out GeneratedRowShape rowShape)
    {
        if (storedRows.GeneratedRowShape != null)
        {
            rowShape = storedRows.GeneratedRowShape;
            return true;
        }

        if (TryGetTypedStoredTableResult(storedRows.TableIndex, out var typedResult))
        {
            rowShape = typedResult.RowShape;
            return true;
        }

        rowShape = null!;
        return false;
    }

    private int GetStoredGeneratedRowsLoopNameDisambiguator(int tableIndex)
    {
        _storedGeneratedRowsLoopNameCounts.TryGetValue(tableIndex, out var disambiguator);
        _storedGeneratedRowsLoopNameCounts[tableIndex] = disambiguator + 1;
        return disambiguator;
    }

    private static ExecutionVariable CreateStoredGeneratedRowsVariable(
        int tableIndex,
        int disambiguator,
        string generatedRowTypeName)
    {
        return new ExecutionVariable(
            CreateIdentifierCandidate($"__storedTable{tableIndex.ToString(CultureInfo.InvariantCulture)}Rows", disambiguator),
            typeof(IReadOnlyList<Row>),
            generatedRowTypeName);
    }

    private static ExecutionVariable CreateGeneratedRowsIndexVariable(string rowsVariableName)
    {
        var name = rowsVariableName.EndsWith("Rows", StringComparison.Ordinal)
            ? $"{rowsVariableName[..^4]}Index"
            : $"{rowsVariableName}Index";
        return new ExecutionVariable(CreateIdentifierCandidate(name, 0), typeof(int));
    }

    private LocalDeclarationStatementSyntax? TryCreateStoredRowsCacheDeclaration(ExecutionExpression source)
    {
        if (source is not ExecutionStoredTableRows storedRows ||
            !_storedRowsCacheNames.TryGetValue(storedRows.TableIndex, out var cacheName) ||
            !_declaredStoredRowsCaches.Add(storedRows.TableIndex))
        {
            return null;
        }

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            cacheName,
            CreateStoredTableRowsRead(storedRows));
    }

    private ForStatementSyntax RenderForEachIndexed(ExecutionForEachIndexed forEachIndexed)
    {
        var bodyStatements = new List<StatementSyntax>();
        if (_emitChunkLoopCancellationChecks)
            bodyStatements.Add(CreatePeriodicCancellationCheck(forEachIndexed.Index.Name));

        bodyStatements.AddRange(CreateIndexedItemDeclarations(
            forEachIndexed.Item,
            forEachIndexed.Source,
            forEachIndexed.Index,
            forEachIndexed.RowAccessMode));
        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabled, _operatorCatalog, forEachIndexed));
        bodyStatements.AddRange(RenderBlock(forEachIndexed.Body).Statements);

        return CreateIndexedForLoop(
            forEachIndexed.Index.Name,
            forEachIndexed.Source,
            StatementEmitter.CreateBlock(bodyStatements));
    }
}
