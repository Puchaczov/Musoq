using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Formatter.Tests.Verification;

internal static class ProjectionAudit
{
    private const string Projector = "Musoq.Formatter.Tests.Verification.AstProjector";

    internal static void Verify(CSharpCompilation compilation, IEnumerable<string> required)
    {
        var projector = compilation.GetTypeByMetadataName(Projector) ?? throw new InvalidOperationException("Missing typed projector.");
        var handlers = projector.GetMembers("Project").OfType<IMethodSymbol>().ToArray();
        var types = handlers.Select(method => VerificationCompiler.Key((INamedTypeSymbol)method.Parameters.Single().Type)).ToHashSet(StringComparer.Ordinal);
        var missing = required.Except(types).ToArray();
        if (missing.Length != 0)
            throw new InvalidOperationException("Missing typed handlers: " + string.Join(", ", missing));
        var dispatched = Dispatches(compilation, projector);
        foreach (var handler in handlers)
        {
            var type = (INamedTypeSymbol)handler.Parameters.Single().Type;
            if (!dispatched.Contains(VerificationCompiler.Key(type)))
                throw new InvalidOperationException("Unreachable typed handler: " + type.Name);
            VerifyHandler(compilation, handler, type);
        }
    }

    private static HashSet<string> Dispatches(CSharpCompilation compilation, INamedTypeSymbol projector)
    {
        var dispatched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in projector.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax();
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var call in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "ProjectNode" or "ProjectValue" } method || method.TypeArguments.Length != 1)
                    continue;
                var type = method.TypeArguments[0];
                var nested = call.ArgumentList.Arguments.Last().DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(invocation => model.GetSymbolInfo(invocation).Symbol).OfType<IMethodSymbol>()
                    .SingleOrDefault(candidate => candidate.Name == "Project" && candidate.ContainingType.Name == "AstProjector");
                if (nested == null || !SymbolEqualityComparer.Default.Equals(nested.Parameters.Single().Type, type))
                    throw new InvalidOperationException("Dispatch must invoke the matching typed projection.");
                var identity = model.GetConstantValue(call.ArgumentList.Arguments[1].Expression);
                if (!identity.HasValue || !Equals(identity.Value, VerificationCompiler.Key((INamedTypeSymbol)type)))
                    throw new InvalidOperationException("Dispatch coverage identity does not match its CLR type.");
                dispatched.Add(VerificationCompiler.Key((INamedTypeSymbol)type));
            }
        }
        return dispatched;
    }

    private static void VerifyHandler(CSharpCompilation compilation, IMethodSymbol handler, INamedTypeSymbol type)
    {
        var declaration = (MethodDeclarationSyntax)handler.DeclaringSyntaxReferences.Single().GetSyntax();
        var model = compilation.GetSemanticModel(declaration.SyntaxTree);
        var statements = declaration.Body?.Statements ?? throw new InvalidOperationException("Projection must have an explicit body.");
        var members = ProjectionPolicy.Members(type).Where(member => ProjectionPolicy.Exclusion(member) == null).ToArray();
        if (statements.Count != members.Length * 2)
            throw new InvalidOperationException("Incomplete member projection: " + type.Name);
        var covered = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        for (var index = 0; index < statements.Count; index += 2)
        {
            var label = Invocation(statements[index]);
            RequireMethod(model, label, "Member");
            var accessor = label.ArgumentList.Arguments.Single().Expression as InvocationExpressionSyntax;
            if (accessor?.Expression.ToString() != "nameof")
                throw new InvalidOperationException("Member labels must reference actual typed accessors.");
            var expression = accessor.ArgumentList.Arguments.Single().Expression;
            var member = model.GetSymbolInfo(expression).Symbol;
            if (member == null || !members.Contains(member, SymbolEqualityComparer.Default) || !covered.Add(member))
                throw new InvalidOperationException("Unknown, duplicate or excluded member accessor: " + expression);
            RequireReceiver(expression, handler.Parameters.Single().Name);
            ProjectionValueAudit.Verify(model, Invocation(statements[index + 1]), ProjectionPolicy.ValueType(member), expression.ToString());
        }
    }

    internal static InvocationExpressionSyntax Invocation(StatementSyntax statement) =>
        (statement as ExpressionStatementSyntax)?.Expression as InvocationExpressionSyntax ??
        throw new InvalidOperationException("Expected a direct projection invocation.");

    internal static void RequireMethod(SemanticModel model, InvocationExpressionSyntax call, string name)
    {
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || method.Name != name ||
            method.ContainingType.ToDisplayString() != Projector)
            throw new InvalidOperationException("Expected typed projector operation: " + name);
    }

    private static void RequireReceiver(ExpressionSyntax expression, string parameter)
    {
        if (expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } || receiver.Identifier.ValueText != parameter)
            throw new InvalidOperationException("Accessor must read the projected instance directly.");
    }
}
