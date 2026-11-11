using System;
using System.IO;
using System.Linq;

namespace Musoq.Tests.Common;

/// <summary>Requires authored input before a source-based guardrail can check violations.</summary>
public static class RequiredSourceInventory
{
    public static string[] Files(string directory, string pattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("Required source directory does not exist: " + directory);

        var files = Directory.GetFiles(directory, pattern, searchOption)
            .Where(file => !Path.GetRelativePath(directory, file)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
            throw new InvalidOperationException("Required authored source inventory is empty: " + directory + "/" + pattern);
        return files;
    }
}
