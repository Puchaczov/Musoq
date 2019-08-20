using System.IO;

namespace Musoq.Schema.Os.Compare.Directories
{
    public class CompareDirectoriesResult
    {
        public CompareDirectoriesResult(
            DirectoryInfo sourceRoot,
            FileInfo sourceFile, 
            DirectoryInfo destinationRoot,
            FileInfo destinationFile, 
            State state)
        {
            SourceRoot = sourceRoot;
            SourceFile = sourceFile;
            DestinationRoot = destinationRoot;
            DestinationFile = destinationFile;
            State = state;
        }

        public DirectoryInfo SourceRoot { get; }

        public FileInfo SourceFile { get; }

        public string SourceFileRelative => SourceFile?.FullName?.Replace(SourceRoot.FullName, string.Empty);

        public DirectoryInfo DestinationRoot { get; }

        public FileInfo DestinationFile { get; }

        public string DestinationFileRelative => DestinationFile?.FullName?.Replace(DestinationRoot.FullName, string.Empty);

        public State State { get; }
    }
}
