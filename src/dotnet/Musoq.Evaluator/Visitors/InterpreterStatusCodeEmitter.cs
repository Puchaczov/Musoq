namespace Musoq.Evaluator.Visitors;

internal static class InterpreterStatusCodeEmitter
{
    internal static string BeginBinary(string type) => Begin(type, "byte", "IBytesInterpreterStatus", "InterpretAt");

    internal static string BeginText(string type, string inputName) =>
        $$"""
            public override {{type}} ParseAt(ReadOnlySpan<char> data, int offset)
            {
                var input = new global::Musoq.Schema.Interpreters.TextParseInput(data);
                return ((global::Musoq.Schema.Interpreters.ITextInterpreterStatus<{{type}}>)this)
                    .ReadAt(ref input, offset).GetValueOrThrow();
            }

            global::Musoq.Schema.Interpreters.ParseAttempt<{{type}}>
                global::Musoq.Schema.Interpreters.ITextInterpreterStatus<{{type}}>.ReadAt(
                    ref global::Musoq.Schema.Interpreters.TextParseInput {{inputName}}, int offset)
            {
                var data = {{inputName}}.Text;
                {{type}}? __parsedValue = null;
                try
                {

            """;

    private static string Begin(string type, string element, string protocol, string strictMethod) =>
        $$"""
            public override {{type}} {{strictMethod}}(ReadOnlySpan<{{element}}> data, int offset) =>
                ((global::Musoq.Schema.Interpreters.{{protocol}}<{{type}}>)this).ReadAt(data, offset).GetValueOrThrow();

            global::Musoq.Schema.Interpreters.ParseAttempt<{{type}}>
                global::Musoq.Schema.Interpreters.{{protocol}}<{{type}}>.ReadAt(ReadOnlySpan<{{element}}> data, int offset)
            {
                {{type}}? __parsedValue = null;
                try
                {

            """;

    internal static string End(string type) =>
        $$"""
                    return global::Musoq.Schema.Interpreters.ParseAttempt<{{type}}>.Success(__parsedValue);
                }
                catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<{{type}}>.Failed(__parsedValue,
                        global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
                }
            }

            """;
}
