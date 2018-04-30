using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Musoq.Parser;

namespace Musoq.Evaluator.CSharpSimplifiedSyntax
{

    public abstract class SimplifiedNode
    {

        [DebuggerStepThrough]
        public abstract void Accept(ISimplifiedExpression visitor);
    }

    public interface ISimplifiedExpression
    {
        void Visit(StatementsNode node);
        void Visit(IdentifierNode node);
        void Visit(ForeachNode node);
        void Visit(CSharpComplexNode node);
    }

    public class StatementsNode : SimplifiedNode
    {
        public SimplifiedNode[] Nodes { get; set; }
        public override void Accept(ISimplifiedExpression visitor)
        {
        }
    }

    public class IdentifierNode : SimplifiedNode
    {
        public string Name { get; set; }
        public override void Accept(ISimplifiedExpression visitor)
        {
        }
    }

    public class ForeachNode : SimplifiedNode
    {
        public IdentifierNode Variable { get; set; }

        public IdentifierNode Source { get; set; }

        public SimplifiedNode Block { get; set; }
        public override void Accept(ISimplifiedExpression visitor)
        {
        }
    }

    public class CSharpComplexNode : SimplifiedNode
    {
        public SyntaxNode Syntax { get; set; }
        public override void Accept(ISimplifiedExpression visitor)
        {
        }
    }
}
