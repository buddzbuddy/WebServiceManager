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
    public class ServiceDescriptionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ServiceDescriptionsController()
        {
            ViewBag.ServiceDescriptionsNav = "active";
        }
        // GET: ServiceDescriptions
        public ActionResult Index()
        {
            return View(db.ServiceDescriptions.ToList());
        }

        // GET: ServiceDescriptions/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ServiceDescription serviceDescription = db.ServiceDescriptions.Find(id);
            if (serviceDescription == null)
            {
                return HttpNotFound();
            }
            return View(serviceDescription);
        }

        // GET: ServiceDescriptions/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ServiceDescriptions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,AgreementNo,ConnectionType,InteractionType,IsActive")] ServiceDescription serviceDescription)
        {
            if (db.ServiceDescriptions.Any(x => x.Name.Trim().ToUpper() == serviceDescription.Name.Trim().ToUpper()))
                ModelState.AddModelError("Name", "Сервис с таким именем уже существует в базе-данных");
            if (ModelState.IsValid)
            {
                serviceDescription.Id = Guid.NewGuid();
                serviceDescription.Name = serviceDescription.Name.Trim().ToUpper();
                db.ServiceDescriptions.Add(serviceDescription);
                db.SaveChanges();
                return RedirectToAction("Details", new { serviceDescription.Id });
            }

            return View(serviceDescription);
        }

        // GET: ServiceDescriptions/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ServiceDescription serviceDescription = db.ServiceDescriptions.Find(id);
            if (serviceDescription == null)
            {
                return HttpNotFound();
            }
            return View(serviceDescription);
        }

        // POST: ServiceDescriptions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,AgreementNo,ConnectionType,InteractionType,IsActive")] ServiceDescription serviceDescription)
        {
            if (ModelState.IsValid)
            {
                db.Entry(serviceDescription).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { serviceDescription.Id });
            }
            return View(serviceDescription);
        }

        // GET: ServiceDescriptions/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ServiceDescription serviceDescription = db.ServiceDescriptions.Find(id);
            if (serviceDescription == null)
            {
                return HttpNotFound();
            }
            return View(serviceDescription);
        }

        // POST: ServiceDescriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ServiceDescription serviceDescription = db.ServiceDescriptions.Find(id);
            db.ServiceDescriptions.Remove(serviceDescription);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        public ActionResult CreateClientDetails(Guid serviceId)
        {
            return View(new ClientDetail { ServiceDescriptionId = serviceId });
        }
        [HttpPost]
        public ActionResult CreateClientDetails(ClientDetail obj)
        {
            if (db.ClientDetails.Any(x => x.WsdlUrl.Trim().ToUpper() == obj.WsdlUrl.Trim().ToUpper() && x.MethodName.Trim().ToUpper() == obj.MethodName.Trim().ToUpper()))
                ModelState.AddModelError("", "Клиент с таким WsdlUrl и MethodName уже существует в базе-данных");
            if (ModelState.IsValid)
            {
                obj.Id = Guid.NewGuid();
                db.ClientDetails.Add(obj);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = obj.ServiceDescriptionId });
            }

            return View(obj);
        }

        public ActionResult ClientDetails(Guid detailsId)
        {
            return PartialView(db.ClientDetails.Find(detailsId));
        }

        public ActionResult DeleteClientDetails(Guid id)
        {
            var obj = db.ClientDetails.Find(id);
            var serviceId = obj.ServiceDescriptionId;
            db.ClientDetails.Remove(obj);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = serviceId });
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
