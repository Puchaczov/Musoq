using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Musoq.Parser;

internal static class TextPatternValidator
{
    public static bool TryValidate(
        string pattern,
        IReadOnlyList<string> captureGroups,
        out string error)
    {
        try
        {
            var validationPattern = pattern.StartsWith(@"\G", StringComparison.Ordinal)
                ? pattern[2..]
                : pattern;
            var regex = new Regex(
                validationPattern,
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
            var availableGroupSet = new HashSet<string>(regex.GetGroupNames(), StringComparer.Ordinal);
            var requestedGroups = new HashSet<string>(StringComparer.Ordinal);

            foreach (var captureGroup in captureGroups)
            {
                if (!requestedGroups.Add(captureGroup))
                {
                    error = $"capture group '{captureGroup}' is listed more than once";
                    return false;
                }

                if (!availableGroupSet.Contains(captureGroup))
                {
                    error = $"capture group '{captureGroup}' is not defined by the pattern";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
        catch (ArgumentException exception)
        {
            error = $"invalid regular expression ({exception.Message})";
            return false;
        }
        catch (NotSupportedException exception)
        {
            error = $"unsupported regular expression construct ({exception.Message})";
            return false;
        }
    }
}
