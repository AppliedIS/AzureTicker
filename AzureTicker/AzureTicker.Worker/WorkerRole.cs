using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Web.Http.SelfHost;
using System.Web.Http;


namespace AzureTicker.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("AzureTicker.Worker entry point called", "Information");

            HttpSelfHostServer server = null;            
            try
            {
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["webapi"];
                Uri baseAddress = new Uri(string.Format("{0}://{1}", endpoint.Protocol, endpoint.IPEndpoint.ToString()));                

                // Set up server configuration 
                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(baseAddress);
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );

                // Create server 
                server = new HttpSelfHostServer(config);

                // Start listening 
                server.OpenAsync().Wait();
            }
            catch (Exception e)
            {
                Trace.WriteLine("Failed to open Web Api host.", "Error");
                Trace.WriteLine(e.ToString(), "Error");
                if (server != null)
                {                    
                    server.Dispose();
                }
            }

            while (true)
            {
                Thread.Sleep(10000);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
