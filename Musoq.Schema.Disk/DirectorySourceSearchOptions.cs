namespace Musoq.Schema.Disk
{
    public class DirectorySourceSearchOptions
    {
        public DirectorySourceSearchOptions(string path, bool useSubDirectories)
        {
            Path = path;
            WithSubDirectories = useSubDirectories;
        }

        public string Path { get; }

        public bool WithSubDirectories { get; }
    }
}