namespace Musoq.Parser.Nodes.From;

public class UnpivotEntryNode(Node expression, string nameValue, TextSpan nameValueSpan)
{
    public UnpivotEntryNode(Node expression, string nameValue)
        : this(expression, nameValue, TextSpan.Empty)
    {
    }

    public Node Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));

    public string NameValue { get; } = nameValue ?? throw new ArgumentNullException(nameof(nameValue));

    public TextSpan NameValueSpan { get; } = nameValueSpan;

    public string Id => $"{nameof(UnpivotEntryNode)}{Expression.Id}{NameValue}";

    public override string ToString()
    {
        return $"{Expression} as {NameValue}";
    }
}
