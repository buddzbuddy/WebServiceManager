using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WEB.Models;
using WEB.Models.Entities;

namespace WEB.Controllers
{
    [Authorize]
    public class ReceiveMonitorController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: ReceiveMonitor
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetReceiveHistory(Guid clientId)
        {
            try
            {
                var items = db.ReceiveHistoryItems.Where(x => x.ClientId == clientId).Select(x => new 
                {
                    x.EntryTime,
                    x.ErrorMessage,
                    x.Id,
                    x.InputSize,
                    x.IsSuccess,
                    x.OperationDuration,
                    x.OutputSize
                }).ToList().GroupBy(x => new DateTime(x.EntryTime.Year, x.EntryTime.Month, x.EntryTime.Day)).OrderBy(x => x.Key).Select(x => new
                {
                    Date = x.Key.ToString("yyyyMMdd"),
                    Amount = new
                    {
                        Total = x.Count(),
                        Error = x.Count(x1 => !x1.IsSuccess),
                        Success = x.Count(x1 => x1.IsSuccess)
                    }
                });
                if (items.Count() > 0)
                {
                    return Json(new { result = true, items }, JsonRequestBehavior.AllowGet);
                }
                else throw new ApplicationException("No data found!");
            }
            catch(Exception e)
            {
                return Json(new { result = false, errorMessage = e.GetBaseException().Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetTransmitHistory(int serviceDetailId)
        {
            try
            {
                var items = db.TransmitHistoryItems.Where(x => x.ServiceDetailId == serviceDetailId).Select(x => new
                {
                    x.EntryTime,
                    x.ErrorMessage,
                    x.Id,
                    x.InputSize,
                    x.IsSuccess,
                    x.OperationDuration,
                    x.OutputSize
                }).ToList().GroupBy(x => new DateTime(x.EntryTime.Year, x.EntryTime.Month, x.EntryTime.Day)).OrderBy(x => x.Key).Select(x => new
                {
                    Date = x.Key.ToString("yyyyMMdd"),
                    Amount = new
                    {
                        Total = x.Count(),
                        Error = x.Count(x1 => !x1.IsSuccess),
                        Success = x.Count(x1 => x1.IsSuccess)
                    }
                });
                if (items.Count() > 0)
                {
                    return Json(new { result = true, items }, JsonRequestBehavior.AllowGet);
                }
                else throw new ApplicationException("No data found!");
            }
            catch (Exception e)
            {
                return Json(new { result = false, errorMessage = e.GetBaseException().Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}