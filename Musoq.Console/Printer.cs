using System.Data;

namespace Musoq.Console
{
    public abstract class Printer
    {
        protected readonly DataTable Table;

        public Printer(DataTable table)
        {
            Table = table;
        }

        public abstract void Print();
    }
}