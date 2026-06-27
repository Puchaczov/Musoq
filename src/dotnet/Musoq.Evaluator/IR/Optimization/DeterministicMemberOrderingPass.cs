using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class DeterministicMemberOrderingPass : IPlanOptimizationPass<CompilationUnitSyntax>
{
    public string Name => "DeterministicMemberOrdering";

    public OptimizationResult<CompilationUnitSyntax> Optimize(CompilationUnitSyntax plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var classes = plan.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
        var memberCount = classes.Sum(static declaration => declaration.Members.Count);

        var rewriter = new MemberOrderingRewriter();
        var optimized = (CompilationUnitSyntax)rewriter.Visit(plan)!;
        if (rewriter.ReorderedClasses == 0)
        {
            return OptimizationResult<CompilationUnitSyntax>.NoChange(
                plan,
                $"Observed {classes.Length} generated class(es) and {memberCount} class member(s); member order was already deterministic.");
        }

        return OptimizationResult<CompilationUnitSyntax>.Changed(
            optimized,
            $"Reordered members in {rewriter.ReorderedClasses} generated class(es).");
    }

    private sealed class MemberOrderingRewriter : CSharpSyntaxRewriter
    {
        public int ReorderedClasses { get; private set; }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var visited = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            var sortedMembers = visited.Members
                .OrderBy(GetMemberGroup)
                .ThenBy(GetMemberName, StringComparer.Ordinal)
                .ThenBy(static member => member.SpanStart)
                .ToArray();

            if (visited.Members.SequenceEqual(sortedMembers))
                return visited;

            ReorderedClasses++;
            return visited.WithMembers(SyntaxFactory.List(sortedMembers));
        }

        private static int GetMemberGroup(MemberDeclarationSyntax member)
        {
            return member switch
            {
                FieldDeclarationSyntax => 0,
                ConstructorDeclarationSyntax => 1,
                PropertyDeclarationSyntax property when HasModifier(property.Modifiers, SyntaxKind.PublicKeyword) => 2,
                EventFieldDeclarationSyntax eventField when HasModifier(eventField.Modifiers, SyntaxKind.PublicKeyword) => 3,
                EventDeclarationSyntax eventDeclaration when HasModifier(eventDeclaration.Modifiers, SyntaxKind.PublicKeyword) => 3,
                MethodDeclarationSyntax method => GetMethodGroup(method.Modifiers),
                PropertyDeclarationSyntax property => GetNonPublicMemberGroup(property.Modifiers),
                EventFieldDeclarationSyntax eventField => GetNonPublicMemberGroup(eventField.Modifiers),
                EventDeclarationSyntax eventDeclaration => GetNonPublicMemberGroup(eventDeclaration.Modifiers),
                ClassDeclarationSyntax => 8,
                StructDeclarationSyntax => 8,
                RecordDeclarationSyntax => 8,
                _ => 9
            };
        }

        private static int GetMethodGroup(SyntaxTokenList modifiers)
        {
            if (HasModifier(modifiers, SyntaxKind.PublicKeyword))
                return 4;

            if (HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
                return 5;

            if (HasModifier(modifiers, SyntaxKind.StaticKeyword))
                return 7;

            return 6;
        }

        private static int GetNonPublicMemberGroup(SyntaxTokenList modifiers)
        {
            if (HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
                return 5;

            if (HasModifier(modifiers, SyntaxKind.StaticKeyword))
                return 7;

            return 6;
        }

        private static string GetMemberName(MemberDeclarationSyntax member)
        {
            return member switch
            {
                FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty,
                EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier.ValueText,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                ClassDeclarationSyntax nestedClass => nestedClass.Identifier.ValueText,
                StructDeclarationSyntax nestedStruct => nestedStruct.Identifier.ValueText,
                RecordDeclarationSyntax nestedRecord => nestedRecord.Identifier.ValueText,
                _ => string.Empty
            };
        }

        private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
        {
            return CodegenReadabilitySyntaxFacts.HasModifier(modifiers, kind);
        }
    }
}
