using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class ExtractAccessColumnFromQueryVisitor : CloneQueryVisitor
{
    private readonly Dictionary<string, List<AccessColumnNode>> _accessColumns = new();
    
    public AccessColumnNode[] GetForAliases(params string[] aliases)
    {
        return _accessColumns.Where(a => aliases.Contains(a.Key)).SelectMany(a => a.Value).ToArray();
    }
    
    public AccessColumnNode[] GetForAliases(string first, string second)
    {
        return _accessColumns.Where(a => a.Key == first || a.Key == second).SelectMany(a => a.Value).ToArray();
    }
    
    public AccessColumnNode[] GetForAlias(string alias)
    {
        return _accessColumns[alias].ToArray();
    }
    
    public override void Visit(AccessColumnNode node)
    {
        if (_accessColumns.TryGetValue(node.Alias, out var list))
        {
            if (list.Any(f => f.Name == node.Name))
            {
                base.Visit(node);
                return;
            }
            
            list.Add(node);
            base.Visit(node);
            return;
        }
        
        _accessColumns.Add(node.Alias, new List<AccessColumnNode> {node});
        base.Visit(node);
    }
}