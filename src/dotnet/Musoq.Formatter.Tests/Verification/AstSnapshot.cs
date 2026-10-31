using System;
using System.Collections.Immutable;
using System.Linq;

namespace Musoq.Formatter.Tests.Verification;

internal sealed class AstSnapshot(string structure, ImmutableArray<Type> returnTypes) : IEquatable<AstSnapshot>
{
    public bool Equals(AstSnapshot? other) => other != null && structure == other.Structure &&
        returnTypes.SequenceEqual(other.ReturnTypes);

    private string Structure => structure;
    private ImmutableArray<Type> ReturnTypes => returnTypes;

    public override bool Equals(object? obj) => obj is AstSnapshot other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(structure);
    public override string ToString() => structure;
}
