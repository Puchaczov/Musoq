using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public class CompiledQuery
    {
        private readonly IRunnable _runnable;

        public CompiledQuery(IRunnable runnable)
        {
            _runnable = runnable;
        }

        public Table Run()
        {
             return Run(CancellationToken.None);
        }

        public Table Run(CancellationToken token)
        {
            return _runnable.Run(token);
        }
    }
}
