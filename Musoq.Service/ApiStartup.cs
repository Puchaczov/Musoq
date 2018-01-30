using System.Net.Http.Formatting;
using System.Web.Http;
using Musoq.Service.Resolvers;
using Owin;

namespace Musoq.Service
{
    public class ApiStartup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration {DependencyResolver = new CustomDependencyResolver()};

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.Routes.MapHttpRoute(
                "ModulesApi",
                "api/{controller}/{action}/{id}/{key}/{status}",
                new
                {
                    id = RouteParameter.Optional,
                    key = RouteParameter.Optional,
                    status = RouteParameter.Optional
                });

            appBuilder.UseWebApi(config);
        }
    }
}