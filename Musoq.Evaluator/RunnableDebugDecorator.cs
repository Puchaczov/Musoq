using System.IO;
using System.Threading;
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

        public Table Run(CancellationToken token)
        {
            var table = _runnable.Run(token);

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
