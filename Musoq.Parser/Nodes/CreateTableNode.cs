using System;

namespace Musoq.Parser.Nodes
{
    public class CreateTableNode : Node
    {
        public CreateTableNode(string name, (string TableName, string TypeName)[] tableTypePairs)
        {
            Name = name;
            TableTypePairs = tableTypePairs;
            Id = $"{nameof(CreateTransformationTableNode)}{name}";
        }

        public string Name { get; }

        public (string TableName, string TypeName)[] TableTypePairs { get; }

        public override Type ReturnType => null;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"CREATE TABLE {Name}";
        }
    }
}