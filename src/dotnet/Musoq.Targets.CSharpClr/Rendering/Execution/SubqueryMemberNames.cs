using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

// Private C# helpers have method-local storage identities. Qualify their emitted
// symbols when several independently lowered plans share one generated class.
internal sealed class SubqueryMemberNames : CSharpSyntaxRewriter
{
    private readonly IReadOnlyDictionary<string, string> _names;

    internal SubqueryMemberNames(string identifier, IReadOnlyList<MemberDeclarationSyntax> members)
    {
        var names = members.OfType<MethodDeclarationSyntax>().Select(static method => method.Identifier.ValueText)
            .Concat(members.OfType<ClassDeclarationSyntax>().Select(static type => type.Identifier.ValueText)
                .Where(static name => name is "CteRowResults" or "CteIndexResults" or "StoredTableColumn" ||
                                      name.StartsWith("StoredTable", StringComparison.Ordinal)));
        _names = names.Distinct(StringComparer.Ordinal).ToDictionary(static name => name,
            name => $"{identifier}_{name}", StringComparer.Ordinal);
    }

    public override SyntaxToken VisitToken(SyntaxToken token) =>
        token.IsKind(SyntaxKind.IdentifierToken) && _names.TryGetValue(token.ValueText, out var name)
            ? SyntaxFactory.Identifier(token.LeadingTrivia, name, token.TrailingTrivia)
            : base.VisitToken(token);
}
