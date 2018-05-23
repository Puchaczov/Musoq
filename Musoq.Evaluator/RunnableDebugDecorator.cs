using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator
{
    public class RunnableDebugDecorator : IRunnable
    {
        private readonly IRunnable _runnable;
        private readonly string[] _filesToDelete;

        public RunnableDebugDecorator(IRunnable runnable, params string[] filesToDelete)
        {
            _runnable = runnable;
            _filesToDelete = filesToDelete;
        }

        public ISchemaProvider Provider
        {
            get => _runnable.Provider;
            set => _runnable.Provider = value;
        }

        public Table Run()
        {
            var table = _runnable.Run();

            foreach (var path in _filesToDelete)
            {
                var file = new FileInfo(path);

                if (file.Exists)
                    file.Delete();
            }

            return table;
        }
    }
}
