using System;
using System.Collections.Generic;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Instructions.Converts;

namespace Musoq.Evaluator.Helpers
{
    public static class EvaluationHelper
    {
        private static readonly IDictionary<(Type, Type), ByteCodeInstruction[]> _convertionsMap;
        static EvaluationHelper()
        {
            _convertionsMap = new Dictionary<(Type, Type), ByteCodeInstruction[]>
            {
                {(typeof(decimal), typeof(decimal)), new ByteCodeInstruction[0]},
                {(typeof(decimal), typeof(long)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(int)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(short)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(decimal)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(long)), new ByteCodeInstruction[0]},
                {(typeof(long), typeof(int)), new ByteCodeInstruction[0]},
                {(typeof(long), typeof(short)), new ByteCodeInstruction[0]},
                {(typeof(int), typeof(decimal)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(int), typeof(long)), new ByteCodeInstruction[0]},
                {(typeof(int), typeof(int)), new ByteCodeInstruction[0]},
                {(typeof(int), typeof(short)), new ByteCodeInstruction[0]},
                {(typeof(short), typeof(decimal)), new ByteCodeInstruction[] {new ConvertToDecimal()}},
                {(typeof(short), typeof(long)), new ByteCodeInstruction[0]},
                {(typeof(short), typeof(int)), new ByteCodeInstruction[0]},
                {(typeof(short), typeof(short)), new ByteCodeInstruction[0]},
                {(typeof(string), typeof(string)), new ByteCodeInstruction[0]},
                {(typeof(bool), typeof(bool)), new ByteCodeInstruction[0]},
                {(typeof(DateTimeOffset), typeof(DateTimeOffset)), new ByteCodeInstruction[0]}
            };
        }

        public static ByteCodeInstruction[] GetConvertingInstructions(Type left, Type right)
        {
            return _convertionsMap[(left, right)];
        }
    }
}
