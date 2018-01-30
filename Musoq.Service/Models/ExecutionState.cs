using System;
using System.Threading.Tasks;
using FQL.Evaluator.Tables;
using FQL.Service.Client;

namespace Musoq.Service.Models
{
    public class ExecutionState
    {
        public ExecutionStatus Status { get; set; }
        public Table Result { get; set; }
        public TimeSpan CompilationTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string FailureMessage { get; set; }

        public Task Task { get; set; }
    }
}