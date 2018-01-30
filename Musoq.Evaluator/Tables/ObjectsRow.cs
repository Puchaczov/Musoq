namespace Musoq.Evaluator.Tables
{
    public class ObjectsRow : Row
    {
        private readonly object[] _columns;

        public ObjectsRow(object[] columns)
        {
            _columns = columns;
        }

        public override object this[int columnNumber] => _columns[columnNumber];

        public override int Count => _columns.Length;

        public override object[] Values => _columns;
    }
}