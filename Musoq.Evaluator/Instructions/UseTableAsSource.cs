using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public abstract class UseTableAsSource : Instruction
    {
        private readonly string _name;

        protected UseTableAsSource(string name)
        {
            _name = name;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = virtualMachine.Current.Tables[_name];
            var tableSource = CreateSource(table);
            virtualMachine.Current.SourceStack.Push(tableSource.Rows.GetEnumerator());
            virtualMachine[Register.Ip] += 1;
        }

        protected abstract TableRowSource CreateSource(Table table);

        public override string DebugInfo()
        {
            return $"USE TABLE {_name} AS SOURCE";
        }
    }
}