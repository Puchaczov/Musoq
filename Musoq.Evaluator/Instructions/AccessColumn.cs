using System;
using System.Diagnostics;

namespace Musoq.Evaluator.Instructions
{
    public class AccessColumn : Instruction
    {
        private readonly Type _columnType;
        private readonly string _name;

        public AccessColumn(string name, Type columnType)
        {
            _name = name;
            _columnType = columnType;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();
            var value = source.Current[_name];

            if(!_columnType.IsAssignableFrom(value.GetType()))
                Debugger.Break();

            switch (_columnType.Name)
            {
                case nameof(Boolean):
                    virtualMachine.Current.BooleanStack.Push((bool) value);
                    break;
                case nameof(Int16):
                    virtualMachine.Current.LongsStack.Push((short) value);
                    break;
                case nameof(Int32):
                    virtualMachine.Current.LongsStack.Push((int) value);
                    break;
                case nameof(Int64):
                    virtualMachine.Current.LongsStack.Push((long) value);
                    break;
                case nameof(Decimal):
                    virtualMachine.Current.NumericsStack.Push((decimal) value);
                    break;
                case nameof(String):
                    virtualMachine.Current.StringsStack.Push((string) value);
                    break;
                default:
                    virtualMachine.Current.ObjectsStack.Push(value);
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"ACCESS COLUMN {_name}";
        }
    }
}