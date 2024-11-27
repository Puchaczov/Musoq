using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Utils.Symbols;

public class IndexBasedContextsPositionsSymbol : Symbol
{
    private readonly Dictionary<int, (string[] Left, string[] Right)> _contextsPositions = new();

    public int GetIndexFor(int index, string alias)
    {
        var leftRight = _contextsPositions[index];
        
        var inLeftIndex = Array.IndexOf(leftRight.Left, alias);
            
        return inLeftIndex == -1 ? Array.IndexOf(leftRight.Right, alias) : inLeftIndex;
    }

    public void Add(IReadOnlyCollection<string> lines)
    {
        var i = lines.Count - 1;
        foreach (var line in lines)
        {
            var leftRight = line.Split(',');
                
            _contextsPositions.Add(i--, (leftRight[0].Split('|'), [leftRight[1]]));
        }
    }
}