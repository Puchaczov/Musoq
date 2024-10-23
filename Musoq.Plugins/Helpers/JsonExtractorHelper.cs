using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Musoq.Plugins.Helpers;

internal class JsonExtractorHelper
{
    public static string[] ExtractFromJson(string json, string path)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            var pathParts = ParseJsonPath(path);
            var results = new List<string>();

            ProcessElement(jsonElement, new Stack<string>(pathParts.Reverse()), results);

            return results.ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string[] ParseJsonPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return [];

        var parts = new List<string>();
        var currentPos = 0;

        if (path.StartsWith('$'))
            currentPos = 1;

        while (currentPos < path.Length)
        {
            if (path[currentPos] == '.')
            {
                currentPos++;
                continue;
            }

            if (path[currentPos] == '[')
            {
                currentPos++;

                var closeBracketPos = FindMatchingCloseBracket(path, currentPos);
                if (closeBracketPos == -1)
                    break;

                var bracketContent = path[currentPos..closeBracketPos];

                if ((bracketContent.StartsWith('\'') && bracketContent.EndsWith('\'')) ||
                    (bracketContent.StartsWith('"') && bracketContent.EndsWith('"')))
                {
                    parts.Add(bracketContent[1..^1]);
                }
                else if (bracketContent == "*")
                {
                    parts.Add("*");
                }
                else
                {
                    parts.Add(bracketContent);
                }

                currentPos = closeBracketPos + 1;
            }
            else
            {
                var nextDot = path.IndexOf('.', currentPos);
                var nextBracket = path.IndexOf('[', currentPos);

                var endPos = nextDot switch
                {
                    -1 when nextBracket == -1 => path.Length,
                    -1 => nextBracket,
                    _ => nextBracket == -1 ? nextDot : Math.Min(nextDot, nextBracket)
                };

                var part = path[currentPos..endPos];
                if (!string.IsNullOrEmpty(part))
                {
                    parts.Add(part);
                }

                currentPos = endPos;
            }
        }

        return parts.ToArray();
    }

    private static int FindMatchingCloseBracket(string path, int startPos)
    {
        var inQuotes = false;
        var quoteChar = '\0';

        for (var i = startPos; i < path.Length; i++)
        {
            var c = path[i];

            switch (c)
            {
                case '\'' or '"' when (i == startPos || path[i - 1] != '\\'):
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                    else if (c == quoteChar)
                    {
                        inQuotes = false;
                    }

                    break;
                }
                case ']' when !inQuotes:
                    return i;
            }
        }

        return -1;
    }

    private static void ProcessElement(JsonElement element, Stack<string> pathParts, List<string> results)
    {
        if (pathParts.Count == 0)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    results.Add(element.GetString() ?? string.Empty);
                    break;
                case JsonValueKind.True:
                    results.Add("true");
                    break;
                case JsonValueKind.False:
                    results.Add("false");
                    break;
                case JsonValueKind.Number:
                    results.Add(element.ToString());
                    break;
                case JsonValueKind.Null:
                    break;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false
                    };
                    results.Add(JsonSerializer.Serialize(element, options));
                    break;
                default:
                    results.Add(element.ToString());
                    break;
            }

            return;
        }

        var currentPath = pathParts.Pop();

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                ProcessArray(element, pathParts, results, currentPath);
                break;

            case JsonValueKind.Object:
                ProcessObject(element, pathParts, results, currentPath);
                break;
        }

        pathParts.Push(currentPath);
    }

    private static void ProcessArray(JsonElement element, Stack<string> pathParts, List<string> results,
        string currentPath)
    {
        if (currentPath == "*")
        {
            foreach (var item in element.EnumerateArray())
            {
                ProcessElement(item, new Stack<string>(pathParts.Reverse()), results);
            }
        }
        else if (int.TryParse(currentPath, out var index) && index < element.GetArrayLength())
        {
            var item = element.EnumerateArray().ElementAt(index);
            ProcessElement(item, pathParts, results);
        }
    }

    private static void ProcessObject(JsonElement element, Stack<string> pathParts, List<string> results,
        string currentPath)
    {
        if ((currentPath.StartsWith('\'') && currentPath.EndsWith('\'')) ||
            (currentPath.StartsWith('"') && currentPath.EndsWith('"')))
        {
            currentPath = currentPath[1..^1];
        }

        if (currentPath == "*")
        {
            foreach (var property in element.EnumerateObject())
            {
                ProcessElement(property.Value, new Stack<string>(pathParts.Reverse()), results);
            }
        }
        else if (element.TryGetProperty(currentPath, out var child))
        {
            ProcessElement(child, pathParts, results);
        }
    }
}