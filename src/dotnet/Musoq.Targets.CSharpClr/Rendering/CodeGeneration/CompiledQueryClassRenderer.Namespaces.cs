using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CompiledQueryClassRenderer
{
    private const int FrozenSetNamespace = 1;
    private const int QueryRowsNamespace = 2;
    private const int StructuralInputsNamespace = 4;
    private const int AllOptionalNamespaces = FrozenSetNamespace | QueryRowsNamespace | StructuralInputsNamespace;

    private IEnumerable<string> ResolveNamespaces(IReadOnlyList<SyntaxNode> members)
    {
        var flags = DetectNamespaces(members);
        var usesFrozenSet = (flags & FrozenSetNamespace) != 0;
        var usesQueryRowsRuntime = (flags & QueryRowsNamespace) != 0;
        var usesStructuralInputs = (flags & StructuralInputsNamespace) != 0;
        foreach (var namespaceName in DefaultNamespaces)
        {
            yield return namespaceName;
            if (usesStructuralInputs && namespaceName == "Musoq.Schema")
                yield return "Musoq.Schema.StructuralInputs";
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

    private sealed class RequiredNamespaceScanner : CSharpSyntaxWalker
    {
        public int Flags;

        public override void Visit(SyntaxNode? node)
        {
            if (Flags != AllOptionalNamespaces)
                base.Visit(node);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (node.Identifier.ValueText == "FrozenSet")
                Flags |= FrozenSetNamespace;
            Check(node);
            base.VisitGenericName(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            Check(node);
            base.VisitIdentifierName(node);
        }

        private void Check(SimpleNameSyntax node)
        {
            if (node.Identifier.ValueText is "QueryRows" or "QueryTableEnumerable" or "QueryEnumerable")
                Flags |= QueryRowsNamespace;
            if (node.Identifier.ValueText is "StructuralTypeDescriptor" or "StructuralFieldDescriptor" or "StructuralDefaultDescriptor" or "StructuralValue")
                Flags |= StructuralInputsNamespace;
        }
    }

    internal static int DetectNamespaces(IReadOnlyList<SyntaxNode> members)
    {
        var scanner = new RequiredNamespaceScanner();
        for (var index = 0; index < members.Count && scanner.Flags != AllOptionalNamespaces; index++)
            scanner.Visit(members[index]);
        return scanner.Flags;
    }
}
