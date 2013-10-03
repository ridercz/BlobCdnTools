using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NConsoler;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Altairis.BlobCdn.Uploader {
    class Program {
        private const int ERRORLEVEL_SUCCESS = 0;
        private const int ERRORLEVEL_FAILURE = 1;
        private const int SECONDS_PER_DAY = 86400;
        private const int MEGABYTE = 1048576;                   // 1 MB
        private const int FILE_SIZE_THRESHOLD = 32 * MEGABYTE;  // 32 MB
        private const int BLOCK_SIZE = 4 * MEGABYTE;            // 4 MB

        private static List<string> fileList = new List<string>();

        static void Main(string[] args) {
            Console.WriteLine("Altairis BlobCDN Uploader version {0:4}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Copyrioght (c) Michal A. Valášek - Altairis, 2013");
            Console.WriteLine("www.altairis.cz | github.com/ridercz/BlobCdnTools");
            Console.WriteLine();
            Consolery.Run();
        }

        [Action("Upload directory content to Azure blob storage")]
        public static void Upload(
            [Required(Description = "Name of folder to upload")]                string folderName,
            [Required(Description = "Storage account name")]                    string accountName,
            [Required(Description = "Primary or secondary access key")]         string accessKey,
            [Optional("cdn", "c", Description = "Container name")]              string containerName,
            [Optional(30, "md", Description = "Value of max-age in days")]      int maxAge,
            [Optional(false, "x", Description = "Overwrite existing files")]    bool overwrite,
            [Optional(false, Description = "Show verbose error messages")]      bool debug) {

            try {
                // Add trailing slash
                if (!folderName.EndsWith("\\")) folderName += "\\";

                Console.Write("Creating Azure Storage client...");
                var cred = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accessKey);
                var account = new CloudStorageAccount(cred, true);
                var blobClient = account.CreateCloudBlobClient();
                Console.WriteLine("OK");

                Console.Write("Checking for existence of container \"{0}\"...", containerName);
                var container = blobClient.GetContainerReference(containerName);
                var created = container.CreateIfNotExists();
                if (created) Console.WriteLine("OK, created");
                else Console.WriteLine("OK, existed");

                Console.Write("Getting list of files to process...");
                GetFileNames(folderName);
                Console.WriteLine("OK, {0} items in queue", fileList.Count);

                Console.WriteLine("Processing queue...");
                foreach (var fileName in fileList) {
                    // Get blob name from file name
                    var blobName = fileName.Remove(0, folderName.Length).Replace('\\', '/');
                    Console.Write("{0}...", blobName);

                    // Get blob
                    var blob = container.GetBlockBlobReference(blobName);

                    // Check if exists when not overwriting
                    if (!overwrite && blob.Exists()) {
                        Console.WriteLine("Exists, skipping...");
                        continue;
                    }

                    // Set blob content type
                    blob.Properties.ContentType = FileTypeHelper.GetMimeType(Path.GetExtension(fileName));

                    // Set blob expiration (using max-age, because is dynamic)
                    if (maxAge > 0) blob.Properties.CacheControl = "public, max-age=" + (maxAge * SECONDS_PER_DAY);
                    
                    // Upload blob
                    var fi = new FileInfo(fileName);
                    UploadFileToBlob(fi, blob);
                }

            }
            catch (Exception ex) {
                Console.WriteLine("Failed!");
                Console.WriteLine(ex.Message);
                if (debug) Console.WriteLine(ex.ToString());
                Environment.Exit(ERRORLEVEL_FAILURE);
            }

            Environment.Exit(ERRORLEVEL_SUCCESS);
        }

        private static void GetFileNames(string folder) {
            if (folder == null) throw new ArgumentNullException("folder");
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "folder");

            var fileNames = Directory.GetFiles(folder);
            fileList.AddRange(fileNames);

            var folderNames = Directory.GetDirectories(folder);
            foreach (var folderName in folderNames) {
                GetFileNames(folderName);
            }
        }

        private static void UploadFileToBlob(FileInfo fileInfo, CloudBlockBlob blob) {
            Console.Write("{0:N2} MB of {1}", (float)fileInfo.Length / MEGABYTE, blob.Properties.ContentType);
            if (fileInfo.Length <= FILE_SIZE_THRESHOLD) {
                // Blob is smaller than limit - single step upload
                Console.Write("...");
                using (var fs = fileInfo.OpenRead()) {
                    blob.UploadFromStream(fs);
                }
                Console.WriteLine("OK");
            }
            else {
                // Blob is too large - upload block by block
                Console.WriteLine(":");
                UploadBlockBlob(fileInfo, blob);
            }
        }

        private static void UploadBlockBlob(FileInfo fileInfo, CloudBlockBlob blob) {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");
            if (blob == null) throw new ArgumentNullException("blob");

            var x = Console.CursorLeft;
            var y = Console.CursorTop;

            var blockCount = Math.Ceiling(((float)fileInfo.Length / BLOCK_SIZE));
            var blockIds = new List<string>();
            using (var file = fileInfo.OpenRead()) {
                var currentBlockId = 0;
                while (file.Position < file.Length) {
                    var bufferSize = BLOCK_SIZE < file.Length - file.Position ? BLOCK_SIZE : file.Length - file.Position;
                    var buffer = new byte[bufferSize];
                    file.Read(buffer, 0, buffer.Length);

                    using (var stream = new MemoryStream(buffer)) {
                        stream.Position = 0;
                        var blockIdString = Convert.ToBase64String(BitConverter.GetBytes(currentBlockId));

                        Console.CursorLeft = x;
                        Console.CursorTop = y;
                        Console.Write("  Block {0} of {1} ({2:N0} %)...",
                            currentBlockId + 1,
                            blockCount,
                            (currentBlockId + 1) / blockCount * 100);
                        blob.PutBlock(blockIdString, stream, null);
                        blockIds.Add(blockIdString);
                        currentBlockId++;
                    }
                }
            }
            blob.PutBlockList(blockIds);
            Console.WriteLine("OK");
        }

    }
}
