using System.Dynamic;
using System.Linq;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a binary switch (tagged union) type annotation:
///     <c>switch Selector { value =&gt; Alias: Type, _ =&gt; Alias: Type }</c>.
///     The parsed branch is selected at runtime by the value of a previously parsed field.
/// </summary>
public class BinarySwitchTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new binary switch type annotation.
    /// </summary>
    /// <param name="selector">The name of the previously parsed field that selects the branch.</param>
    /// <param name="cases">The ordered switch cases, with an optional trailing default case.</param>
    public BinarySwitchTypeNode(string selector, BinarySwitchCaseNode[] cases)
    {
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        Cases = cases ?? throw new ArgumentNullException(nameof(cases));
        Id = $"{nameof(BinarySwitchTypeNode)}{selector}{string.Join(',', cases.Select(c => c.BranchAlias))}";
    }

    /// <summary>
    ///     Gets the name of the previously parsed field that selects the branch.
    /// </summary>
    public string Selector { get; }

    /// <summary>
    ///     Gets the ordered switch cases. A trailing case with <see cref="BinarySwitchCaseNode.IsDefault" />
    ///     set to true, when present, is the default branch.
    /// </summary>
    public BinarySwitchCaseNode[] Cases { get; }

    /// <summary>
    ///     Gets the default case, or null when no default is declared.
    /// </summary>
    public BinarySwitchCaseNode? DefaultCase => Cases.FirstOrDefault(c => c.IsDefault);

    /// <inheritdoc />
    public override Type ClrType => typeof(ExpandoObject);

    /// <inheritdoc />
    public override bool IsFixedSize => false;

    /// <inheritdoc />
    public override int? FixedSizeBytes => null;

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"switch {Selector} {{ {string.Join(", ", Cases.Select(c => c.ToString()))} }}";
    }
}
