namespace Musoq.Converter.Exceptions;

public class CompilationException : Exception
{
    public CompilationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CompilationException(string message)
        : base(message)
    {
    }

    public CompilationException()
    {
    }
}
