using System;

namespace Musoq.Evaluator.Exceptions
{
    public class FieldLinkIndexOutOfRangeException : Exception
    {
        public FieldLinkIndexOutOfRangeException(int index, int groups)
            : base($"There is no group selected by '{index}' value. Max allowed group index for this query is {groups}")
        {
        }
    }
}
