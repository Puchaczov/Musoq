using System.Text.Json;

namespace Musoq.Benchmarks;

public static class BundleFiles
{
    public static string Resolve(string root, string relative)
    {
        var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(directory, relative));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (Path.IsPathRooted(relative) || !path.StartsWith(directory, comparison))
            throw new InvalidDataException($"Artifact path escapes its bundle: {relative}");
        return path;
    }

    public static void Validate(string root, IEnumerable<string> relativePaths)
    {
        foreach (var relative in relativePaths)
        {
            var path = Resolve(root, relative);
            if (!File.Exists(path))
                throw new InvalidDataException($"Missing artifact: {relative}");
            try
            {
                using var stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Damaged JSON artifact: {relative}", exception);
            }
        }
    }
}
