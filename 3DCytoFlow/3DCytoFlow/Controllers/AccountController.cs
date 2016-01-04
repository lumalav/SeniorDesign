using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        const string Greeting = "\nHi User! This is 3DCytoFlow giving you an update on your recent request";

        private readonly CytoFlowDBContext _db = new CytoFlowDBContext();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Require the user to have a confirmed email before they can log on.
            var applicationUser = await UserManager.FindByNameAsync(model.Email);
            
            //Get the actual user from the DB
            var user = _db.Users.FirstOrDefault(i => i.Email.Equals(applicationUser.Email));

            if (applicationUser != null && user != null)
            {
                if (!await UserManager.IsEmailConfirmedAsync(applicationUser.Id))
                {
                    ViewBag.errorMessage = "Your email have not been confirmed.";
                    return View("Error");
                }
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                //Get the doctor's role
                var role = _db.UserRoles.First(i => i.Name.Equals("doctor"));

                var applicationUser = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(applicationUser, model.Password);

                //Create the user and assign role
                var user = new User
                {
                    FirstName = model.FirstName,
                    Middle = model.Middle,
                    LastName = model.LastName,
                    DOB = model.DOB,
                    Login = model.Email,
                    Password = applicationUser.PasswordHash,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.WorkAddress,
                    City = model.City,
                    Zip = model.Zip,
                    UserRole_Id = role.Id,
                    UserRole = role
                };

                //add it to the DB
                _db.Users.Add(user);

                if (result.Succeeded)
                {
                    // Send an email with this link
                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(applicationUser.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = applicationUser.Id, code }, Request.Url.Scheme);

                    EmailService.SendMail(new IdentityMessage
                                          {
                                             Subject = "Confirm " + user.FirstName + " " + user.LastName + " account",
                                             Body = "Please confirm " + user.FirstName + " " + user.LastName + " account by clicking: " + callbackUrl
                                          });

                    //save the changes to the db
                    _db.SaveChanges();

                    return View("WaitConfirmation");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/UploadFile
        public ActionResult UploadFile()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = GetUser();

                var model = new UploadFileModel {Patients = user.Patients};
                
                return View(model);
            }

            return RedirectToAction("LogIn", "Account");
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }

            var result = await UserManager.ConfirmEmailAsync(userId, code);

            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // Done
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { UserId = user.Id, code }, Request.Url.Scheme);

                EmailService.SendMail(new IdentityMessage
                {
                    Subject = "Reset Password",
                    Body = "Please reset your password by clicking here: " + callbackUrl 
                }, user.Email);

                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var applicationUser = await UserManager.FindByNameAsync(model.Email);
            if (applicationUser == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            var result = await UserManager.ResetPasswordAsync(applicationUser.Id, model.Code, model.Password);

            var user = _db.Users.First(i => i.Email.Equals(applicationUser.Email));
            user.Password = applicationUser.PasswordHash;

            if (result.Succeeded)
            {
                _db.SaveChanges();
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, model.ReturnUrl, model.RememberMe });
        }

        // 
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
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
        /// <summary>
        /// returns a patient with the provided name
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        private Patient GetPatient(string firstName, string lastName)
        {
            return _db.Patients.First(i => i.FirstName.Equals(firstName) && i.LastName.Equals(lastName));
        }
        /// <summary>
        /// Prepares the storage that will receive the .fcs file
        /// </summary>
        /// <param name="blocksCount"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SetMetadata(int blocksCount, string fileName, long fileSize, string patient)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            var patientCompleteName = patient.Split(' ');

            var firstName = patientCompleteName[0];
            var lastName = patientCompleteName[1];

            //container name will be lastname-name-id of the user. Everything in lowercase or Azure complains with a 400 error
            var user = GetUser();
            var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;
            var container = GetContainer(storageAccount, containerName.ToLower());

            //get the patient
            var storedPatient = GetPatient(firstName, lastName);

            //blob exact name and location
            var blobName = lastName + "-" + firstName + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".fcs";

            //filename will be lastname-name-uploaddate.fcs of the patient
            var fileToUpload = new CloudFile()
            {
                OriginalFileName = fileName,
                Patient = storedPatient,
                BlockCount = blocksCount,
                FileName = blobName.ToLower(),
                Size = fileSize,
                BlockBlob = container.GetBlockBlobReference(blobName.ToLower()),
                StartTime = DateTime.Now,
                IsUploadCompleted = false,
                UploadStatusMessage = string.Empty
            };

            Session.Add("CurrentFile", fileToUpload);

            return Json(true);
        }
        
        /// <summary>
        /// Uploads a chunk
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Sends every chunk of the .fcs file and sends an sms to the user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private ActionResult CommitAllChunks(CloudFile model)
        {
            model.IsUploadCompleted = true;

            var errorInOperation = false;

            try
            {
                var blockList = Enumerable.Range(1, (int)model.BlockCount).ToList().ConvertAll(rangeElement =>
                               Convert.ToBase64String(Encoding.UTF8.GetBytes(
                               string.Format(CultureInfo.InvariantCulture, "{0:D4}", rangeElement))));

                model.BlockBlob.PutBlockList(blockList);

                var duration = DateTime.Now - model.StartTime;

                float fileSizeInKb = model.Size / 1024;

                var fileSizeMessage = fileSizeInKb > 1024 ?
                    string.Concat((fileSizeInKb / 1024).ToString(CultureInfo.CurrentCulture), " MB") :
                    string.Concat(fileSizeInKb.ToString(CultureInfo.CurrentCulture), " KB");

                var message = string.Format(CultureInfo.CurrentCulture,
                    "File uploaded successfully. {0} took {1} seconds to upload\nYou'll receive another SMS when the results are completed",
                    fileSizeMessage, duration.TotalSeconds);

                //Get the user
                var user = GetUser();
                var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id + "/" + model.FileName;
                //if the analysis did not exist before, add a new record to the db
                if (ThereIsNoPreviousAnalysis(model, fcsPath))
                {
                    var analysis = new Analysis
                    {
                        Date = DateTime.Now.Date,
                        FcsFilePath = fcsPath,
                        FcsUploadDate = DateTime.Now.ToString("MM-dd-yyyy"),
                        ResultFilePath = "",
                        ResultDate = DateTime.Now.Date,
                        Delta = 0.00
                    };

                    var storedPatient = GetPatient(model.Patient.FirstName, model.Patient.LastName);

                    storedPatient.Analyses.Add(analysis);
                    user.Analyses.Add(analysis);
                    _db.SaveChanges();
                }
                //otherwise, continue with the process and
                //notify the user about the success of the file upload
                SmsService.SendSms(new IdentityMessage
                {
                    Destination = user.Phone,
                    Body = Greeting + "\nStatus on: " + model.OriginalFileName + "\n" + message
                });

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
        /// <summary>
        /// returns if there is no previous analysis for this particular user and a particular date
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fcsPath"></param>
        /// <returns></returns>
        private bool ThereIsNoPreviousAnalysis(CloudFile model, string fcsPath)
        {
            return
                !_db.Analyses.Any(
                    i =>
                        i.Patient.FirstName.Equals(model.Patient.FirstName) &&
                        i.Patient.LastName.Equals(model.Patient.LastName) && i.FcsFilePath.Equals(fcsPath));
        }

        /// <summary>
        /// Uploads the current chunk to the storage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="chunk"></param>
        /// <param name="id"></param>
        /// <returns></returns>
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

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}