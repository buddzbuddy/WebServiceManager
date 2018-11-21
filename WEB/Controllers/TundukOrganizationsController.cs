using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WEB.Models;
using WEB.Models.Entities;

namespace WEB.Controllers
{
    [Authorize]
    public class TundukOrganizationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.TundukOrganizations.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TundukOrganization tundukOrganization = db.TundukOrganizations.Find(id);
            if (tundukOrganization == null)
            {
                return HttpNotFound();
            }
            return View(tundukOrganization);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,MemberCode")] TundukOrganization tundukOrganization)
        {
            if (ModelState.IsValid)
            {
                db.TundukOrganizations.Add(tundukOrganization);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tundukOrganization);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TundukOrganization tundukOrganization = db.TundukOrganizations.Find(id);
            if (tundukOrganization == null)
            {
                return HttpNotFound();
            }
            return View(tundukOrganization);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,MemberCode")] TundukOrganization tundukOrganization)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tundukOrganization).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tundukOrganization);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TundukOrganization tundukOrganization = db.TundukOrganizations.Find(id);
            if (tundukOrganization == null)
            {
                return HttpNotFound();
            }
            return View(tundukOrganization);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TundukOrganization tundukOrganization = db.TundukOrganizations.Find(id);
            db.TundukOrganizations.Remove(tundukOrganization);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult CreateSubsystem(int tundukOrganizationId)
        {
            var model = new Subsystem
            {
                TundukOrganizationId = tundukOrganizationId,
                TundukOrganization = db.TundukOrganizations.Find(tundukOrganizationId)
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateSubsystem(Subsystem model)
        {
            if(db.Subsystems.Any(x => x.Name == model.Name && x.TundukOrganizationId == model.TundukOrganizationId))
            {
                ModelState.AddModelError("Name", "Already exist");
            }
            if (ModelState.IsValid)
            {
                db.Subsystems.Add(model);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = model.TundukOrganizationId });
            }
            model.TundukOrganization = db.TundukOrganizations.Find(model.TundukOrganizationId);
            return View(model);
        }

        public ActionResult CreateServiceCode(int subsystemId)
        {
            var model = new ServiceCode
            {
                SubsystemId = subsystemId,
                Subsystem = db.Subsystems.Find(subsystemId)
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateServiceCode(ServiceCode model)
        {
            if (db.ServiceCodes.Any(x => x.Name == model.Name && x.Version == model.Version && x.SubsystemId == model.SubsystemId))
            {
                ModelState.AddModelError("Name", "Already exist");
            }
            var subsystem = db.Subsystems.Find(model.SubsystemId);
            if (ModelState.IsValid)
            {
                db.ServiceCodes.Add(model);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = subsystem.TundukOrganizationId });
            }
            model.Subsystem = subsystem;
            return View(model);
        }

        public ActionResult DeleteServiceCode(int id)
        {
            var obj = db.ServiceCodes.Find(id);
            var orgId = obj.Subsystem.TundukOrganizationId;
            if (obj != null)
            {
                db.ServiceCodes.Remove(obj);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = orgId });
            }
            return HttpNotFound();
        }
        public ActionResult DeleteSubsystem(int id)
        {
            var obj = db.Subsystems.Find(id);
            var orgId = obj.TundukOrganizationId;
            if (obj != null)
            {
                db.Subsystems.Remove(obj);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = orgId });
            }
            return HttpNotFound();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
