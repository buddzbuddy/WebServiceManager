using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using WEB.Models;
using WEB.Models.Entities;

namespace WEB.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ReportsController()
        {
            ViewBag.ReportsNav = "active";
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult RequestByType()
        {
            var model = new RequestByTypeViewModel
            {
                SearchResult = new List<ReportItemViewModel>()
            };

            return View(model);
        }
        [HttpPost]
        public ActionResult RequestByType(RequestByTypeViewModel model)
        {
            var query = db.ServiceDescriptions.AsQueryable();

            if(model.ConnectionType != null)
            {
                query = query.Where(x => x.ConnectionType == model.ConnectionType);
            }
            if(model.InteractionType != null)
            {
                query = query.Where(x => x.InteractionType == model.InteractionType);
            }

            model.SearchResult = (from r in query
                                  select new ReportItemViewModel
                                  {
                                      Connection = r
                                  }).ToList();

            return View(model);
        }

        [HttpGet]
        public ActionResult RequestByPeriod()
        {
            var model = new RequestByPeriodViewModel
            {
                SearchResult = new List<RequestByPeriodItem>()
            };

            return View(model);
        }
        [HttpPost]
        public ActionResult RequestByPeriod(RequestByPeriodViewModel model)
        {
            model.SearchResult = new List<RequestByPeriodItem>();

            var connections = db.ServiceDescriptions.Include(x => x.ClientDetails).Include(x => x.ServiceDetails).ToList();

            foreach(var c in connections)
            {
                var modelItem = new RequestByPeriodItem
                {
                    Connection = c
                };
                var receivedHistoryQuery = db.ReceiveHistoryItems.AsQueryable();
                if(c.ClientDetails.Count > 0)
                {
                    var cIds = c.ClientDetails.Select(x1 => x1.Id).ToList();
                    receivedHistoryQuery = receivedHistoryQuery.Where(x => cIds.Contains(x.ClientId ?? Guid.Empty));
                }
                else
                {
                    receivedHistoryQuery = null;
                }


                var transmittedHistoryQuery = db.TransmitHistoryItems.AsQueryable();
                if (c.ServiceDetails.Count > 0)
                {
                    var sIds = c.ServiceDetails.Select(x1 => x1.Id);
                    transmittedHistoryQuery = transmittedHistoryQuery.Where(x => sIds.Contains(x.ServiceDetailId ?? 0));
                }
                else
                {
                    transmittedHistoryQuery = null;
                }


                if (model.StartDate != null)
                {
                    receivedHistoryQuery = receivedHistoryQuery != null ? receivedHistoryQuery.Where(x => x.EntryTime >= model.StartDate) : receivedHistoryQuery;
                    transmittedHistoryQuery = transmittedHistoryQuery != null ? transmittedHistoryQuery.Where(x => x.EntryTime >= model.StartDate) : transmittedHistoryQuery;
                }
                if(model.EndDate != null)
                {
                    receivedHistoryQuery = receivedHistoryQuery != null ? receivedHistoryQuery.Where(x => x.EntryTime <= model.EndDate) : receivedHistoryQuery;
                    transmittedHistoryQuery = transmittedHistoryQuery != null ? transmittedHistoryQuery.Where(x => x.EntryTime <= model.EndDate) : transmittedHistoryQuery;
                }
                
                modelItem._ReceivedDataSize = receivedHistoryQuery != null ? receivedHistoryQuery.Sum(x => x.OutputSize) ?? 0 : 0;
                modelItem._TransmittedDataSize = transmittedHistoryQuery != null ? transmittedHistoryQuery.Sum(x => x.OutputSize) ?? 0 : 0;
                modelItem.TransmittedRows = transmittedHistoryQuery != null ? transmittedHistoryQuery.Sum(x => x.OutputRows) ?? 0 : 0;
                if (!(modelItem._ReceivedDataSize == 0 && modelItem._TransmittedDataSize == 0 && modelItem.TransmittedRows == 0))
                    model.SearchResult.Add(modelItem);
            }
            return View(model);
        }

        public ActionResult RequestByOrg()
        {
            var model = new RequestByOrgViewModel
            {
                ReceivedHistoryByOrgs = new List<RequestByOrgItem>(),
                TransmittedHistoryByOrgs = new List<RequestByOrgItem>()
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult RequestByOrg(RequestByOrgViewModel model)
        {
            model.ReceivedHistoryByOrgs = new List<RequestByOrgItem>();
            model.TransmittedHistoryByOrgs = new List<RequestByOrgItem>();

            var receiveHistoryQuery = db.ReceiveHistoryItems.AsQueryable();
            var transmitHistoryQuery = db.TransmitHistoryItems.AsQueryable();
            if (model.StartDate != null)
            {
                receiveHistoryQuery = receiveHistoryQuery.Where(x => x.EntryTime >= model.StartDate);
                transmitHistoryQuery = transmitHistoryQuery.Where(x => x.EntryTime >= model.StartDate);
            }
            if (model.EndDate != null)
            {
                receiveHistoryQuery = receiveHistoryQuery.Where(x => x.EntryTime <= model.EndDate);
                transmitHistoryQuery = transmitHistoryQuery.Where(x => x.EntryTime <= model.EndDate);
            }

            var receiveOrgs = receiveHistoryQuery.Select(x => x.Client.ServiceCode.Subsystem.TundukOrganization.MemberCode).Distinct().ToList();
            foreach(var orgCode in receiveOrgs)
            {
                //Организация, откуда было поступление данных в КИССП
                var org = db.TundukOrganizations.FirstOrDefault(x => x.MemberCode == orgCode);
                if(org == null)
                {
                    org = new TundukOrganization { MemberCode = orgCode, Name = "НЕИЗВЕСТНО" };
                }
                //все клиенты к данной организации
                var orgReceiveHistoryQuery = receiveHistoryQuery.Where(x => x.Client.ServiceCode.Subsystem.TundukOrganization.MemberCode == orgCode);
                foreach(var conName in orgReceiveHistoryQuery.Select(x => x.Client.ServiceDescription.Name).Distinct().ToList())
                {
                    var item = new RequestByOrgItem
                    {
                        ConnectionName = conName,
                        Organization = org,
                        _ReceivedDataSize = orgReceiveHistoryQuery.Where(x => x.Client.ServiceDescription.Name == conName).Sum(x => x.OutputSize) ?? 0
                    };
                    model.ReceivedHistoryByOrgs.Add(item);
                }
            }

            var transmitOrgs = transmitHistoryQuery.Select(x => x.c_memberCode).Distinct().ToList();
            foreach (var orgCode in transmitOrgs)
            {
                //Организация, куда были переданы данные из КИССП
                var org = db.TundukOrganizations.FirstOrDefault(x => x.MemberCode == orgCode);
                if (org == null)
                {
                    org = new TundukOrganization { MemberCode = orgCode, Name = "НЕИЗВЕСТНО" };
                }
                //все трансферы в данную организацию
                var orgTransmitHistoryQuery = transmitHistoryQuery.Where(x => x.c_memberCode == orgCode);
                foreach (var conName in orgTransmitHistoryQuery.ToList().Select(x => (x.ServiceDetail ?? new ServiceDetail { ServiceCode = new ServiceCode { Name = x.ErrorMessage } }).ServiceCode.Name).Distinct())
                {
                    var item = new RequestByOrgItem
                    {
                        ConnectionName = conName,
                        Organization = org,
                        _TransmittedDataSize = orgTransmitHistoryQuery.ToList().Where(x => (x.ServiceDetail != null && x.ServiceDetail.ServiceCode.Name == conName) || (x.ErrorMessage == conName)).Sum(x => x.OutputSize) ?? 0,
                        TransmittedRows = orgTransmitHistoryQuery.ToList().Where(x => (x.ServiceDetail != null && x.ServiceDetail.ServiceCode.Name == conName) || (x.ErrorMessage == conName)).Sum(x => x.OutputRows) ?? 0
                    };
                    model.TransmittedHistoryByOrgs.Add(item);
                }
            }
            return View(model);
        }
    }
}