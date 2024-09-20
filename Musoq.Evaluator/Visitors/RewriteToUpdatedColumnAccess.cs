using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteToUpdatedColumnAccess(IReadOnlyDictionary<string, string> usedTables) : CloneQueryVisitor
    {
        private WhereNode _where;
        
        private FromNode _from;
        
        public WhereNode Where
        {
            get 
            {
                if (_where is not null) 
                {
                    return _where;
                }
                
                _where = (WhereNode) Nodes.Pop();
                
                return _where;
            }
        }
        
        public FromNode From 
        {
            get 
            {
                if (_from is not null) 
                {
                    return _from;
                }
                
                _from = (FromNode) Nodes.Pop();
                
                return _from;
            }
        }

        public override void Visit(AccessColumnNode node)
        {
            base.Visit(usedTables.TryGetValue(node.Alias, out var alias)
                ? new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), alias, node.ReturnType,
                    TextSpan.Empty)
                : node);
        }
    }
}