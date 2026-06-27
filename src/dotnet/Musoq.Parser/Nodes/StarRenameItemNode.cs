namespace Musoq.Parser.Nodes;

public sealed record StarRenameItemNode(string SourceName, string TargetName)
{
    public override string ToString()
    {
        return $"{SourceName} as {TargetName}";
    }
}
