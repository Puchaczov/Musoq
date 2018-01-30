using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class ShouldBePresentInTheTable : Node
    {
        public ShouldBePresentInTheTable(string table, bool negate, string[] keys)
        {
            Table = table;
            ExpectedResult = negate;
            Keys = keys;
            var idKeys = keys.Length == 0 ? string.Empty : keys.Aggregate((a, b) => a + b);
            Id = $"{nameof(ShouldBePresentInTheTable)}{table}{negate}{idKeys}";
        }

        public string Table { get; }

        public string[] Keys { get; }

        public bool ExpectedResult { get; }

        public override Type ReturnType => null;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"Should be present in the table {Table}";
        }
    }
}