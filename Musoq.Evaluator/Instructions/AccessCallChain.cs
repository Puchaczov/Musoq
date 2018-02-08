using System;
using System.Reflection;

namespace Musoq.Evaluator.Instructions
{
    public class AccessCallChain : ByteCodeInstruction
    {
        private readonly (PropertyInfo Property, object Arg)[] _props;

        public AccessCallChain((PropertyInfo Property, object Arg)[] props)
        {
            _props = props;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var obj = virtualMachine.Current.ObjectsStack.Pop();

            for (int i = 0; i < _props.Length; i++)
            {
                var arg = _props[i].Arg;

                switch (arg)
                {
                    case int index:
                        obj = _props[i].Property.GetValue(obj);
                        obj = ((Array) obj).GetValue(index);
                        break;
                    case string key:
                        obj = _props[i].Property.GetValue(obj, new object[] { key });
                        break;
                }
            }

            switch (obj.GetType().Name)
            {
                case nameof(Boolean):
                    virtualMachine.Current.BooleanStack.Push((bool)obj);
                    break;
                case nameof(Int16):
                    virtualMachine.Current.LongsStack.Push((short)obj);
                    break;
                case nameof(Int32):
                    virtualMachine.Current.LongsStack.Push((int)obj);
                    break;
                case nameof(Int64):
                    virtualMachine.Current.LongsStack.Push((long)obj);
                    break;
                case nameof(Decimal):
                    virtualMachine.Current.NumericsStack.Push((decimal)obj);
                    break;
                case nameof(String):
                    virtualMachine.Current.StringsStack.Push((string)obj);
                    break;
                default:
                    virtualMachine.Current.ObjectsStack.Push(obj);
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "ACCESS CALL CHAIN";
        }
    }
}
