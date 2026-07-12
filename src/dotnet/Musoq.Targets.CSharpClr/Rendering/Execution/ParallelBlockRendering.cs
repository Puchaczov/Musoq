using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record ParallelRunnerRuntimeMember(
        string ParameterName,
        string FieldName,
        TypeSyntax Type);

    private IEnumerable<StatementSyntax> RenderParallelBlock(ExecutionParallelBlock parallel, ExecutionRenderContext context)
    {
        var captures = CollectParallelBlockCaptures(parallel, context);

        foreach (var task in parallel.Tasks)
            yield return CreateLocalDeclaration(
                CreateVariableTypeSyntax(task.Output),
                task.Output.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        var runnerName = CreateParallelRunnerVariableName(parallel);
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            runnerName,
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(CreateParallelRunnerTypeName(parallel)))
                .WithArgumentList(CreateArgumentList(CreateParallelRunnerConstructorArguments(parallel, captures, context))));

        var taskActions = parallel.Tasks
            .Select((_, index) => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(runnerName),
                SyntaxFactory.IdentifierName(CreateParallelTaskRunMethodName(parallel, index))))
            .ToArray();

        yield return CreateParallelInvokeStatement(parallel, taskActions);
        yield return QueryEmitter.GenerateCancellationCheck();

        for (var index = 0; index < parallel.Tasks.Count; index++)
        {
            yield return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(parallel.Tasks[index].Output.Name),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(runnerName),
                    SyntaxFactory.IdentifierName(CreateParallelTaskResultPropertyName(index)))));
        }

        foreach (var statement in RenderBlock(parallel.Merge.Body, context).Statements)
            yield return statement;
    }

    private ExpressionStatementSyntax CreateParallelInvokeStatement(
        ExecutionParallelBlock parallel,
        IReadOnlyList<ExpressionSyntax> taskActions)
    {
        var arguments = new List<ExpressionSyntax> { CreateParallelOptionsCreation(parallel.MaxDegreeOfParallelism) };
        arguments.AddRange(taskActions);

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(Parallel)),
                        SyntaxFactory.IdentifierName(nameof(Parallel.Invoke))))
                .WithArgumentList(CreateArgumentList(arguments)));
    }

    private IEnumerable<MemberDeclarationSyntax> CreateParallelBlockMembers(
        ExecutionParallelBlock parallel,
        ExecutionRenderContext context)
    {
        var captures = CollectParallelBlockCaptures(parallel, context);

        for (var index = 0; index < parallel.Tasks.Count; index++)
            yield return CreateParallelTaskBuildFunction(parallel, index, captures, context);

        yield return CreateParallelRunnerClass(parallel, captures, context);
    }

    private MethodDeclarationSyntax CreateParallelTaskBuildFunction(
        ExecutionParallelBlock parallel,
        int taskIndex,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var task = parallel.Tasks[taskIndex];
        var statements = CreateParallelTaskBuildFunctionStatements(task, context);

        return SyntaxFactory.MethodDeclaration(
                CreateVariableTypeSyntax(task.Output),
                CreateParallelTaskBuildFunctionName(parallel, taskIndex))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateParallelTaskParameterList(parallel, captures, context))
            .WithBody(StatementEmitter.CreateBlock(statements));
    }

    private List<StatementSyntax> CreateParallelTaskBuildFunctionStatements(
        ExecutionParallelTask task,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var previousTypedRowBufferVariables = session.TypedRowBufferVariables;
        var typedRowBufferVariables = new Dictionary<string, GeneratedRowShape>(
            previousTypedRowBufferVariables,
            StringComparer.Ordinal);

        if (TryGetParallelTaskTypedRowBuffer(task, context, out var table, out var rowShape))
        {
            typedRowBufferVariables[table.Name] = rowShape;
        }
        else if (TypedStoredTableResultResolver.TryGetParallelTaskResultTable(task, out table))
        {
            typedRowBufferVariables.Remove(table.Name);
        }

        session.TypedRowBufferVariables = typedRowBufferVariables;

        try
        {
            var statements = new List<StatementSyntax>
            {
                CreateLocalDeclaration(
                    CreateVariableTypeSyntax(task.Output),
                    task.Output.Name,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
            };
            statements.AddRange(RenderParallelTaskBody(task, context).Statements);
            statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(task.Output.Name)));

            return task.RelatedQueryIdentifier is { } relatedQueryIdentifier
                ? CreateRelatedParallelTaskStatements(relatedQueryIdentifier, statements)
                : statements;
        }
        finally
        {
            session.TypedRowBufferVariables = previousTypedRowBufferVariables;
        }
    }

    private bool TryGetParallelTaskTypedRowBuffer(
        ExecutionParallelTask task,
        ExecutionRenderContext context,
        out ExecutionVariable table,
        out GeneratedRowShape rowShape)
    {
        table = null!;
        rowShape = null!;

        return task.RelatedTableIndex is { } tableIndex &&
               context.Session.TypedStoredTableResults.TryGetValue(tableIndex, out var typedResult) &&
               TypedStoredTableResultResolver.TryGetParallelTaskResultTable(task, out table) &&
               (rowShape = typedResult.RowShape) != null;
    }

    private List<StatementSyntax> CreateRelatedParallelTaskStatements(
        string queryIdentifier,
        IReadOnlyList<StatementSyntax> taskStatements)
    {
        return
        [
            QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.Begin),
            SyntaxFactory.TryStatement()
                .WithBlock(StatementEmitter.CreateBlock(taskStatements))
                .WithFinally(SyntaxFactory.FinallyClause(StatementEmitter.CreateBlock(
                    QueryEmitter.GeneratePhaseChangeStatement(queryIdentifier, QueryPhase.End))))
        ];
    }

    private ClassDeclarationSyntax CreateParallelRunnerClass(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(CreateParallelRunnerFields(parallel, captures, context));
        members.AddRange(parallel.Tasks.Select(CreateParallelTaskResultProperty));
        members.Add(CreateParallelRunnerConstructor(parallel, captures, context));
        members.AddRange(parallel.Tasks.Select((_, index) => CreateParallelTaskRunMethod(parallel, index, captures, context)));

        return SyntaxFactory.ClassDeclaration(CreateParallelRunnerTypeName(parallel))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List(members));
    }

    private IEnumerable<FieldDeclarationSyntax> CreateParallelRunnerFields(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        foreach (var member in CreateParallelRunnerRuntimeMembers(parallel, context))
            yield return CreateParallelRunnerField(member.FieldName, member.Type);

        foreach (var capture in captures)
            yield return CreateParallelRunnerField(
                CreateParallelRunnerCapturedFieldName(capture),
                CreateCapturedLocalTypeSyntax(capture));
    }

    private static FieldDeclarationSyntax CreateParallelRunnerField(string fieldName, TypeSyntax type)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(type)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(fieldName))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private static PropertyDeclarationSyntax CreateParallelTaskResultProperty(
        ExecutionParallelTask task,
        int taskIndex)
    {
        return SyntaxFactory.PropertyDeclaration(
                CreateVariableTypeSyntax(task.Output),
                CreateParallelTaskResultPropertyName(taskIndex))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List([
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            ])));
    }

    private ConstructorDeclarationSyntax CreateParallelRunnerConstructor(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var assignments = CreateParallelRunnerRuntimeMembers(parallel, context)
            .Select<ParallelRunnerRuntimeMember, StatementSyntax>(member =>
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    CreateParallelRunnerAssignmentTarget(member),
                    SyntaxFactory.IdentifierName(member.ParameterName))))
            .Concat(captures.Select(static capture => SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(CreateParallelRunnerCapturedFieldName(capture)),
                SyntaxFactory.IdentifierName(capture.Name)))))
            .ToArray();

        return SyntaxFactory.ConstructorDeclaration(CreateParallelRunnerTypeName(parallel))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(CreateParallelRunnerConstructorParameters(parallel, captures, context))))
            .WithBody(StatementEmitter.CreateBlock(assignments));
    }

    private static ExpressionSyntax CreateParallelRunnerAssignmentTarget(ParallelRunnerRuntimeMember member)
    {
        return string.Equals(member.ParameterName, member.FieldName, StringComparison.Ordinal)
            ? CreateThisMemberAccess(member.FieldName)
            : SyntaxFactory.IdentifierName(member.FieldName);
    }

    private MethodDeclarationSyntax CreateParallelTaskRunMethod(
        ExecutionParallelBlock parallel,
        int taskIndex,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var assignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(CreateParallelTaskResultPropertyName(taskIndex)),
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(
                    CreateParallelTaskBuildFunctionName(parallel, taskIndex)))
                .WithArgumentList(CreateArgumentList(CreateParallelRunnerStaticTaskArguments(parallel, captures, context)))));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                CreateParallelTaskRunMethodName(parallel, taskIndex))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(StatementEmitter.CreateBlock(assignment));
    }

    private static string CreateParallelTaskRunMethodName(ExecutionParallelBlock parallel, int taskIndex)
    {
        return CreateIdentifierCandidate(
            $"Run{CreatePascalIdentifier(parallel.Name)}Task{taskIndex.ToString(CultureInfo.InvariantCulture)}",
            0);
    }

    private static string CreateParallelTaskBuildFunctionName(ExecutionParallelBlock parallel, int taskIndex)
    {
        return CreateIdentifierCandidate(
            $"Build{CreatePascalIdentifier(parallel.Name)}Task{taskIndex.ToString(CultureInfo.InvariantCulture)}",
            0);
    }

    private static string CreateParallelRunnerTypeName(ExecutionParallelBlock parallel)
    {
        return CreateIdentifierCandidate($"{CreatePascalIdentifier(parallel.Name)}Runner", 0);
    }

    private static string CreateParallelRunnerVariableName(ExecutionParallelBlock parallel)
    {
        var typeName = CreateParallelRunnerTypeName(parallel);
        return char.ToLowerInvariant(typeName[0]) + typeName[1..];
    }

    private static string CreateParallelTaskResultPropertyName(int taskIndex)
    {
        return $"Task{taskIndex.ToString(CultureInfo.InvariantCulture)}Result";
    }

    private static MemberAccessExpressionSyntax CreateThisMemberAccess(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ThisExpression(),
            SyntaxFactory.IdentifierName(memberName));
    }

    private CapturedLocal[] CollectParallelBlockCaptures(
        ExecutionParallelBlock parallel,
        ExecutionRenderContext context)
    {
        var excludedNames = new HashSet<string>(CreateRuntimeHelperParameterNames(context), StringComparer.Ordinal);

        foreach (var runtimeMember in CreateParallelRunnerRuntimeMembers(parallel, context))
            excludedNames.Add(runtimeMember.FieldName);

        foreach (var task in parallel.Tasks)
        {
            excludedNames.Add(task.Output.Name);
            foreach (var variableName in CollectDeclaredVariableNames(task.Body))
                excludedNames.Add(variableName);
        }

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        foreach (var task in parallel.Tasks)
            AddHelperCaptures(task.Body, excludedNames, captures);

        return captures.Values.ToArray();
    }

    private BlockSyntax RenderParallelTaskBody(ExecutionParallelTask task, ExecutionRenderContext context)
    {
        var session = context.Session;
        var previousDeclaredStoredRowsCaches = session.DeclaredStoredRowsCaches;
        session.DeclaredStoredRowsCaches = new HashSet<int>(previousDeclaredStoredRowsCaches);

        try
        {
            var statements = new List<StatementSyntax>
            {
                QueryEmitter.GenerateCancellationCheck()
            };
            statements.AddRange(RenderBlock(task.Body, context).Statements);
            return StatementEmitter.CreateBlock(statements);
        }
        finally
        {
            session.DeclaredStoredRowsCaches = previousDeclaredStoredRowsCaches;
        }
    }

    private static ObjectCreationExpressionSyntax CreateParallelOptionsCreation(int maxDegreeOfParallelism)
    {
        return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(typeof(ParallelOptions)))
            .WithArgumentList(SyntaxFactory.ArgumentList())
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>([
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(nameof(ParallelOptions.CancellationToken)),
                        SyntaxFactory.IdentifierName("token")),
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(nameof(ParallelOptions.MaxDegreeOfParallelism)),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(maxDegreeOfParallelism)))
                ])));
    }
}
