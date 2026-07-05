using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderForEach(
        ExecutionForEach forEach,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        if (TryCreateStoredGeneratedRowsIndexedLoop(forEach, context, out var storedGeneratedRowsLoop))
        {
            foreach (var statement in storedGeneratedRowsLoop)
                yield return statement;
            yield break;
        }

        var cacheDeclaration = TryCreateStoredRowsCacheDeclaration(forEach.Source, context);
        if (cacheDeclaration != null)
            yield return cacheDeclaration;

        var bodyStatements = new List<StatementSyntax>();
        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body, context).Statements);

        yield return StatementEmitter.CreateForeach(
            EscapeIdentifier(forEach.Item.Name),
            RenderExpression(forEach.Source, context),
            StatementEmitter.CreateBlock(bodyStatements));
    }

    private IEnumerable<StatementSyntax> RenderForEachWithOrdinality(
        ExecutionForEachWithOrdinality forEach,
        ExecutionRenderContext context)
    {
        var rowsVariableName = CreateIdentifierCandidate($"{forEach.Ordinal.Name}Rows", 0);
        var listVariableName = CreateIdentifierCandidate($"{forEach.Ordinal.Name}List", 0);
        var itemType = CreateVariableTypeSyntax(forEach.Item);

        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            rowsVariableName,
            RenderExpression(forEach.Source, context));

        yield return StatementEmitter.CreateIf(
            SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName(rowsVariableName),
                SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.ParseTypeName($"IReadOnlyList<{itemType}>"),
                    SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(listVariableName)))),
            RenderOrdinalityIndexedLoop(forEach, listVariableName, itemType, context),
            RenderOrdinalityForeachLoop(forEach, rowsVariableName, context));
    }

    private ForStatementSyntax RenderOrdinalityIndexedLoop(
        ExecutionForEachWithOrdinality forEach,
        string listVariableName,
        TypeSyntax itemType,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var bodyStatements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                itemType,
                forEach.Item.Name,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(listVariableName),
                    SyntaxFactory.IdentifierName(forEach.Ordinal.Name)))
        };
        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Insert(0, CreatePeriodicCancellationCheck(forEach.Ordinal.Name));

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body, context).Statements);

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
        string rowsVariableName,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var bodyStatements = new List<StatementSyntax>();
        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Add(QueryEmitter.GenerateCancellationCheck());

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, forEach));
        bodyStatements.AddRange(RenderBlock(forEach.Body, context).Statements);
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
        ExecutionRenderContext context,
        out IReadOnlyList<StatementSyntax> statements)
    {
        var session = context.Session;
        statements = [];

        if (!TryCreateGeneratedRowsLoopSource(
                forEach.Source,
                context,
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
        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Insert(0, CreatePeriodicCancellationCheck(indexVariable.Name));

        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, forEach));
        using (EnterGeneratedRowVariableType(context, forEach.Item.Name, rowsVariable.GeneratedRowTypeName!))
            bodyStatements.AddRange(RenderBlock(forEach.Body, context).Statements);

        var loop = CreateIndexedForLoop(
            indexVariable.Name,
            rowsVariable,
            StatementEmitter.CreateBlock(bodyStatements));

        statements = rowsDeclaration == null ? [loop] : [rowsDeclaration, loop];
        return true;
    }

    private bool TryCreateGeneratedRowsLoopSource(
        ExecutionExpression source,
        ExecutionRenderContext context,
        out ExecutionVariable rowsVariable,
        out StatementSyntax? rowsDeclaration,
        out bool sourceAlreadyTyped)
    {
        rowsVariable = null!;
        rowsDeclaration = null;
        sourceAlreadyTyped = false;

        if (source is ExecutionStoredTableRows storedRows &&
            !context.Session.StoredRowsCacheNames.ContainsKey(storedRows.TableIndex) &&
            TryResolveStoredGeneratedRowsShape(storedRows, context, out var rowShape))
        {
            var nameDisambiguator = GetStoredGeneratedRowsLoopNameDisambiguator(storedRows.TableIndex, context);
            rowsVariable = CreateStoredGeneratedRowsVariable(storedRows.TableIndex, nameDisambiguator, rowShape.TypeName);
            sourceAlreadyTyped = TryGetTypedStoredTableResult(storedRows.TableIndex, rowShape, context, out _);
            rowsDeclaration = CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rowsVariable.Name,
                CreateStoredTableRowsRead(storedRows, context));
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
        ExecutionRenderContext context,
        out GeneratedRowShape rowShape)
    {
        if (storedRows.GeneratedRowShape != null)
        {
            rowShape = storedRows.GeneratedRowShape;
            return true;
        }

        if (TryGetTypedStoredTableResult(storedRows.TableIndex, context, out var typedResult))
        {
            rowShape = typedResult.RowShape;
            return true;
        }

        rowShape = null!;
        return false;
    }

    private int GetStoredGeneratedRowsLoopNameDisambiguator(
        int tableIndex,
        ExecutionRenderContext context)
    {
        context.Session.StoredGeneratedRowsLoopNameCounts.TryGetValue(tableIndex, out var disambiguator);
        context.Session.StoredGeneratedRowsLoopNameCounts[tableIndex] = disambiguator + 1;
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

    private LocalDeclarationStatementSyntax? TryCreateStoredRowsCacheDeclaration(
        ExecutionExpression source,
        ExecutionRenderContext context)
    {
        if (source is not ExecutionStoredTableRows storedRows ||
            !context.Session.StoredRowsCacheNames.TryGetValue(storedRows.TableIndex, out var cacheName) ||
            !context.Session.DeclaredStoredRowsCaches.Add(storedRows.TableIndex))
        {
            return null;
        }

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            cacheName,
            CreateStoredTableRowsRead(storedRows, context));
    }

    private ForStatementSyntax RenderForEachIndexed(
        ExecutionForEachIndexed forEachIndexed,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var bodyStatements = new List<StatementSyntax>();
        if (session.EmitChunkLoopCancellationChecks)
            bodyStatements.Add(CreatePeriodicCancellationCheck(forEachIndexed.Index.Name));

        bodyStatements.AddRange(CreateIndexedItemDeclarations(
            forEachIndexed.Item,
            forEachIndexed.Source,
            forEachIndexed.Index,
            forEachIndexed.RowAccessMode));
        bodyStatements.AddRange(LoopOperatorProfilingStatementFactory.Create(IsOperatorProfilingEnabledFor(context), session.OperatorCatalog, forEachIndexed));
        bodyStatements.AddRange(RenderBlock(forEachIndexed.Body, context).Statements);

        return CreateIndexedForLoop(
            forEachIndexed.Index.Name,
            forEachIndexed.Source,
            StatementEmitter.CreateBlock(bodyStatements));
    }
}
