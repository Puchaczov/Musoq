using System.Linq;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator;

public class Operators
{
    public bool Like(string content, string searchFor)
    {
        return
            new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\")
                .Replace(searchFor, ch => @"\" + ch).Replace('_', '.')
                .Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(content);
    }

    public bool RLike(string content, string pattern)
    {
        return new Regex(pattern).IsMatch(content);
    }

    public bool Contains<T>(T value, T[] values)
    {
        return values.Contains(value);
    }
}