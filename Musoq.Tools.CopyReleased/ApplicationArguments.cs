using CommandLine;

namespace Musoq.Tools.CopyReleased
{
    public class ApplicationArguments
    {
        [Option("source", HelpText = "Source release folder.", Required = true)]
        public string SourceDir { get; set; }

        [Option("destinationDirName", HelpText = "Source destination folder name.", Required = true)]
        public string DestinationDirName { get; set; }

        [Option("configurationDir", HelpText = "Source destination folder name.", Required = true)]
        public string ConfigurationDir { get; set; }

        [Option("buildType", HelpText = "DEBUG / RELEASE", Required = true)]
        public string BuildType { get; set; }
    }
}