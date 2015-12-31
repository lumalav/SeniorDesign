using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Twilio;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Twilio information
        /// </summary>
        const string Greeting = "\nHi User! This is 3DCytoFlow giving you an update on your recent request";
        const string AccountSid = "AC783d5c8576eb1aa7aa03abca29e7a488";
        const string AuthToken = "ec8c9bdecaa381736288af8c3452e171";
        public ActionResult Index()
        {
            //this is only needed to display the files using the container name in the index page
            var storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var storageClient = storageAccount.CreateCloudBlobClient();
            var storageContainer = storageClient.GetContainerReference(
                ConfigurationManager.AppSettings.Get("CloudStorageContainerReference"));
            var blobsList = new CloudFilesModel(storageContainer.ListBlobs(useFlatBlobListing: true));
            return View(blobsList);
        }

        public ActionResult UploadFile()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("LogIn", "Account");
        }

        [HttpPost]
        public ActionResult SetMetadata(int blocksCount, string fileName, long fileSize)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            //container name will be lastname-name-id of the user
       //     var containerName = ConfigurationManager.AppSettings["CloudStorageContainerReference"].ToString();

            var container = GetContainer(storageAccount, "smith-stan-46");

            var fileToUpload = new CloudFile()
            {
                BlockCount = blocksCount,
                FileName = fileName,
                Size = fileSize,
                BlockBlob = container.GetBlockBlobReference(fileName),
                StartTime = DateTime.Now,
                IsUploadCompleted = false,
                UploadStatusMessage = string.Empty
            };
            Session.Add("CurrentFile", fileToUpload);
            return Json(true);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UploadChunk(int id)
        {
            var request = Request.Files["Slice"];

            var chunk = new byte[request.ContentLength];

            request.InputStream.Read(chunk, 0, Convert.ToInt32(request.ContentLength));

            JsonResult returnData;

            const string fileSession = "CurrentFile";

            if (Session[fileSession] != null)
            {
                var model = (CloudFile) Session[fileSession];

                returnData = UploadCurrentChunk(model, chunk, id);

                if (returnData != null)
                {
                    return returnData;
                }
                if (id == model.BlockCount)
                {
                    return CommitAllChunks(model);
                }
            }
            else
            {
                returnData = Json(new
                {
                    error = true,
                    isLastBlock = false,
                    message = string.Format(CultureInfo.CurrentCulture,
                        "Failed to Upload file.", "Session Timed out")
                });
                return returnData;
            }

            return Json(new { error = false, isLastBlock = false, message = string.Empty });
        }

        private ActionResult CommitAllChunks(CloudFile model)
        {
            model.IsUploadCompleted = true;

            var errorInOperation = false;

            try
            {
                var blockList = Enumerable.Range(1, (int) model.BlockCount).ToList().ConvertAll(rangeElement =>
                                Convert.ToBase64String(Encoding.UTF8.GetBytes(
                                string.Format(CultureInfo.InvariantCulture, "{0:D4}", rangeElement))));

                model.BlockBlob.PutBlockList(blockList);

                var duration = DateTime.Now - model.StartTime;

                float fileSizeInKb = model.Size / 1024;

                var fileSizeMessage = fileSizeInKb > 1024 ?
                    string.Concat((fileSizeInKb / 1024).ToString(CultureInfo.CurrentCulture), " MB") :
                    string.Concat(fileSizeInKb.ToString(CultureInfo.CurrentCulture), " KB");

                var message = string.Format(CultureInfo.CurrentCulture,
                    "File uploaded successfully. {0} took {1} seconds to upload",
                    fileSizeMessage, duration.TotalSeconds);

                //After commiting the file. Send a confirmation to the user
                var twilio = new TwilioRestClient(AccountSid, AuthToken);
                twilio.SendMessage("+12027598248", "+14077163399", Greeting + "\nStatus on: " + model.FileName + "\n" + message, "");

                model.UploadStatusMessage = message;
            }
            catch (StorageException e)
            {
                model.UploadStatusMessage = "Failed to Upload file. Exception - " + e.Message;
                errorInOperation = true;
            }
            finally
            {
                Session.Remove("CurrentFile");
            }
            return Json(new
            {
                error = errorInOperation,
                isLastBlock = model.IsUploadCompleted,
                message = model.UploadStatusMessage
            });
        }

        private JsonResult UploadCurrentChunk(CloudFile model, byte[] chunk, int id)
        {
            using (var chunkStream = new MemoryStream(chunk))
            {
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, "{0:D4}", id)));
                try
                {
                    model.BlockBlob.PutBlock(
                        blockId,
                        chunkStream, null, null,
                        new BlobRequestOptions()
                        {
                            RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3)
                        });

                    return null;
                }
                catch (StorageException e)
                {
                    Session.Remove("CurrentFile");
                    model.IsUploadCompleted = true;
                    model.UploadStatusMessage = "Failed to Upload file. Exception - " + e.Message;
                    return Json(new { error = true, isLastBlock = false, message = model.UploadStatusMessage });
                }
            }
        }

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