using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Musoq.Parser.Nodes;

namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private enum Purpose { Structure, Coverage }

    private readonly Purpose purpose;
    private readonly StringBuilder? structure;
    private readonly ImmutableArray<Type>.Builder returnTypes = ImmutableArray.CreateBuilder<Type>();
    private readonly HashSet<string> coverage = new(StringComparer.Ordinal);
    private readonly HashSet<object> active = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

    private AstProjector(Purpose purpose)
    {
        this.purpose = purpose;
        if (purpose == Purpose.Structure)
            structure = new StringBuilder();
    }

    internal static AstSnapshot Snapshot(Node node)
    {
        var writer = new AstProjector(Purpose.Structure);
        writer.Write(node);
        return new AstSnapshot(writer.structure!.ToString(), writer.returnTypes.ToImmutable());
    }

    internal static IReadOnlySet<string> Coverage(Node node)
    {
        var writer = new AstProjector(Purpose.Coverage);
        writer.Write(node);
        return writer.coverage.ToFrozenSet(StringComparer.Ordinal);
    }

    private void ProjectValue<T>(T value, string identity, Action<AstProjector, T> project) where T : struct
    {
        coverage.Add(identity);
        structure?.Append(identity).Append('{');
        project(this, value);
        structure?.Append('}');
    }

    private void ProjectNode<T>(T? value, string identity, Action<AstProjector, T> project) where T : class
    {
        if (value == null)
        {
            Null();
            return;
        }
        // CLR equality rejects unreviewed derived shapes; no runtime members are discovered.
        if (value.GetType() != typeof(T))
            throw new InvalidOperationException("Unreviewed derived parser shape: " + identity);
        coverage.Add(identity);
        if (purpose == Purpose.Coverage && !visited.Add(value))
            return;
        if (!active.Add(value))
            throw new InvalidOperationException("Cyclic source AST: " + identity);
        structure?.Append(identity).Append('{');
        project(this, value);
        structure?.Append('}');
        active.Remove(value);
    }

    private void Member(string name) => structure?.Append(name).Append('=');
    private void Null() => structure?.Append("null;");

    private void Write(Type? value)
    {
        if (value == null)
        {
            Null();
            return;
        }
        if (value is NullNode.NullType)
        {
            structure?.Append("parser-null-type;");
            return;
        }
        structure?.Append("clr-type;");
        if (purpose == Purpose.Structure)
            returnTypes.Add(value);
    }

    private void Unbound(object? value)
    {
        if (value != null)
            throw new InvalidOperationException("Execution-bound metadata is not part of a source AST.");
        Null();
    }
}
