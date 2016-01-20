using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace _3DCytoFlow.Controllers
{
    public class FileController : Controller
    {
        /// <summary>
        /// Downloads all the json files from the storage and saves them in the Results folder
        /// </summary> 
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DownloadResult(string path)
        {
            var jsonString = "";
            //prepare container name
            var index = path.IndexOf('/');

            var containerName = path.Substring(0, index);

            //prepare blobName
            var blobName = path.Substring(index + 1);

            // Retrieve storage account from web.config
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            //retrieve container
            var container = GetContainer(storageAccount, containerName);

            //List blobs and directories in this container
            var blobs = container.ListBlobs(useFlatBlobListing: true);

            //prepare the location
            foreach (var blob in blobs.Where(i => i.Uri.ToString().Contains(".json")).Cast<CloudBlockBlob>().Where(i => i.Name.Equals(blobName)))
            {
                using (var stream = new MemoryStream())
                {
                    blob.DownloadToStream(stream);

                    stream.Position = 0;

                    var serializer = new JsonSerializer();

                    using (var sr = new StreamReader(stream))
                    {                        
                        using (var jsonTextReader = new JsonTextReader(sr))
                        {
                            var result = serializer.Deserialize(jsonTextReader);
                            jsonString = JsonConvert.SerializeObject(result);
                        }
                    }
                }
            }

            return Content(jsonString);
        }

        /// <summary>
        /// returns the container, if not it will create a new one
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public CloudBlobContainer GetContainer(CloudStorageAccount account, string name)
        {
            //blob client now
            var blobClient = account.CreateCloudBlobClient();

            //the container for this is companystyles
            var container = blobClient.GetContainerReference(name);

            //Create a new container, if it does not exist
            container.CreateIfNotExists();

            return container;
        }

    }
}