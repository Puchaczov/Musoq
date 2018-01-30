using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public abstract class SetOperatorNode : BinaryNode
    {
        protected SetOperatorNode(TokenType type, string[] keys, Node left, Node right, bool isNested,
            bool isTheLastOne)
            : base(left, right)
        {
            OperatorType = (SetOperator) type;
            Keys = keys;
            IsNested = isNested;
            IsTheLastOne = isTheLastOne;
            Id = CalculateId(this);
        }

        public SetOperator OperatorType { get; }

        public string[] Keys { get; }

        public override Type ReturnType => typeof(void);

        public override string Id { get; }

        public string ResultTableName { get; protected set; }

        public bool IsNested { get; }

        public bool IsTheLastOne { get; }
    }
}