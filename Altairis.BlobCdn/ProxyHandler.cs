using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Web;

namespace Altairis.BlobCdn {

    public class ProxyHandler : IHttpHandler {
        private static CloudBlobContainer container;
        private static int expireDays;

        static ProxyHandler() {
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

        public bool IsReusable {
            get {
                return true;
            }
        }

        public void ProcessRequest(HttpContext context) {
            if (context == null) throw new ArgumentNullException("context");

            var fileName = context.Request.RequestContext.RouteData.Values["path"] as string;

            // Handle invalid input
            if (string.IsNullOrWhiteSpace(fileName)) {
                SetErrorContext(context, 400, "Bad Request");
                return;
            }

            // Get blob and send it to user
            var blob = container.GetBlockBlobReference(fileName);
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(expireDays));
            context.Response.ContentType = FileTypeHelper.GetMimeType(System.IO.Path.GetExtension(fileName));
            try {
                blob.DownloadToStream(context.Response.OutputStream);
            }
            catch (StorageException sex) {
                SetErrorContext(context, sex.RequestInformation.HttpStatusCode, sex.RequestInformation.HttpStatusMessage);
            }
        }

        private void SetErrorContext(HttpContext context, int status, string description) {
            if (context == null) throw new ArgumentNullException("context");
            if (status < 100 || status > 999) throw new ArgumentOutOfRangeException("status");
            if (description == null) throw new ArgumentNullException("description");
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "description");

            context.Response.StatusCode = status;
            context.Response.StatusDescription = description;
            context.Response.Output.WriteLine(string.Format(Properties.Resources.ErrorPage, status, description));
        }

    }
}