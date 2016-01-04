using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _3DCytoFlow.Controllers
{
    public class PatientsController : Controller
    {
        private readonly CytoFlowDBContext _db = new CytoFlowDBContext();

        // GET: Patients
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = GetUser();
         
                return View(_db.Patients.Where(i => i.User_Id.Equals(user.Id)).ToList());
            }

            return RedirectToAction("LogIn", "Account");
        }

        // GET: Patients/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var patient = _db.Patients.Find(id);
            
            if (patient == null)
            {
                return HttpNotFound();
            }
            return View(patient);
        }

        // GET: Patients/Create
        public ActionResult Create()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("LogIn", "Account");
        }

        // POST: Patients/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,FirstName,Middle,LastName,DOB,Email,Phone,Address,City,Zip")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                var user = GetUser();
                user.Patients.Add(patient);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(patient);
        }

        // GET: Patients/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            var patient = _db.Patients.Find(id);
            
            if (patient == null)
            {
                return HttpNotFound();
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,FirstName,Middle,LastName,DOB,Email,Phone,Address,City,Zip")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(patient).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // GET: Patients/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            var patient = _db.Patients.Find(id);
            
            if (patient == null)
            {
                return HttpNotFound();
            }
            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {          
            var patient = _db.Patients.Find(id);
            
            _db.Patients.Remove(patient);
            
            _db.SaveChanges();
            
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Helpers
        private User GetUser()
        {
            return _db.Users.First(i => i.Email.Equals(User.Identity.Name));
        }
        #endregion
    }
}