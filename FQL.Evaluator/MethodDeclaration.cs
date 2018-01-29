using System;
using System.Linq;
using System.Reflection;

namespace FQL.Evaluator
{
    public class MethodDeclaration
    {
        public MethodDeclaration(MethodInfo methodInfo)
        {
            Method = methodInfo;
        }

        public string Name => Method.Name;

        private MethodInfo Method { get; }

        public Type[] Arguments => Method.GetParameters().Select(f => f.ParameterType).ToArray();
        public Type Return => Method.ReturnParameter.ParameterType;
    }
}