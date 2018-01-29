using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FQL.Evaluator.Instructions
{
    public class AccessCallChain : ByteCodeInstruction
    {
        private (PropertyInfo Property, object Arg)[] props;

        public AccessCallChain((PropertyInfo Property, object Arg)[] props)
        {
            this.props = props;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var obj = virtualMachine.Current.ObjectsStack.Pop();

            for (int i = 0; i < props.Length; i++)
            {
                obj = props[i].Property.GetValue(obj);

                var arg = props[i].Arg;
                if (arg is int index)
                {
                    obj = ((Array)obj).GetValue(index);
                }
                else if (arg is string key)
                {
                    obj = ((IDictionary) obj)[key];
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
