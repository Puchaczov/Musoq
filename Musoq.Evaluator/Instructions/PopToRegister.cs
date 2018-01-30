namespace Musoq.Evaluator.Instructions
{
    public class PopToRegister : ByteCodeInstruction
    {
        private readonly Register _register;

        public PopToRegister(Register register)
        {
            _register = register;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            //TO DO: what if register is IP ?!?!
            virtualMachine.Current.Registers[(int) _register] = virtualMachine.Current.LongsStack.Pop();

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"POP REG {_register}";
        }
    }
}