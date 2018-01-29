using System;
using System.ServiceProcess;
using FQL.Service.Client;
using FQL.Service.Client.Helpers;
using Microsoft.Owin.Hosting;

namespace FQL.Service
{
    public partial class ContextService : ServiceBase
    {
        private IDisposable _server;

        public ContextService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _server = WebApp.Start<ApiStartup>($"http://{ApplicationConfiguration.ServerAddress}/");
        }

        protected override void OnStop()
        {
            _server.Dispose();
        }

#if DEBUG
        public void Start(string[] args)
        {
            OnStart(args);
            Console.WriteLine("FQL.Server started at {0}.", ApplicationConfiguration.ServerAddress);
            var api = new ApplicationFlowApi(ApplicationConfiguration.ServerAddress);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            OnStop();
            Console.WriteLine("Stopped");
        }
#endif
    }
}