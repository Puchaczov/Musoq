using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed class CompiledQueryClassRenderer(RenderContext context)
{
    private const string DefaultNamespaceName = "Query.Compiled";
    private const string DefaultClassName = "CompiledQuery";

    private static readonly string[] DefaultNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Threading",
        "System.Threading.Tasks",
        "Microsoft.Extensions.Logging",
        "Musoq.Schema",
        "Musoq.Schema.Optimization",
        "Musoq.Evaluator",
        "Musoq.Evaluator.Tables",
        "Musoq.Evaluator.Helpers",
        "Musoq.Schema.DataSources",
        "System.Linq"
    ];

    private readonly RenderContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public CompilationUnitSyntax Render(
        string queryIdentifier,
        int inMemoryTableCount = 0,
        int cteIndexResultCount = 0,
        string? runMethodNameOverride = null)
    {
        var runMethodName = ResolveRunMethodName(queryIdentifier, runMethodNameOverride);
        var members = CreateClassMembers(runMethodName, inMemoryTableCount, cteIndexResultCount);
        var classDeclaration = (ClassDeclarationSyntax)CreateClassDeclaration(members);
        var namespaceDeclaration = ClassEmitter.CreateNamespaceDeclaration(
            ResolveNamespaceName(),
            ResolveNamespaces(members),
            classDeclaration);

        return ClassEmitter.CreateCompilationUnit(namespaceDeclaration);
    }


    private bool IsInstrumentationEnabled => _context.InstrumentationMode != QueryInstrumentationMode.Disabled;

    private SyntaxNode CreateClassDeclaration(IList<SyntaxNode> members)
    {
        if (_context.ResultMode == QueryResultMode.TypedEnumerable)
        {
            var outputType = _context.OutputType
                             ?? throw new InvalidOperationException("Typed enumerable result mode requires an output type.");
            return ClassEmitter.CreateTypedClassDeclaration(
                _context.Generator,
                DefaultClassName,
                outputType,
                members);
        }

        return ClassEmitter.CreateClassDeclaration(
            _context.Generator,
            DefaultClassName,
            members,
            IsInstrumentationEnabled);
    }

    private IEnumerable<string> ResolveNamespaces(IReadOnlyList<SyntaxNode> members)
    {
        var usesFrozenSet = members.Any(UsesFrozenSet);
        var usesQueryRowsRuntime = members.Any(UsesQueryRowsRuntime);
        foreach (var namespaceName in DefaultNamespaces)
        {
            yield return namespaceName;
            if (usesQueryRowsRuntime && namespaceName == "Musoq.Evaluator.Helpers")
                yield return "Musoq.Evaluator.Runtime";
            if (usesFrozenSet && namespaceName == "System.Collections.Generic")
                yield return "System.Collections.Frozen";
            if (IsInstrumentationEnabled && namespaceName == "Musoq.Evaluator")
                yield return "Musoq.Evaluator.Diagnostics";
            if (IsInstrumentationEnabled && namespaceName == "Musoq.Schema")
                yield return "Musoq.Schema.Diagnostics";
        }
    }

    private static bool UsesFrozenSet(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<GenericNameSyntax>()
            .Any(static name => name.Identifier.ValueText == "FrozenSet");
    }

    private static bool UsesQueryRowsRuntime(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<SimpleNameSyntax>()
            .Any(static name => name.Identifier.ValueText is "QueryRows" or "QueryTableEnumerable" or "QueryEnumerable");
    }
    private List<SyntaxNode> CreateClassMembers(
        string computeMethodName,
        int inMemoryTableCount,
        int cteIndexResultCount)
    {
        var members = new List<SyntaxNode>();

        foreach (var classMember in _context.ClassMembers)
        {
            if (classMember is not MemberDeclarationSyntax)
                throw UnsupportedShape.Of(
                    $"Class member type {classMember.GetType().Name}", "CompiledQueryClassRenderer");

            members.Add(classMember);
        }

        if (!ContainsMethod(members, computeMethodName))
            members.Add(CreatePlaceholderComputeMethod(computeMethodName));

        if (_context.ResultMode == QueryResultMode.TypedEnumerable)
        {
            var outputType = _context.OutputType
                             ?? throw new InvalidOperationException("Typed enumerable result mode requires an output type.");
            ClassEmitter.AddTypedRunnableMembers(
                members,
                computeMethodName,
                outputType,
                _context.ScriptParameterDefinitions);
        }
        else if (_context.ResultMode == QueryResultMode.TableViaRows)
        {
            var resultInfo = _context.TableViaRowsResult
                             ?? throw new InvalidOperationException("TableViaRows result mode requires final select-shape result metadata.");
            if (IsInstrumentationEnabled)
            {
                ClassEmitter.AddProfiledTableViaRowsRunnableMembers(
                    members,
                    computeMethodName,
                    resultInfo,
                    _context.ScriptParameterDefinitions,
                    _context.ForceTableResultMaterialization);
            }
            else
            {
                ClassEmitter.AddTableViaRowsRunnableMembers(
                    members,
                    computeMethodName,
                    resultInfo,
                    _context.ScriptParameterDefinitions,
                    forceTableResultMaterialization: _context.ForceTableResultMaterialization);
            }
        }
        else if (_context.TableViaRowsResult != null)
        {
            if (IsInstrumentationEnabled)
            {
                ClassEmitter.AddProfiledTableViaRowsRunnableMembers(
                    members,
                    computeMethodName,
                    _context.TableViaRowsResult,
                    _context.ScriptParameterDefinitions,
                    _context.ForceTableResultMaterialization);
            }
            else
            {
                ClassEmitter.AddTableViaRowsRunnableMembers(
                    members,
                    computeMethodName,
                    _context.TableViaRowsResult,
                    _context.ScriptParameterDefinitions,
                    useLifecycleWrapper: false,
                    forceTableResultMaterialization: _context.ForceTableResultMaterialization);
            }
        }
        else if (IsInstrumentationEnabled)
        {
            ClassEmitter.AddProfiledRunnableMembers(
                members,
                $"{computeMethodName}(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token, null)",
                $"{computeMethodName}(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token, profileRecorder)",
                _context.ScriptParameterDefinitions);
        }
        else
        {
            ClassEmitter.AddRunnableMembers(
                members,
                $"{computeMethodName}(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token)",
                _context.ScriptParameterDefinitions);
        }

        return members;
    }

    private string ResolveComputeMethodName(string queryIdentifier)
    {
        return QueryMethodNameResolver.Resolve(_context, queryIdentifier);
    }

    private string ResolveRunMethodName(string queryIdentifier, string? runMethodNameOverride)
    {
        if (!string.IsNullOrWhiteSpace(runMethodNameOverride))
            return runMethodNameOverride;

        return ResolveComputeMethodName(queryIdentifier);
    }

    private string ResolveNamespaceName()
    {
        return string.IsNullOrWhiteSpace(_context.AssemblyName)
            ? DefaultNamespaceName
            : _context.AssemblyName;
    }

    private static bool ContainsMethod(IEnumerable<SyntaxNode> members, string methodName)
    {
        return members
            .OfType<MethodDeclarationSyntax>()
            .Any(method => method.Identifier.ValueText == methodName);
    }

    private MethodDeclarationSyntax CreatePlaceholderComputeMethod(string computeMethodName)
    {
        if (_context.ResultMode == QueryResultMode.TypedEnumerable)
        {
            return CreatePlaceholderTypedComputeMethod(
                computeMethodName,
                _context.OutputType
                ?? throw new InvalidOperationException("Typed enumerable result mode requires an output type."));
        }

        if (_context.TableViaRowsResult is { } resultInfo)
            return CreatePlaceholderRowsComputeMethod(computeMethodName, resultInfo.RowTypeName);

        var exception = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(NotImplementedException)))
            .WithArgumentList(SyntaxFactory.ArgumentList());
        var body = StatementEmitter.CreateBlock(StatementEmitter.CreateThrow(exception));

        return MethodDeclarationHelper.CreateStandardPrivateMethod(computeMethodName, body);
    }

    private static MethodDeclarationSyntax CreatePlaceholderTypedComputeMethod(string computeMethodName, Type outputType)
    {
        var exception = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(InvalidOperationException)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Typed enumerable query method was not generated."))))));
        var body = StatementEmitter.CreateBlock(StatementEmitter.CreateThrow(exception));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            ExecutionSyntaxFactory.CreateTypeSyntax(outputType)))),
                SyntaxFactory.Identifier(computeMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateTypedRunContextParameterList())
            .WithBody(body);
    }

    private static MethodDeclarationSyntax CreatePlaceholderRowsComputeMethod(string computeMethodName, string rowTypeName)
    {
        var exception = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(InvalidOperationException)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Table rows query method was not generated."))))));
        var body = StatementEmitter.CreateBlock(StatementEmitter.CreateThrow(exception));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{rowTypeName}>"),
                SyntaxFactory.Identifier(computeMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateStandardParameterList())
            .WithBody(body);
    }
}
