using System;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator.Tables;
using Musoq.Service.Client.Core;

namespace Musoq.Service.Core.Models
{
    public class ExecutionState
    {
        public ExecutionStatus Status { get; set; }
        public Table Result { get; set; }
        public TimeSpan CompilationTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] FailureMessages { get; set; }
        public Task Task { get; set; }

        public bool IsActioned {
            get {
                lock (_isActionedGuard)
                    return _isActionedTimes > 0;
            }
        }

        public bool MakeActionedOrFalse()
        {
            lock (_isActionedGuard)
            {
                if (_isActionedTimes > 0)
                    return false;

                _isActionedTimes += 1;
                return true;
            }
        }

        public void MakeActioned()
        {
            lock (_isActionedGuard)
                _isActionedTimes += 1;
        }

        public void MakeUnactioned()
        {
            lock (_isActionedGuard)
                _isActionedTimes -= 1;
        }

        public string HashedQuery { get; set; }

        private object _isActionedGuard = new object();
        private int _isActionedTimes = 0;
    }
}