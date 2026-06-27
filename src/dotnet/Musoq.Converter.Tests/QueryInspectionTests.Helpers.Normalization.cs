using System;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string Normalize(string text)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Trim('\n')
            .Split('\n');

        var indentation = GetCommonIndentation(lines);

        if (indentation == 0)
            return string.Join("\n", lines).Trim();

        for (var index = 0; index < lines.Length; index++)
            if (lines[index].Length >= indentation)
                lines[index] = lines[index][indentation..];

        return string.Join("\n", lines).Trim();
    }

    private static int GetCommonIndentation(string[] lines)
    {
        var indentation = int.MaxValue;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var spaces = 0;
            while (spaces < line.Length && line[spaces] == ' ')
                spaces++;

            indentation = Math.Min(indentation, spaces);
        }

        return indentation == int.MaxValue ? 0 : indentation;
    }
}
