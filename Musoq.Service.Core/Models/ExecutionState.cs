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

        public bool IsActioned
        {
            get
            {
                return _isActionedTimes > 0;
            }
        }

        public bool MakeActioned()
        {
            if (_isActionedTimes > 0)
                return false;

            _isActionedTimes += 1;
            return true;
        }

        public void MakeUnactioned()
        {
            _isActionedTimes -= 1;
        }

        public string HashedQuery { get; set; }
        private int _isActionedTimes = 0;
    }
}