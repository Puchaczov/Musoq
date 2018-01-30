using System;
using System.Collections.Generic;
using System.Text;

namespace FQL.Parser.Nodes
{
    public class StarNode : BinaryNode
    {
        public StarNode(Node left, Node right) : base(left, right)
        {
            Id = CalculateId(this);
        }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} * {Right.ToString()}";
        }
    }

    public class FSlashNode : BinaryNode
    {
        public FSlashNode(Node left, Node right) 
            : base(left, right)
        {
            Id = CalculateId(this);
        }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} / {Right.ToString()}";
        }
    }

    public class ModuloNode : BinaryNode
    {
        public ModuloNode(Node left, Node right) 
            : base(left, right)
        {
            Id = CalculateId(this);
        }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} % {Right.ToString()}";
        }
    }

    public class AddNode : BinaryNode
    {
        public AddNode(Node left, Node right) : base(left, right)
        {
            Id = CalculateId(this);
        }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} + {Right.ToString()}";
        }
    }

    public class HyphenNode : BinaryNode
    {
        public HyphenNode(Node left, Node right) 
            : base(left, right)
        {
            Id = CalculateId(this);
        }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} - {Right.ToString()}";
        }
    }
}
