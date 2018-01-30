using System;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Instructions
{
    public class PrepareMethodCall : ByteCodeInstruction
    {
        private readonly MethodInfo _method;
        private readonly object _methodsAggregator;
        private readonly ParameterInfo[] _parametersToLoad;
        private readonly InjectTypeAttribute[] _toInjectAttributes;

        public PrepareMethodCall(MethodInfo method, object methodsAggregator)
        {
            _method = method;
            var parameters = method.GetParameters();
            var parametersToInject = parameters.GetParametersWithAttribute<InjectTypeAttribute>();
            _toInjectAttributes =
                parametersToInject.Select(f => f.GetCustomAttribute<InjectTypeAttribute>()).ToArray();
            _parametersToLoad = parameters.GetParametersWithoutAttribute<InjectTypeAttribute>().ToArray();
            _methodsAggregator = methodsAggregator;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.ObjectsStack;
            var source = virtualMachine.Current.SourceStack.Peek();
            foreach (var attribute in _toInjectAttributes)
                switch (attribute.GetType().Name)
                {
                    case nameof(InjectSourceAttribute):
                        stack.Push(source.Current.Context);
                        break;
                    case nameof(InjectGroupAttribute):
                        stack.Push(virtualMachine.Current.CurrentGroup);
                        break;
                    case nameof(InjectGroupAccessName):
                        stack.Push(virtualMachine.Current.StringsStack.Pop());
                        break;
                }

            var parameters = new object[_parametersToLoad.Length];

            for (int i = 0, j = _parametersToLoad.Length - 1; i < _parametersToLoad.Length; i++, --j)
            {
                var item = _parametersToLoad[j];

                switch (item.ParameterType.Name)
                {
                    case nameof(Int16):
                    case nameof(Int32):
                    case nameof(Int64):
                        parameters[j] = virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Decimal):
                        parameters[j] = virtualMachine.Current.NumericsStack.Pop();
                        break;
                    case nameof(String):
                        parameters[j] = virtualMachine.Current.StringsStack.Pop();
                        break;
                    default:
                        parameters[j] = virtualMachine.Current.ObjectsStack.Pop();
                        break;
                }
            }

            for (var i = 0; i < parameters.Length; i++) stack.Push(parameters[i]);

            stack.Push(_methodsAggregator);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"PREPARE CALL FOR {_method.Name}";
        }
    }
}