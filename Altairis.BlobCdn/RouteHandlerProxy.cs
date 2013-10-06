using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;

namespace Altairis.BlobCdn {
    public class RouteHandlerProxy<T> : IRouteHandler where T : IHttpHandler, new() {
        public IHttpHandler GetHttpHandler(RequestContext requestContext) {
            return new T();
        }
    }
}
