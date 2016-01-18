using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _3DCytoFlow.Controllers
{
    public class AnalysesController : Controller
    {
        private readonly CytoFlowDBContext _db = new CytoFlowDBContext();

        // GET: Analysis
        public ActionResult Index()
        {
            return View(_db.Analyses.ToList());
        }

        // GET: Analysis/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var analysis = _db.Analyses.Find(id);
            
            if (analysis == null)
            {
                return HttpNotFound();
            }
            return View(analysis);
        }

        // GET: Analysis/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            var analysis = _db.Analyses.Find(id);
            
            if (analysis == null)
            {
                return HttpNotFound();
            }
            return View(analysis);
        }

        // POST: Analysis/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var analysis = _db.Analyses.Find(id);
            
            _db.Analyses.Remove(analysis);
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
    }
}
