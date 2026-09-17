using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Parser.Nodes;

public enum StructuralTypeSyntaxKind
{
    Scalar,
    Record,
    Collection
}

/// <summary>
///     Recursive type syntax used by structural parameter and script-variable declarations.
///     This is deliberately independent from interpretation-schema type annotations.
/// </summary>
public sealed class StructuralTypeSyntaxNode : Node
{
    private StructuralTypeSyntaxNode(
        StructuralTypeSyntaxKind kind,
        string? scalarName,
        IReadOnlyList<StructuralTypeFieldSyntax>? fields,
        StructuralTypeSyntaxNode? elementType,
        bool isNullable,
        TextSpan span)
    {
        Kind = kind;
        ScalarName = scalarName;
        Fields = fields == null ? [] : new ReadOnlyCollection<StructuralTypeFieldSyntax>(fields.ToArray());
        ElementType = elementType;
        IsNullable = isNullable;
        Span = span;
        FullSpan = span;
        Id = $"{nameof(StructuralTypeSyntaxNode)}{ToString()}";
    }

    public StructuralTypeSyntaxKind Kind { get; }

    public string? ScalarName { get; }

    public IReadOnlyList<StructuralTypeFieldSyntax> Fields { get; }

    public StructuralTypeSyntaxNode? ElementType { get; }

    /// <summary>
    ///     Gets whether this type node has a postfix nullable marker.
    /// </summary>
    public bool IsNullable { get; }

    public static StructuralTypeSyntaxNode Scalar(string name, TextSpan span, bool isNullable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A scalar type name is required.", nameof(name));
        return new StructuralTypeSyntaxNode(StructuralTypeSyntaxKind.Scalar, name, null, null, isNullable, span);
    }

    public static StructuralTypeSyntaxNode Record(
        IReadOnlyList<StructuralTypeFieldSyntax> fields,
        TextSpan span,
        bool isNullable = false)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new StructuralTypeSyntaxNode(StructuralTypeSyntaxKind.Record, null, fields, null, isNullable, span);
    }

    public static StructuralTypeSyntaxNode Collection(
        StructuralTypeSyntaxNode elementType,
        TextSpan span,
        bool isNullable = false)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        return new StructuralTypeSyntaxNode(StructuralTypeSyntaxKind.Collection, null, null, elementType, isNullable, span);
    }

    public StructuralTypeSyntaxNode WithNullable(TextSpan span)
    {
        return new StructuralTypeSyntaxNode(Kind, ScalarName, Fields, ElementType, true, Span.IsEmpty ? span : Span.Through(span));
    }

    public override Type? ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        // Type syntax is metadata carried by declarations.  It intentionally uses the
        // generic visitor hook until semantic declaration traversal is introduced.
        visitor.Visit((Node)this);
    }

    public override string ToString()
    {
        var text = Kind switch
        {
            StructuralTypeSyntaxKind.Scalar => ScalarName!,
            StructuralTypeSyntaxKind.Record => $"({string.Join(", ", Fields.Select(static field => field.ToString()))})",
            StructuralTypeSyntaxKind.Collection => $"{ElementType!.ToString()}[]",
            _ => throw new InvalidOperationException($"Unknown structural type kind {Kind}.")
        };
        return IsNullable ? $"{text}?" : text;
    }
}

public sealed class StructuralTypeFieldSyntax
{
    public StructuralTypeFieldSyntax(
        string name,
        StructuralTypeSyntaxNode type,
        Node? defaultValue,
        TextSpan nameSpan,
        TextSpan span)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Field name cannot be empty.", nameof(name)) : name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
        NameSpan = nameSpan;
        Span = span;
    }

    public string Name { get; }

    public StructuralTypeSyntaxNode Type { get; }

    public Node? DefaultValue { get; }

    public TextSpan NameSpan { get; }

    public TextSpan Span { get; }

    public bool HasDefault => DefaultValue != null;

    public override string ToString()
    {
        return HasDefault ? $"{Name}: {Type.ToString()} = {DefaultValue!.ToString()}" : $"{Name}: {Type.ToString()}";
    }
}
