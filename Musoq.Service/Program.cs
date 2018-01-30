namespace Musoq.Service
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
#if DEBUG
            var server = new ContextService();
            server.Start(args);
#else
            var ServicesToRun = new ServiceBase[]
            {
                new ContextService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}