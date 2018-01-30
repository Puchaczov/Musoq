using CommandLine;

namespace Musoq.Console
{
    public class ApplicationArguments
    {
        [Option('q', HelpText = "Put the query here!")]
        public string Query { get; set; }

        [Option("addr", HelpText = "Set different address for particular query", Required = false)]
        public string Address { get; set; }

        [Option("qs", HelpText = "Use the query from file.", Required = false)]
        public string QuerySourceFile { get; set; }

        [Option("sd", HelpText = "Save response to file")]
        public string QueryScoreFile { get; set; }
    }
}