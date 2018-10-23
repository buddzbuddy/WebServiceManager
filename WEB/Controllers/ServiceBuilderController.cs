using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WEB.Controllers
{
    public class ServiceBuilderController : Controller
    {
        public ActionResult CreateProjectFolder(string name, bool isTunduk = false)
        {
            if (!isTunduk)
                return Json(new { result = CMD_Commands.CreateProjectFolder(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
            else
            {
                return Json(new { result = CMD_Commands.CreateWCFProjectFolder(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PublishToPackage(string name, bool withDeploy = false)
        {
            if (!withDeploy)
            {
                return Json(new { result = CMD_Commands.PublishToPackage(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = CMD_Commands.PublishToPackage(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult IsPublished(string name)
        {
            return Json(new { result = CMD_Commands.IsPublished(name) }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CheckDeploy(string name)
        {
            return Json(new { result = CMD_Commands.DeployWCF(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Deploy(string name)
        {
            return Json(new { result = CMD_Commands.DeployWCF(name, out List<string> outputLines, out string error, true), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult IsDeployed(string name)
        {
            var isDeployed = CMD_Commands.IsDeployed(name);
            if (isDeployed)
            {
                return Json(new { result = true, serviceUrl = "http://localhost/Tunduk" + name + "/Service.svc" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult CheckProjectFolder(string name)
        {
            return Json(new { result = CMD_Commands.ExistFolder(name, false) }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult HasBuilding(string name)
        {
            var prjPath = CMD_Commands.GetFullProjectPath(name);
            prjPath = CMD_Commands.GetFullProjectPath(prjPath, "bin\\service.exe");
            return Json(new { result = CMD_Commands.HasFile(prjPath) }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult BuildProject(string name)
        {
            return Json(new { result = CMD_Commands.BuildProject(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult RebuildProject(string name)
        {
            return Json(new { result = CMD_Commands.RebuildProject(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InstallService(string name)
        {
            return Json(new { result = CMD_Commands.InstallService(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UninstallService(string name)
        {
            return Json(new { result = CMD_Commands.UninstallService(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult StartService(string name)
        {
            return Json(new { result = CMD_Commands.StartService(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult StopService(string name)
        {
            return Json(new { result = CMD_Commands.StopService(name, out List<string> outputLines, out string error), error, outputLines = outputLines.ToArray() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetWinServiceInfo(string name)
        {
            CMD_Commands.GetWinServiceInfo(name, out bool isExist, out bool isStarted, out string error);
            return Json(new { result = string.IsNullOrEmpty(error), isExist, isStarted, error }, JsonRequestBehavior.AllowGet);
        }
    }
}