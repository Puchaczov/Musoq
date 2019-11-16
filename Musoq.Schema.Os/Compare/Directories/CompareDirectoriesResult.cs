using Musoq.Schema.Os.Files;
using System.IO;

namespace Musoq.Schema.Os.Compare.Directories
{
    public class CompareDirectoriesResult
    {
        public CompareDirectoriesResult(
            DirectoryInfo sourceRoot,
            ExtendedFileInfo sourceFile, 
            DirectoryInfo destinationRoot,
            ExtendedFileInfo destinationFile, 
            State state)
        {
            SourceRoot = sourceRoot;
            SourceFile = sourceFile;
            DestinationRoot = destinationRoot;
            DestinationFile = destinationFile;
            State = state;
        }

        public DirectoryInfo SourceRoot { get; }

        public ExtendedFileInfo SourceFile { get; }

        public string SourceFileRelative => SourceFile?.FullName?.Replace(SourceRoot.FullName, string.Empty);

        public DirectoryInfo DestinationRoot { get; }

        public ExtendedFileInfo DestinationFile { get; }

        public string DestinationFileRelative => DestinationFile?.FullName?.Replace(DestinationRoot.FullName, string.Empty);

        public State State { get; }
    }
}
