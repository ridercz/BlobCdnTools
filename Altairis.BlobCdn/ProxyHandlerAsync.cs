using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Altairis.BlobCdn {
    public class ProxyHandlerAsync : HttpTaskAsyncHandler {
        private static CloudBlobContainer container;
        private static int expireDays;

        static ProxyHandlerAsync() {
            var cfg = System.Configuration.ConfigurationManager.GetSection("altairis.blobCdn") as Altairis.BlobCdn.Configuration.BlobCdnSection;

            // Create account context
            var account = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.ConnectionStrings[cfg.Storage.ConnectionStringName].ConnectionString);
            var blobClient = account.CreateCloudBlobClient();

            // Get or create container
            container = blobClient.GetContainerReference(cfg.Storage.ContainerName);
            container.CreateIfNotExists();

            // Get expiration
            expireDays = cfg.Caching.ExpireDays;
        }

        public override Task ProcessRequestAsync(HttpContext context) {
            var fileName = context.Request.RequestContext.RouteData.Values["path"] as string;

            // Handle invalid input
            if (string.IsNullOrWhiteSpace(fileName)) {
                context.Response.StatusCode = 404;
                context.Response.StatusDescription = "Object Not Found";
                return context.Response.Output.WriteLineAsync("<h1>HTTP Error 404</h1> Object Not Found");
            }

            // Get blob and send it to user
            var blob = container.GetBlockBlobReference(fileName);
            context.Response.Headers.Add("X-Powered-By", "Altairis BlobCDN");
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(expireDays));
            context.Response.ContentType = FileTypeHelper.GetMimeType(System.IO.Path.GetExtension(fileName));
            return blob.DownloadToStreamAsync(context.Response.OutputStream);
        }

    }
}
