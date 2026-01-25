using System;
using System.Collections.Generic;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a string type annotation: string[size] encoding modifiers [as TextSchemaName].
/// </summary>
public class StringTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new string type annotation.
    /// </summary>
    /// <param name="sizeExpression">The expression determining byte size.</param>
    /// <param name="encoding">The character encoding.</param>
    /// <param name="modifiers">Optional string processing modifiers.</param>
    /// <param name="asTextSchemaName">Optional text schema name for Binary-Text composition.</param>
    public StringTypeNode(Node sizeExpression, StringEncoding encoding, StringModifier modifiers = StringModifier.None,
        string? asTextSchemaName = null)
    {
        SizeExpression = sizeExpression ?? throw new ArgumentNullException(nameof(sizeExpression));
        Encoding = encoding;
        Modifiers = modifiers;
        AsTextSchemaName = asTextSchemaName;
        Id = $"{nameof(StringTypeNode)}{sizeExpression.Id}{encoding}{modifiers}{asTextSchemaName ?? ""}";
    }

    /// <summary>
    ///     Gets the expression that determines the byte size.
    ///     For UTF-8, this is bytes not characters.
    /// </summary>
    public Node SizeExpression { get; }

    /// <summary>
    ///     Gets the character encoding.
    /// </summary>
    public StringEncoding Encoding { get; }

    /// <summary>
    ///     Gets the string processing modifiers.
    /// </summary>
    public StringModifier Modifiers { get; }

    /// <summary>
    ///     Gets the text schema name for Binary-Text composition (the 'as SchemaName' clause).
    ///     When set, the string field is further parsed using the specified text schema.
    /// </summary>
    public string? AsTextSchemaName { get; }

    /// <inheritdoc />
    public override Type ClrType => AsTextSchemaName != null ? typeof(object) : typeof(string);

    /// <inheritdoc />
    public override bool IsFixedSize => SizeExpression is IntegerNode;

    /// <inheritdoc />
    public override int? FixedSizeBytes => SizeExpression is IntegerNode intNode
        ? int.Parse(intNode.ObjValue.ToString()!)
        : null;

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var encodingStr = Encoding.ToString().ToLowerInvariant();
        var modifierStr = GetModifierString();
        var asClause = AsTextSchemaName != null ? $" as {AsTextSchemaName}" : "";

        return string.IsNullOrEmpty(modifierStr)
            ? $"string[{SizeExpression.ToString()}] {encodingStr}{asClause}"
            : $"string[{SizeExpression.ToString()}] {encodingStr} {modifierStr}{asClause}";
    }

    private string GetModifierString()
    {
        var parts = new List<string>();

        if ((Modifiers & StringModifier.Trim) != 0)
            parts.Add("trim");
        if ((Modifiers & StringModifier.RTrim) != 0)
            parts.Add("rtrim");
        if ((Modifiers & StringModifier.LTrim) != 0)
            parts.Add("ltrim");
        if ((Modifiers & StringModifier.NullTerm) != 0)
            parts.Add("nullterm");

        return string.Join(" ", parts);
    }
}
