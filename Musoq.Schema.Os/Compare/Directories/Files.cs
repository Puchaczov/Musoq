using Musoq.Schema.Os.Files;
using System.Collections;
using System.Collections.Generic;

namespace Musoq.Schema.Os.Compare.Directories
{
    public class Files : IReadOnlyList<ExtendedFileInfo>
    {
        private readonly IReadOnlyList<ExtendedFileInfo> _files;

        public Files(IReadOnlyList<ExtendedFileInfo> files)
        {
            _files = files;
        }

        public ExtendedFileInfo Source => _files[0];

        public ExtendedFileInfo Destination => _files[1];

        public ExtendedFileInfo this[int index] => _files[index];

        public int Count => _files.Count;

        public IEnumerator<ExtendedFileInfo> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
