using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;

namespace Altairis.BlobCdn.DemoSite {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            // Add route for handling CDNs
            RouteTable.Routes.Add(new Route("cdn/{*path}", new RouteHandlerProxy<ProxyHandlerAsync>()));
        }

    }
}