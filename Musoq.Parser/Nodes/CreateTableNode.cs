using System;
using System.Text;

namespace Musoq.Parser.Nodes;

public class CreateTableNode : Node
{
    public CreateTableNode(string name, (string ColumnName, string TypeName)[] tableTypePairs)
    {
        Name = name;
        TableTypePairs = tableTypePairs;
        Id = $"{nameof(CreateTableNode)}{name}";
    }

    public string Name { get; }

    public (string ColumnName, string TypeName)[] TableTypePairs { get; }

    public override Type ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var cols = new StringBuilder();
            
        if (TableTypePairs.Length == 0)
            return $"table {Name} {{}};";
            
        if (TableTypePairs.Length == 1)
            return $"table {Name} {{ {TableTypePairs[0].ColumnName} '{TableTypePairs[0].TypeName}' }};";

        cols.Append($"{TableTypePairs[0].ColumnName} '{TableTypePairs[0].TypeName}', ");

        for (var i = 1; i < TableTypePairs.Length - 1; ++i)
        {
            cols.Append($"{TableTypePairs[i].ColumnName} '{TableTypePairs[i].TypeName}', ");
        }

        cols.Append($"{TableTypePairs[^1].ColumnName} '{TableTypePairs[^1].TypeName}'");
            
        return $"table {Name} {{ {cols} }};";
    }
}