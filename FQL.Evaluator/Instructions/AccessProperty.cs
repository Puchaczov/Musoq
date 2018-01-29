using System;
using System.Reflection;

namespace FQL.Evaluator.Instructions
{
    public abstract class AcessObject : ByteCodeInstruction
    {
        protected AcessObject(Type returnType)
        {
            ReturnType = returnType;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var obj = virtualMachine.Current.ObjectsStack.Pop();

            switch (ReturnType.Name)
            {
                case nameof(Boolean):
                    virtualMachine.Current.BooleanStack.Push((bool)GetValue(obj));
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    virtualMachine.Current.LongsStack.Push((long)GetValue(obj));
                    break;
                case nameof(Decimal):
                    virtualMachine.Current.NumericsStack.Push((decimal)GetValue(obj));
                    break;
                case nameof(String):
                    virtualMachine.Current.StringsStack.Push((string)GetValue(obj));
                    break;
                default:
                    virtualMachine.Current.ObjectsStack.Push(GetValue(obj));
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }

        protected abstract object GetValue(object obj);

        protected Type ReturnType { get; }
    }

    public class AccessProperty : AcessObject
    {
        public AccessProperty(PropertyInfo property, object arg)
            : base(property.PropertyType)
        {
            Property = property;
            Arg = arg;
        }

        protected PropertyInfo Property { get; }

        private object Arg { get; }

        public override string DebugInfo()
        {
            return $"ACESS {Property.Name}";
        }

        protected override object GetValue(object obj)
        {
            if(Arg != null)
                return Property.GetValue(obj, new []{ Arg });

            return Property.GetValue(obj);
        }
    }
}