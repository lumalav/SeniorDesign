using System.Linq;
using System.Web.Mvc;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly CytoFlowDBContext _db = new CytoFlowDBContext();

        public ActionResult Index()
        {
            //get the storage account
//            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
//            var storageClient = storageAccount.CreateCloudBlobClient();
//
//            //get the user and the container name
//            var user = GetUser();
//            var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;
//
//            //get the blobs from the container
//            var storageContainer = storageClient.GetContainerReference(containerName.ToLower());
//            var blobsList = new CloudFilesModel(storageContainer.ListBlobs(useFlatBlobListing: true));

            return View();
        }

        #region Helpers
        /// <summary>
        /// returns the current user
        /// </summary>
        /// <returns></returns>
        private User GetUser()
        {
            return _db.Users.First(i => i.Email.Equals(User.Identity.Name));
        }
        #endregion
    }
}