using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClassLibrary
{
    public static class CMD_Commands
    {
        public static readonly string _MainFolderPath = System.Configuration.ConfigurationManager.AppSettings["MainFolder"];
        public static readonly string _ProjectTemplatePath = _MainFolderPath + "\\ProjectTemplates";
        public static readonly string _ServiceProjectTemplatePath = _ProjectTemplatePath + "\\Service";
        public static readonly string _WCFProjectTemplatePath = _ProjectTemplatePath + "\\WCFService";
        public static readonly string _ServiceProjectsPath = _MainFolderPath + "\\ServiceProjects";

        public static readonly string _copyFolder = "xcopy {0} {1} /E";

        public static readonly string _buildProject = "msbuild {0} /t:Build";
        public static readonly string _rebuildProject = "msbuild {0} /t:Clean;Rebuild";

        public static readonly string _installServiceCMD = @"installutil {0}";
        public static readonly string _uninstallServiceCMD = @"installutil /u {0}";

        public static readonly string _startServiceCMD = "net start {0}";
        public static readonly string _stopServiceCMD = "net stop {0}";

        public static readonly string _service_infoCMD = "sc query {0}";
        public static readonly string _service_doesnt_exist_msg_en = "The specified service does not exist as an installed service.";
        public static readonly string _service_doesnt_exist_msg_ru = "Указанная служба не установлена.";
        public static readonly string _service_name_msg_en = "SERVICE_NAME: {0}";
        public static readonly string _service_name_msg_ru = "Имя_службы: {0}";
        public static readonly string _service_state_stoped_msg_en = "STOPPED";
        public static readonly string _service_state_running_msg_en = "RUNNING";

        static bool ExecuteCMD(string command, out List<string> outputLines, out string error)
        {
            if (string.IsNullOrEmpty(command)) throw new Exception("Command is empty!");
            error = string.Empty;
            outputLines = new List<string>();
            using (Process process = Process.Start(new ProcessStartInfo("cmd")
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                Verb = "RunAs",
                //Domain = System.Configuration.ConfigurationManager.AppSettings["Domain"],
                UserName = System.Configuration.ConfigurationManager.AppSettings["UserName"],
                PasswordInClearText = System.Configuration.ConfigurationManager.AppSettings["PasswordInClearText"]
            }))
            {
                process.StandardInput.AutoFlush = true;

                if (process.StandardInput.BaseStream.CanWrite)
                {
                    process.StandardInput.WriteLine(command);
                }
                process.StandardInput.Close();
                while (!process.StandardOutput.EndOfStream)
                {
                    string s = process.StandardOutput.ReadLine();
                    outputLines.Add(s);
                }
                error = process.StandardError.ReadToEnd();
                process.Close();
            }
            return string.IsNullOrEmpty(error);
        }

        public static bool ExistFolder(string fullPath, bool isFull = true)
        {
            if(!isFull)
            {
                fullPath = Path.Combine(_MainFolderPath, _ServiceProjectsPath, fullPath);
            }
            return Directory.Exists(fullPath);
        }
        public static bool CreateProjectFolder(string name, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var fullPath = Path.Combine(_MainFolderPath, _ServiceProjectsPath, name);
                if (ExistFolder(fullPath))
                {
                    error = "Не могу создать(скопировать из шаблона) проект, данное расположение " + fullPath + " уже существует!";
                    return false;
                }
                Directory.CreateDirectory(fullPath);
                return CopyTemplateProject(fullPath, out outputLines, out error); ;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
        public static bool CreateWCFProjectFolder(string name, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var fullPath = Path.Combine(_MainFolderPath, _ServiceProjectsPath, name);
                if (ExistFolder(fullPath))
                {
                    error = "Не могу создать(скопировать из шаблона) проект, данное расположение " + fullPath + " уже существует!";
                    return false;
                }
                Directory.CreateDirectory(fullPath);
                var isCreated = CreateInitWCFProject(fullPath, out outputLines, out error);
                if (!isCreated)
                {
                    Directory.Delete(fullPath, true);
                }
                return isCreated;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static bool UpdateSourceCode(string prjFolderPath, out string error)
        {
            error = "";
            var sourceFilePath = Path.Combine(prjFolderPath, "Service.cs");
            if (File.Exists(sourceFilePath))
            {
                string sourceCode = File.ReadAllText(sourceFilePath);
                var winServiceName = "WCFWindowsService" + (new DirectoryInfo(prjFolderPath)).Name;
                sourceCode = sourceCode.Replace("WCFWindowsServiceSample", winServiceName);
                File.WriteAllText(sourceFilePath, sourceCode, Encoding.UTF8);
                return true;
            }
            else
            {
                error = "Исходный файл не найден в папке проекта: " + sourceFilePath;
                return false;
            }
        }
        private static bool UpdateAppConfig(string prjFolderPath, out string error)
        {
            error = "";
            var filePath = Path.Combine(prjFolderPath, "App.config");
            if (File.Exists(filePath))
            {
                var winServiceName = "WCFWindowsService" + (new DirectoryInfo(prjFolderPath)).Name;
                try
                {
                    var doc = XDocument.Load(filePath);
                    doc.Root.Element("system.serviceModel")
                        .Element("services")
                        .Element("service")
                        .Element("host")
                        .Element("baseAddresses")
                        .Element("add").SetAttributeValue("baseAddress", "http://localhost:9000/WCFServices/" + winServiceName);
                    doc.Save(filePath);
                    return true;
                }
                catch (Exception e)
                {
                    error = e.Message;
                    return false;
                }
            }
            else
            {
                error = "Файл конфигурации не найден в папке скопированного проекта: " + filePath;
                return false;
            }
        }
        public static bool CopyTemplateProject(string destPath, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            var command = string.Format(_copyFolder, _ServiceProjectTemplatePath, destPath);
            try
            {
                var isCopied = ExecuteCMD(command, out outputLines, out error);
                if (isCopied)
                {
                    if(!UpdateSourceCode(destPath, out error))
                    {
                        return false;
                    }
                    if (!UpdateAppConfig(destPath, out error))
                    {
                        return false;
                    }
                }
                return isCopied;
            }
            catch(Exception e)
            {
                error = e.Message + ", cmd: " + command;
                return false;
            }
        }

        public static string GetFullProjectPath(string prjName)
        {
            return Path.Combine(_MainFolderPath, _ServiceProjectsPath, prjName);
        }
        public static bool ExistPublishFiles(string prjName, out string error)
        {
            error = "";
            var publishPath = Path.Combine(GetFullProjectPath(prjName), "publish");
            var requiredFileTypes = new List<string>
            {
                "Tunduk{0}.deploy.cmd",
                "Tunduk{0}.deploy-readme.txt",
                "Tunduk{0}.SetParameters.xml",
                "Tunduk{0}.SourceManifest.xml",
                "Tunduk{0}.zip"
            };
            bool hasErrors = false;
            foreach(var requiredFile in requiredFileTypes)
            {
                var fileName = string.Format(requiredFile, prjName);
                var filePath = Path.Combine(publishPath, fileName);
                if (!File.Exists(filePath))
                {
                    hasErrors = true;
                    error += "Обязательный файл \"" + fileName + "\" отсутствует!";
                }
            }
            if (hasErrors)
            {
                error += "Расположение для публикаций: " + publishPath;
            }

            return !hasErrors;
        }
        public static bool IsPublished(string prjName)
        {
            return ExistPublishFiles(prjName, out string error);
        }
        public static readonly string _appcmdGetAppListCMD = @"c:\Windows\System32\inetsrv\appcmd.exe list app";
        public static bool IsDeployed(string prjName)
        {
            string IISAppName = "Tunduk" + prjName;

            if(ExecuteCMD(_appcmdGetAppListCMD, out List<string> outputLines, out string error))
            {
                foreach(var line in outputLines)
                {
                    if(line.Contains("Default Web Site/" + IISAppName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static string GetFullProjectPath(params string[] paths)
        {
            return Path.Combine(paths);
        }
        public static bool HasFile(string filePath)
        {
            return File.Exists(filePath);
        }
        public static bool BuildProject(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var prjFilePath = Path.Combine(_ServiceProjectsPath, prjName, "Service.csproj");
                if (File.Exists(prjFilePath))
                    return ExecuteCMD(string.Format(_buildProject, prjFilePath), out outputLines, out error);
                else
                {
                    error = "Путь к файлу проекта для построения не существует! Параметры: " + prjFilePath;
                    return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool RebuildProject(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var prjFilePath = Path.Combine(_ServiceProjectsPath, prjName, "Service.csproj");
                if (File.Exists(prjFilePath))
                    return ExecuteCMD(string.Format(_rebuildProject, prjFilePath), out outputLines, out error);
                else
                {
                    error = "Путь к файлу проекта для перепостроения не существует! Параметры: " + prjFilePath;
                    return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool InstallService(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var prjFilePath = Path.Combine(_ServiceProjectsPath, prjName, "bin\\service.exe");
                if (File.Exists(prjFilePath))
                    return ExecuteCMD(string.Format(_installServiceCMD, prjFilePath), out outputLines, out error);
                else
                {
                    error = "Путь к файлу процесса для установки сервиса не существует! Параметры: " + prjFilePath;
                    return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
        public static bool UninstallService(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var prjFilePath = Path.Combine(_ServiceProjectsPath, prjName, "bin\\service.exe");
                if (File.Exists(prjFilePath))
                    return ExecuteCMD(string.Format(_uninstallServiceCMD, prjFilePath), out outputLines, out error);
                else
                {
                    error = "Путь к файлу процесса для удаления сервиса не существует! Параметры: " + prjFilePath;
                    return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool StartService(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var winServiceName = "WCFWindowsService" + prjName;
                return ExecuteCMD(string.Format(_startServiceCMD, winServiceName), out outputLines, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
        public static bool StopService(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var winServiceName = "WCFWindowsService" + prjName;
                return ExecuteCMD(string.Format(_stopServiceCMD, winServiceName), out outputLines, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static void GetWinServiceInfo(string prjName, out bool isExist, out bool isStarted, out string error)
        {
            isExist = true;
            isStarted = false;
            error = "";
            var winServiceName = "WCFWindowsService" + prjName;
            ExecuteCMD(string.Format(_service_infoCMD, winServiceName), out List<string> outputLines, out error);

            var outputStr = string.Join("", outputLines);
            if(outputStr.Contains(_service_doesnt_exist_msg_en) || outputStr.Contains(_service_doesnt_exist_msg_ru))
            {
                isExist = false;
            }
            if (outputStr.Contains(_service_state_running_msg_en))
            {
                isStarted = true;
            }
            else if (outputStr.Contains(_service_state_stoped_msg_en))
            {
                isStarted = false;
            }
        }


        public static readonly string _generateFromWsdlCMD = "\"" + System.Configuration.ConfigurationManager.AppSettings["SvcUtilPath"] + "\" {0} http://x-road.eu/xsd/xroad.xsd /out:{1}\\IService.cs /config:{1}\\generated_from_wsdl.config_ignore /namespace:*,WCFService";
        private static bool UpdateWCFSourceCode(string prjFolderPath, out string error)
        {
            try
            {
                error = "";
                var serviceName = (new DirectoryInfo(prjFolderPath)).Name;
                var wsdlFilePath = Path.Combine(prjFolderPath, "wsdl\\mlsd_service.wsdl");
                if (File.Exists(wsdlFilePath))
                {
                    //+1) Set actual endpoint location in wsdl
                    var doc = XDocument.Load(wsdlFilePath);
                    XNamespace wsdl = "http://schemas.xmlsoap.org/wsdl/";
                    XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
                    doc.Root
                        .Element(wsdl + "service")
                        .Element(wsdl + "port")
                        .Element(soap + "address").SetAttributeValue("location", "http://10.1.4.5/Tunduk" + serviceName + "/Service.svc");
                    doc.Save(wsdlFilePath);
                }
                else
                {
                    error = "Файл-wsdl не найден в расположении: " + wsdlFilePath;
                    return false;
                }

                string wsdlCodePath = Path.Combine(prjFolderPath, "IService.cs");
                //+2) Generate interface from WSDL
                //+2.1) Set Valid output location
                //throw new Exception(string.Format(_generateFromWsdlCMD, wsdlFilePath, prjFolderPath));
                if (!ExecuteCMD(string.Format(_generateFromWsdlCMD, wsdlFilePath, prjFolderPath), out List<string> outputLines, out error))
                {
                    if (!(
                        error.Contains("http://www.w3.org/XML/1998/namespace:lang") &&
                        error.Contains("http://www.w3.org/XML/1998/namespace:space") &&
                        error.Contains("http://www.w3.org/XML/1998/namespace:base") &&
                        error.Contains("http://www.w3.org/XML/1998/namespace:id") &&
                        error.Contains("http://www.w3.org/XML/1998/namespace:specialAttrs") &&
                        outputLines.Contains(wsdlCodePath)
                        ))
                    {
                        //return false;
                    }
                    else
                    {
                        error = "";//skip unknown error
                    }
                }

                if (!File.Exists(wsdlCodePath))
                {
                    error = "Файл интерфейса сгенерированного из WSDL не найден в папке проекта: " + wsdlCodePath;
                    return false;
                }


                //+3) Overwrite Service.svc.cs with implementation from WSDL-interface
                var sourceFilePath = Path.Combine(prjFolderPath, "Service.svc.cs");
                if (!File.Exists(sourceFilePath))
                {
                    error = "Файл реализации интерфейса не найден в папке проекта: " + sourceFilePath;
                    return false;
                }
                string sourceCode = File.ReadAllText(sourceFilePath);
                string interfaceName = "mlsd_service";
                string implementTmpl = "WCFService.PersonDetailsResponse PersonDetails(WCFService.PersonDetailsRequest request)";
                string responseTypeName = implementTmpl.Trim().Split(' ')[0];
                var i = false;
                foreach (var lineStr in File.ReadAllLines(wsdlCodePath))
                {
                    if (i)
                    {
                        implementTmpl = lineStr.Replace(";", "");
                        break;
                    }
                    if (lineStr.Trim().StartsWith("public interface"))
                    {
                        interfaceName = lineStr.Replace("public interface", "").Trim();
                    }
                    if (lineStr.Trim() == "[System.ServiceModel.ServiceKnownTypeAttribute(typeof(XRoadIdentifierType))]")
                    {
                        i = true;
                    }
                }

                sourceCode = sourceCode.Replace("<INTERFACE_NAME_HERE>", interfaceName);
                sourceCode = sourceCode.Replace("<IMPLEMENT_HERE>", implementTmpl);
                sourceCode = sourceCode.Replace("<ResponseTypeNameHere>", responseTypeName);

                sourceCode = sourceCode.Replace("//", "");//uncomment 

                File.WriteAllText(sourceFilePath, sourceCode, Encoding.UTF8);

                //+4) Set ApplicationName in CustomProfile.pubxml
                var publishProfilePath = Path.Combine(prjFolderPath, "Properties\\PublishProfiles\\CustomProfile.pubxml");
                if (File.Exists(publishProfilePath))
                {
                    /*var doc = XDocument.Load(publishProfilePath);

                    if (doc.Root == null)
                        throw new Exception("<Project> is NULL: " + publishProfilePath);
                    
                    var propGroup = doc.Root.Element("PropertyGroup");
                    if (propGroup == null)
                        throw new Exception("<PropertyGroup> is NULL: " + publishProfilePath);
                    var loaction = propGroup.Element("DesktopBuildPackageLocation");

                    if (loaction == null)
                        throw new Exception("<DesktopBuildPackageLocation> is NULL: " + publishProfilePath);
                    loaction.Value = "publish\\Tunduk" + serviceName + ".zip";

                    var iisAppName = propGroup.Element("DeployIisAppPath");
                    if (iisAppName == null)
                        throw new Exception("<DeployIisAppPath> is NULL: " + publishProfilePath);
                    iisAppName.Value = "Default Web Site/Tunduk" + serviceName;
                    //Return blocked attribute
                    var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
                    doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                    var root = new XElement(ns + "Project", new XElement(ns + "a", "b"));
                    doc.Save(publishProfilePath);*/
                    var profileXmlString = File.ReadAllText(publishProfilePath);
                    profileXmlString = profileXmlString.Replace("<DesktopBuildPackageLocation>publish\\WCFService.zip</DesktopBuildPackageLocation>", "<DesktopBuildPackageLocation>publish\\Tunduk" + serviceName + ".zip</DesktopBuildPackageLocation>");
                    profileXmlString = profileXmlString.Replace("<DeployIisAppPath>Default Web Site/WCFService</DeployIisAppPath>", "<DeployIisAppPath>Default Web Site/Tunduk" + serviceName + "</DeployIisAppPath>");
                    File.WriteAllText(publishProfilePath, profileXmlString, Encoding.UTF8);
                }
                else
                {
                    error = "Файл профиля публикации не найден в папке скопированного проекта: " + publishProfilePath;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                error = e.Message + ", stacktrace: " + e.StackTrace;
                return false;
            }
        }
        public static bool CreateInitWCFProject(string destPath, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                var isCopied = true; ExecuteCMD(string.Format(_copyFolder, _WCFProjectTemplatePath, destPath), out outputLines, out error);
                if (isCopied)
                {
                    if (!UpdateWCFSourceCode(destPath, out error))
                    {
                        return false;
                    }
                }
                return isCopied;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static readonly string _publishCMD = @"msbuild {0} /p:DeployOnBuild=true /p:PublishProfile=CustomProfile";//{0} - path to WCFService.csproj
        public static bool PublishToPackage(string prjName, out List<string> outputLines, out string error)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                //Check requirements
                var prjPath = GetFullProjectPath(prjName);
                if (!ExistFolder(prjPath))
                {
                    error = "Путь к папке проекта не найден! Path: " + prjPath;
                    return false;
                }
                string prjFilePath = Path.Combine(prjPath, "WCFService.csproj");
                if(!File.Exists(prjFilePath))
                {
                    error = "Файл WCFService.csproj в папке проекта не найден! Path: " + prjFilePath;
                    return false;
                }
                string publishProfilePath = Path.Combine(prjPath, "Properties\\PublishProfiles\\CustomProfile.pubxml");
                if (!File.Exists(publishProfilePath))
                {
                    error = "Профиль публикации в папке проекта не найден! Path: " + publishProfilePath;
                    return false;
                }

                //Do publish to PrjFolder\publish
                return ExecuteCMD(string.Format(_publishCMD, prjFilePath), out outputLines, out error);
            }
            catch (Exception e)
            {
                error = e.Message + ", stacktrace: " + e.StackTrace;
                return false;
            }
        }
        public static readonly string _deployCMD = @"{0}\\Tunduk{1}.deploy.cmd /{2}";//{0} - path to publishFolder, {1} - ProjectName, {2} - verb T or Y
        public static bool DeployWCF(string prjName, out List<string> outputLines, out string error, bool isProduction = false)
        {
            error = "";
            outputLines = new List<string>();
            try
            {
                //Check requirements
                if(!ExistPublishFiles(prjName, out error))
                {
                    return false;
                }

                var publishPath = Path.Combine(GetFullProjectPath(prjName), "publish");
                if (!isProduction)
                    return ExecuteCMD(string.Format(_deployCMD, publishPath, prjName, "T"), out outputLines, out error);
                else
                {
                    if (ExecuteCMD(string.Format(_deployCMD, publishPath, prjName, "T"), out outputLines, out error))
                    {
                        outputLines = new List<string>();
                        error = "";
                        return ExecuteCMD(string.Format(_deployCMD, publishPath, prjName, "Y"), out outputLines, out error);
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message + ", stacktrace: " + e.StackTrace;
                return false;
            }
        }
    }
}
