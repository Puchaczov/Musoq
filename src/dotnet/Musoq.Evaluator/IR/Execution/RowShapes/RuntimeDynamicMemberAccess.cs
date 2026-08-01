using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Reads a schema-advertised member from a public <see cref="System.Dynamic.DynamicObject" />
/// source row. The generated target is allowed to use a DLR get-member operation
/// for this access; all surrounding values remain statically typed.
/// </summary>
public sealed record RuntimeDynamicMemberAccess : FieldAccessStrategy
{
    public RuntimeDynamicMemberAccess(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("A runtime dynamic member name is required.", nameof(memberName));

        MemberName = memberName;
    }

    public string MemberName { get; }
}

/// <summary>
/// Describes a nested path whose root is a source field and whose segments were
/// resolved from schema metadata and dynamic type hints.
/// </summary>
public sealed record RuntimeDynamicMemberPathAccess : FieldAccessStrategy
{
    public RuntimeDynamicMemberPathAccess(
        string rootFieldName,
        ExecutionTypeRef rootFieldType,
        IReadOnlyList<RuntimeDynamicMemberPathSegment> segments,
        bool rootIsDynamic = true)
    {
        if (string.IsNullOrWhiteSpace(rootFieldName))
            throw new ArgumentException("A runtime dynamic root field name is required.", nameof(rootFieldName));

        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0)
            throw new ArgumentException("At least one path segment is required.", nameof(segments));

        RootFieldName = rootFieldName;
        RootFieldType = rootFieldType;
        Segments = segments;
        RootIsDynamic = rootIsDynamic;
    }

    public string RootFieldName { get; }

    public ExecutionTypeRef RootFieldType { get; }

    public IReadOnlyList<RuntimeDynamicMemberPathSegment> Segments { get; }

    public bool RootIsDynamic { get; }
}

public sealed record RuntimeDynamicMemberPathSegment
{
    public RuntimeDynamicMemberPathSegment(string memberName, ExecutionTypeRef resultType, bool isDynamic)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("A runtime dynamic path member name is required.", nameof(memberName));

        MemberName = memberName;
        ResultType = resultType;
        IsDynamic = isDynamic;
    }

    public string MemberName { get; }

    public ExecutionTypeRef ResultType { get; }

    public bool IsDynamic { get; }
}
