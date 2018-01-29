using System;
using System.Reflection;

namespace FQL.Evaluator.Instructions
{
    public class AccessMethod : ByteCodeInstruction
    {
        private readonly int _argsCount;

        public AccessMethod(int argsCount, MethodInfo method)
        {
            Method = method;
            _argsCount = argsCount;
        }

        protected MethodInfo Method { get; }

        public Type VoidType => typeof(void);

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.ObjectsStack;
            var obj = stack.Pop();
            var args = new object[_argsCount];

            for (var i = _argsCount - 1; i >= 0; --i)
                args[i] = stack.Pop();

            switch (Method.ReturnType.Name)
            {
                case nameof(Boolean):
                    virtualMachine.Current.BooleanStack.Push((bool) Method.Invoke(obj, args));
                    break;
                case nameof(Int16):
                    virtualMachine.Current.LongsStack.Push((short) Method.Invoke(obj, args));
                    break;
                case nameof(Int32):
                    virtualMachine.Current.LongsStack.Push((int) Method.Invoke(obj, args));
                    break;
                case nameof(Int64):
                    virtualMachine.Current.LongsStack.Push((long) Method.Invoke(obj, args));
                    break;
                case nameof(Decimal):
                    virtualMachine.Current.NumericsStack.Push((decimal) Method.Invoke(obj, args));
                    break;
                case nameof(String):
                    virtualMachine.Current.StringsStack.Push((string) Method.Invoke(obj, args));
                    break;
                default:
                    var retObj = Method.Invoke(obj, args);
                    if (Method.ReturnType.Name == VoidType.Name)
                        break;
                    virtualMachine.Current.ObjectsStack.Push(retObj);
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"ACCESS {Method.Name}";
        }
    }
}