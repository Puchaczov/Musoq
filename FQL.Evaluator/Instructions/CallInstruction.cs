using System;
using System.Reflection;

namespace FQL.Evaluator.Instructions
{
    public abstract class CallInstruction<T> : ByteCodeInstruction
    {
        protected CallInstruction(int argsCount, MethodInfo methodInfo)
        {
            ArgsCount = argsCount;
            MethodInfo = methodInfo;
        }

        protected int ArgsCount { get; }
        protected MethodInfo MethodInfo { get; }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.ObjectsStack;

            var obj = virtualMachine.Current.ObjectsStack.Pop();
            var args = new object[ArgsCount];
            for (var i = 0; i < ArgsCount; ++i)
                args[i] = stack.Pop();

            switch (MethodInfo.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    virtualMachine.Current.LongsStack.Push((long) MethodInfo.Invoke(obj, args));
                    break;
                case nameof(Decimal):
                    virtualMachine.Current.NumericsStack.Push((decimal) MethodInfo.Invoke(obj, args));
                    break;
                case nameof(String):
                    virtualMachine.Current.StringsStack.Push((string) MethodInfo.Invoke(obj, args));
                    break;
                default:
                    virtualMachine.Current.ObjectsStack.Push(MethodInfo.Invoke(obj, args));
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }
    }
}