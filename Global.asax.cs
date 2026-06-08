using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using rag_can_aspx.Services.Jobs;

namespace rag_can_aspx
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Código que se ejecuta al iniciar la aplicación
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Infraestructura de jobs (NIVEL 1):
            JobStatusManager.EnsureFolders();      // crea App_Data/status y App_Data/logs
            JobStatusManager.ReconcileOnStartup(); // repara estados "running" huerfanos tras un reciclaje
            Scheduler.Start();                     // arranca el scheduler in-process
        }

        void Application_End(object sender, EventArgs e)
        {
            Scheduler.Stop();
        }
    }
}