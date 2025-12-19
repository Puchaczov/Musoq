using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator;

public partial class Operators
{
    private static readonly ConcurrentDictionary<string, Regex> LikePatternCache = new();
    private static readonly ConcurrentDictionary<string, Regex> RLikePatternCache = new();
    private static readonly Regex EscapePattern = CreateEscapeRegex();

    public bool? Like(string content, string searchFor)
    {
        if (content is null || searchFor is null)
            return null;
            
        var regex = LikePatternCache.GetOrAdd(searchFor, pattern =>
        {
            var escaped = EscapePattern.Replace(pattern, match => @"\" + match.Value);
            var sqlPattern = escaped.Replace("_", ".").Replace("%", ".*");
            return new Regex(@"\A" + sqlPattern + @"\z", RegexOptions.Singleline | RegexOptions.Compiled);
        });
        
        return regex.IsMatch(content);
    }

    public bool? RLike(string content, string pattern)
    {
        if (content is null || pattern is null)
            return null;
            
        var regex = RLikePatternCache.GetOrAdd(pattern, p => 
            new Regex(p, RegexOptions.Compiled));
        
        return regex.IsMatch(content);
    }

    public bool Contains<T>(T value, T[] values)
    {
        return values.Contains(value);
    }

    [GeneratedRegex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.Compiled)]
    private static partial Regex CreateEscapeRegex();
}