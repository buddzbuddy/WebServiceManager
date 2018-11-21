using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class ServiceDescriptionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ServiceDescriptionsController()
        {
            ViewBag.ServiceDescriptionsNav = "active";
        }
        public ActionResult Index()
        {
            return View(db.ServiceDescriptions.ToList());
        }

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

        public ActionResult Create()
        {
            return View();
        }

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ServiceDescription serviceDescription = db.ServiceDescriptions.Find(id);
            db.ServiceDescriptions.Remove(serviceDescription);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult SelectTundukOrganization(Guid serviceId, int? id, string returnAction)
        {
            ViewBag.serviceId = serviceId;
            ViewBag.returnAction = returnAction;
            if (id != null)
            {
                return RedirectToAction("SelectSubsystem", new { serviceId, tundukOrganizationId = id, returnAction });
            }
            return View(db.TundukOrganizations.ToList());
        }

        public ActionResult SelectSubsystem(Guid serviceId, int tundukOrganizationId, int? id, string returnAction)
        {
            ViewBag.serviceId = serviceId;
            ViewBag.returnAction = returnAction;
            ViewBag.OrganizationName = db.TundukOrganizations.Find(tundukOrganizationId).Name;
            if (id != null)
            {
                return RedirectToAction(returnAction, new { serviceId, subsystemId = id });
            }
            var subsystems = db.Subsystems.Where(x => x.TundukOrganizationId == tundukOrganizationId).ToList();
            if(subsystems.Count == 1)
            {
                return RedirectToAction(returnAction, new { serviceId, subsystemId = subsystems[0].Id });
            }
            return View(subsystems);
        }

        public ActionResult CreateClientDetails(Guid serviceId, int? subsystemId)
        {
            if(subsystemId == null)
            {
                return RedirectToAction("SelectTundukOrganization", new { serviceId, returnAction = "CreateClientDetails" });
            }

            var serviceCodes = db.ServiceCodes.Where(x => x.SubsystemId == subsystemId).ToList();
            ViewBag.ServiceCodeId = (from sc in serviceCodes
                                     select new SelectListItem
                                     {
                                         Text = sc.Name,
                                         Value = sc.Id.ToString()
                                     });
            ViewBag.SubsystemId = subsystemId;
            var subsystem = db.Subsystems.Find(subsystemId);
            ViewBag.OrganizationName = db.TundukOrganizations.Find(subsystem.TundukOrganizationId).Name;
            ViewBag.SubsystemName = subsystem.Name;
            return View(new ClientDetail { ServiceDescriptionId = serviceId });
        }
        [HttpPost]
        public ActionResult CreateClientDetails(ClientDetail obj, int? subsystemId)
        {
            var objDb = db.ClientDetails.FirstOrDefault(x => x.ServiceCodeId == obj.ServiceCodeId);
            if (objDb != null)
            {
                ModelState.AddModelError("ServiceCodeId", "Для указанного сервиса клиент уже существует! Name: " + objDb.ServiceDescription.Name);
            }
            if (ModelState.IsValid)
            {
                obj.Id = Guid.NewGuid();
                db.ClientDetails.Add(obj);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = obj.ServiceDescriptionId });
            }

            ViewBag.ServiceCodeId = (from sc in db.ServiceCodes
                                     where sc.SubsystemId == subsystemId
                                     select new SelectListItem
                                     {
                                         Text = sc.Name,
                                         Value = sc.Id.ToString(),
                                         Selected = sc.Id == obj.ServiceCodeId
                                     }).ToList();
            ViewBag.SubsystemId = subsystemId;
            var subsystem = db.Subsystems.Find(subsystemId);
            ViewBag.OrganizationName = db.TundukOrganizations.Find(subsystem.TundukOrganizationId).Name;
            ViewBag.SubsystemName = subsystem.Name;
            return View(obj);
        }

        public ActionResult ClientDetails(Guid detailsId)
        {
            return PartialView(db.ClientDetails.Find(detailsId));
        }

        public ActionResult CreateServiceDetails(Guid serviceId, int? subsystemId)
        {
            if (subsystemId == null)
            {
                var ownerTundukMemberCode = ConfigurationManager.AppSettings["c_memberCode"];
                if (!string.IsNullOrEmpty(ownerTundukMemberCode))
                {
                    var orgFromDb = db.TundukOrganizations.FirstOrDefault(x => x.MemberCode == ownerTundukMemberCode);
                    if(orgFromDb != null)
                    {
                        return RedirectToAction("SelectTundukOrganization", new { orgFromDb.Id, serviceId, returnAction = "CreateServiceDetails" });
                    }
                }
                return RedirectToAction("SelectTundukOrganization", new { serviceId, returnAction = "CreateServiceDetails" });
            }

            var serviceCodes = db.ServiceCodes.Where(x => x.SubsystemId == subsystemId).ToList();
            ViewBag.ServiceCodeId = (from sc in serviceCodes
                                     select new SelectListItem
                                     {
                                         Text = sc.Name,
                                         Value = sc.Id.ToString()
                                     });
            ViewBag.SubsystemId = subsystemId;
            var subsystem = db.Subsystems.Find(subsystemId);
            ViewBag.OrganizationName = db.TundukOrganizations.Find(subsystem.TundukOrganizationId).Name;
            ViewBag.SubsystemName = subsystem.Name;
            var serviceDesc = db.ServiceDescriptions.Find(serviceId);
            return View(new ServiceDetail { ServiceDescriptionId = serviceId });
        }
        [HttpPost]
        public ActionResult CreateServiceDetails(ServiceDetail obj, int? subsystemId)
        {
            var objDb = db.ServiceDetails.FirstOrDefault(x => x.ServiceCodeId == obj.ServiceCodeId);
            if (objDb != null)
                ModelState.AddModelError("ServiceCodeId", "Адаптер с таким ServiceCode уже зарегистрирован! Name: " + objDb.ServiceDescription.Name);
            if (ModelState.IsValid)
            {
                db.ServiceDetails.Add(obj);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = obj.ServiceDescriptionId });
            }
            ViewBag.ServiceCodeId = (from sc in db.ServiceCodes
                                     where sc.SubsystemId == subsystemId
                                     select new SelectListItem
                                     {
                                         Text = sc.Name,
                                         Value = sc.Id.ToString(),
                                         Selected = sc.Id == obj.ServiceCodeId
                                     }).ToList();
            ViewBag.SubsystemId = subsystemId;
            var subsystem = db.Subsystems.Find(subsystemId);
            ViewBag.OrganizationName = db.TundukOrganizations.Find(subsystem.TundukOrganizationId).Name;
            ViewBag.SubsystemName = subsystem.Name;
            return View(obj);
        }

        public ActionResult ServiceDetails(int detailsId)
        {
            return PartialView(db.ServiceDetails.Find(detailsId));
        }

        public ActionResult DeleteClientDetails(Guid id)
        {
            var obj = db.ClientDetails.Find(id);
            var serviceId = obj.ServiceDescriptionId;
            db.ClientDetails.Remove(obj);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = serviceId });
        }

        public ActionResult DeleteServiceDetails(int id)
        {
            var obj = db.ServiceDetails.Find(id);
            var serviceId = obj.ServiceDescriptionId;
            db.ServiceDetails.Remove(obj);
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
