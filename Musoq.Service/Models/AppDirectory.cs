using FQL.Parser;

namespace Musoq.Service.Models
{
    public class AppDirectory : INamedSource
    {
        public AppDirectory(string path, bool searchSubFolders, string name)
        {
            Path = path;
            SearchSubFolders = searchSubFolders;
            Name = name;
        }

        public string Path { get; }
        public bool SearchSubFolders { get; }
        public string Name { get; }
    }
}