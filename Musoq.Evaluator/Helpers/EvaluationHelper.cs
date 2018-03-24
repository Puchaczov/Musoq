using System;
using System.Collections.Generic;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Instructions.Converts;

namespace Musoq.Evaluator.Helpers
{
    public static class EvaluationHelper
    {
        private static readonly IDictionary<(Type, Type), Instruction[]> _convertionsMap;
        static EvaluationHelper()
        {
            _convertionsMap = new Dictionary<(Type, Type), Instruction[]>
            {
                {(typeof(decimal), typeof(decimal)), new Instruction[0]},
                {(typeof(decimal), typeof(long)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(int)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(short)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(long)), new Instruction[0]},
                {(typeof(long), typeof(int)), new Instruction[0]},
                {(typeof(long), typeof(short)), new Instruction[0]},
                {(typeof(int), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(int), typeof(long)), new Instruction[0]},
                {(typeof(int), typeof(int)), new Instruction[0]},
                {(typeof(int), typeof(short)), new Instruction[0]},
                {(typeof(short), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(short), typeof(long)), new Instruction[0]},
                {(typeof(short), typeof(int)), new Instruction[0]},
                {(typeof(short), typeof(short)), new Instruction[0]},
                {(typeof(string), typeof(string)), new Instruction[0]},
                {(typeof(bool), typeof(bool)), new Instruction[0]},
                {(typeof(DateTimeOffset), typeof(DateTimeOffset)), new Instruction[0]}
            };
        }

        public static Instruction[] GetConvertingInstructions(Type left, Type right)
        {
            return _convertionsMap[(left, right)];
        }
    }
}
