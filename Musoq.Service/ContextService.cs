using System;
using System.IO;
using System.ServiceProcess;
using Microsoft.Owin.Hosting;
using Musoq.Service.Environment;

namespace Musoq.Service
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
            _server = WebApp.Start<ApiStartup>(ApplicationConfiguration.HttpServerAdress);
        }

        protected override void OnStop()
        {
            _server.Dispose();
        }

#if DEBUG
        public void Start(string[] args)
        {
            OnStart(args);
            Console.WriteLine("{1} started at {0}.", ApplicationConfiguration.ServerAddress, nameof(Musoq));

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            OnStop();
            Console.WriteLine("Stopped");
        }
#endif
    }
}