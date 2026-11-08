using System.IO;

namespace Musoq.Tests.Common;

public static class RepositoryPaths
{
    public static string FindRoot() => FindRoot(AppContext.BaseDirectory);

    public static string FindRoot(string startingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startingDirectory);
        DirectoryInfo? directory = new(Path.GetFullPath(startingDirectory));

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "dotnet", "Musoq.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Musoq repository from '{startingDirectory}'.");
    }
}
