using System;

namespace FQL.Service.Client
{
    public class ResultTable
    {
        public ResultTable(string name, string[] columns, object[][] rows, TimeSpan computationTime)
        {
            Name = name;
            Columns = columns;
            Rows = rows;
            ComputationTime = computationTime;
        }

        public string[] Columns { get; }

        public object[][] Rows { get; }

        public string Name { get; }

        public TimeSpan ComputationTime { get; }
    }
}