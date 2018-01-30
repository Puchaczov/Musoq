using System;

namespace Musoq.Evaluator.Instructions
{
    public class GrabRow : ByteCodeInstruction
    {
        private readonly Type[] _columnsTypes;

        public GrabRow(Type[] columnsTypes)
        {
            _columnsTypes = columnsTypes;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var columns = new object[_columnsTypes.Length];

            for (var i = _columnsTypes.Length - 1; i >= 0; --i)
                switch (_columnsTypes[i].Name)
                {
                    case nameof(Boolean):
                        columns[i] = virtualMachine.Current.BooleanStack.Pop();
                        break;
                    case nameof(Int16):
                        columns[i] = (short) virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Int32):
                        columns[i] = (int) virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Int64):
                        columns[i] = virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Decimal):
                        columns[i] = virtualMachine.Current.NumericsStack.Pop();
                        break;
                    case nameof(String):
                        columns[i] = virtualMachine.Current.StringsStack.Pop();
                        break;
                    default:
                        columns[i] = virtualMachine.Current.ObjectsStack.Pop();
                        break;
                }


            virtualMachine.Current.ObjectsStack.Push(columns);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "GRAB ROW";
        }
    }
}