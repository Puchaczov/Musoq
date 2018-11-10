using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Musoq.Schema.Os.Dlls
{
    public class DllInfo
    {
        public FileInfo FileInfo { get; set; }

        public Assembly Assembly { get; set; }

        public FileVersionInfo Version { get; set; }
    }
}
