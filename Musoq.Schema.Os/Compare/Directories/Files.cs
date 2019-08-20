using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Musoq.Schema.Os.Compare.Directories
{
    public class Files : IReadOnlyList<FileInfo>
    {
        private readonly IReadOnlyList<FileInfo> _files;

        public Files(IReadOnlyList<FileInfo> files)
        {
            _files = files;
        }

        public FileInfo Source => _files[0];

        public FileInfo Destination => _files[1];

        public FileInfo this[int index] => _files[index];

        public int Count => _files.Count;

        public IEnumerator<FileInfo> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
