namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a single case in a binary switch (tagged union) field.
///     Each case maps a constant selector value (or the default <c>_</c>) to a branch
///     with a unique alias and a binary type annotation.
/// </summary>
public class BinarySwitchCaseNode
{
    /// <summary>
    ///     Creates a new binary switch case.
    /// </summary>
    /// <param name="caseValue">The constant selector value this case matches, or null for the default case.</param>
    /// <param name="branchAlias">The alias exposed for this branch.</param>
    /// <param name="branchType">The binary type annotation parsed when this case is selected.</param>
    public BinarySwitchCaseNode(Node? caseValue, string branchAlias, TypeAnnotationNode branchType)
    {
        CaseValue = caseValue;
        BranchAlias = branchAlias ?? throw new ArgumentNullException(nameof(branchAlias));
        BranchType = branchType ?? throw new ArgumentNullException(nameof(branchType));
    }

    /// <summary>
    ///     Gets the constant selector value this case matches.
    ///     Null indicates the default case (<c>_</c>).
    /// </summary>
    public Node? CaseValue { get; }

    /// <summary>
    ///     Gets the alias exposed for this branch.
    /// </summary>
    public string BranchAlias { get; }

    /// <summary>
    ///     Gets the binary type annotation parsed when this case is selected.
    /// </summary>
    public TypeAnnotationNode BranchType { get; }

    /// <summary>
    ///     Gets whether this is the default case.
    /// </summary>
    public bool IsDefault => CaseValue == null;

    /// <inheritdoc />
    public override string ToString()
    {
        var label = IsDefault ? "_" : CaseValue!.ToString();
        return $"{label} => {BranchAlias}: {BranchType}";
    }
}
