using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WEB.Models;

namespace WEB.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DashboardNav = " active";
            return View();
        }
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult GetServiceAmount()
        {
            return Json(db.ServiceDetails.Count(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetClientAmount()
        {
            return Json(db.ClientDetails.Count(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSuccessRequestsAmount()
        {
            return Json(db.ReceiveHistoryItems.Count(x => x.IsSuccess), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetErrorRequestsAmount()
        {
            return Json(db.ReceiveHistoryItems.Count(x => !x.IsSuccess), JsonRequestBehavior.AllowGet);
        }
    }
}