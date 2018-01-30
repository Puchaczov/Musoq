using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public class AddNewRow : ByteCodeInstruction
    {
        private readonly string _name;

        public AddNewRow(string name)
        {
            _name = name;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = virtualMachine.Current.Tables[_name];
            var columnValues = (object[]) virtualMachine.Current.ObjectsStack.Pop();
            table.Add(new ObjectsRow(columnValues));
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"ADD ROW {_name}";
        }
    }
}