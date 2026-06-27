namespace Musoq.Schema.Exceptions;

public class TableNotFoundException : Exception
{
    public TableNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TableNotFoundException(string message)
        : base(message)
    {
    }

    public TableNotFoundException()
    {
    }
}
