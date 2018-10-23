using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using WEB.Models;
using WEB.Models.Entities;
using WEB.SOAP.Clients;

namespace WEB.Controllers
{
    public class SoapClientController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult GetRequiredParams(string wsdlUrl, string methodName)
        {
            var result = SOAPClient.GetRequiredParams(wsdlUrl, methodName, out List<SOAP.Parameter> inputParams, out List<SOAP.Parameter> outputParams, out string errorMessage);
            return Json(new { result, inputParams = InitParamTypes(inputParams), outputParams = InitParamTypes(outputParams), errorMessage }, JsonRequestBehavior.AllowGet);
        }
        private static long? GetSizeOfObjectInBytes(object item)
        {
            if (item == null) return 0;
            try
            {
                // hackish solution to get an approximation of the size
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    MaxDepth = 10,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var formatter = new JsonMediaTypeFormatter { SerializerSettings = jsonSerializerSettings };
                using (var stream = new MemoryStream())
                {
                    formatter.WriteToStreamAsync(item.GetType(), item, stream, null, null).GetAwaiter().GetResult();
                    return stream.Length / 4; // 32 bits per character = 4 bytes per character
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        [HttpPost]
        public ActionResult SendRequest(InputParam[] InputParams, Guid clientId)
        {
            var log = new ReceiveHistoryItem
            {
                ClientId = clientId,
                EntryTime = DateTime.Now,
                InputSize = 0,
                OutputSize = 0,
                IsSuccess = false
            };
            try
            {
                if (InputParams != null)
                {
                    log.InputSize = GetSizeOfObjectInBytes(InputParams) ?? 0;
                    var soapInputParams = new Dictionary<string, object>();
                    InitParams(InputParams, soapInputParams);

                    var obj = db.ClientDetails.Find(clientId);
                    var soap_request = new SOAPClient.SOAPEnvelope
                    {
                        Header = new SOAPClient.SOAPEnvelope.SOAPHeader
                        {
                            service = new SOAPClient.SOAPEnvelope.SOAPHeader.SOAPService
                            {
                                memberClass = obj.s_memberClass,
                                memberCode = obj.s_memberCode,
                                objectType = obj.s_objectType,
                                subsystemCode = obj.s_subsystemCode,
                                xRoadInstance = obj.s_xRoadInstance,
                                serviceCode = obj.s_serviceCode,
                                serviceVersion = obj.s_serviceVersion
                            },
                            client = new SOAPClient.SOAPEnvelope.SOAPHeader.SOAPClient
                            {
                                memberClass = obj.c_memberClass,
                                memberCode = obj.c_memberCode,
                                objectType = obj.c_objectType,
                                subsystemCode = obj.c_subsystemCode,
                                xRoadInstance = obj.c_xRoadInstance
                            },
                            id = obj.id1,
                            issue = obj.issue,
                            protocolVersion = obj.protocolVersion,
                            userId = obj.userId
                        },
                        Body = new SOAPClient.SOAPEnvelope.SOAPBody
                        {

                        }
                    };
                    var result = SOAPClient.Execute(soapInputParams, obj.TargetNamespace, obj.ServiceURL, obj.WsdlUrl, obj.MethodName, soap_request, out SOAPClient.SOAPEnvelope soap_response, out string errorMessage);
                    if (result)
                    {
                        string jsonText = JsonConvert.SerializeXmlNode((XmlElement)soap_response.Body.Content);

                        jsonText = jsonText/*.Substring(0, jsonText.Length - 1)*/.Replace("\\", "").Replace("@", "");
                        var str = jsonText.Substring(0, 15);
                        var endIndx = jsonText.IndexOf(":" + obj.MethodName);
                        if (endIndx != -1)
                        {
                            endIndx++;
                            var startIndx = jsonText.IndexOf("{\"") + 2;
                            var termStr = jsonText.Substring(startIndx, endIndx - startIndx);
                            var termStr2 = string.Format(":{0}", termStr.Substring(0, (termStr.Length - 1)));
                            jsonText = jsonText.Replace(termStr, "").Replace(termStr2, "");// + string.Format("[{0}][{1}][{2}][{3}][{4}]", termStr, termStr2, startIndx, endIndx, str);
                        }

                        //jsonText = jsonText.Replace("\"", "").Replace("@", "");
                        //jsonText = jsonText.Replace("http", "\"http").Replace(".ee", ".ee\"");
                        var data = JsonConvert.DeserializeObject<TempResult>(jsonText);
                        //return Json(new { result, soapInputParams, data, jsonText }, JsonRequestBehavior.AllowGet);
                        if (GetFaultString((XmlElement)soap_response.Body.Content, out string faultCode, out string faultString))
                        {
                            throw new ApplicationException(string.Format("Error from remote-server! FaultCode: {0}, FaultString: {1}", faultCode, faultString));
                        }
                        log.IsSuccess = true;
                        log.OutputSize = GetSizeOfObjectInBytes(soap_response.Body.Content) ?? 0;
                        return Json(data, JsonRequestBehavior.AllowGet);
                    }
                    else
                        throw new ApplicationException(errorMessage);
                }
                else
                    throw new ApplicationException("InputParams is Empty! Please fill out InputParams!");
            }
            catch(Exception e)
            {
                log.IsSuccess = false;
                log.ErrorMessage = e.GetBaseException().Message;
                return Json(new { result = false, errorMessage = e.GetBaseException().Message + ", trace: " + e.StackTrace }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                log.OperationDuration = (DateTime.Now - log.EntryTime).TotalMilliseconds;
                db.ReceiveHistoryItems.Add(log);
                db.SaveChanges();
            }
        }
        public class TempResult
        {
            public _personDetailsResponse PersonDetailsResponse { get; set; }
            public class _personDetailsResponse
            {
                public string xmlns { get; set; }
                public _request request { get; set; }
                public _response response { get; set; }
                public class _response
                {
                    public bool Result { get; set; }
                    public string ErrorMessage { get; set; }
                    public _PaymentPeriod PaymentPeriod { get; set; }
                    public class _PaymentPeriod
                    {
                        public string StartDate { get; set; }
                        public string EndDate { get; set; }
                    }
                }
                public class _request
                {
                    public string PIN { get; set; }
                    public int Month { get; set; }
                    public int Year { get; set; }
                }
            }
            public _testPassportDataByPSNResponse testPassportDataByPSNResponse { get; set; }
            public class _testPassportDataByPSNResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string pin { get; set; }
                    public string series { get; set; }
                    public string number { get; set; }
                }
                public class _response
                {
                    public string pin { get; set; }
                    public string surname { get; set; }
                    public string name { get; set; }
                    public string patronymic { get; set; }
                    public string nationality { get; set; }
                    public string dateOfBirth { get; set; }
                    public string passportSeries { get; set; }
                    public string passportNumber { get; set; }
                    public string voidStatus { get; set; }
                    public string issuedDate { get; set; }
                    public string passportAuthority { get; set; }
                    public string expiredDate { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }
        }
        private void InitParams(InputParam[] inputParams, Dictionary<string, object> soapInputParams)
        {
            foreach (var prm in inputParams)
            {
                if (prm.Value != null)
                {
                    soapInputParams.Add(prm.Name, prm.Value);
                }
                else
                {
                    var subSoapInputParams = new Dictionary<string, object>();
                    InitParams(prm.Include, subSoapInputParams);
                    soapInputParams.Add(prm.Name, subSoapInputParams);
                }
            }
        }
        private Dictionary<string, object> InitParamTypes(List<SOAP.Parameter> paramList, Dictionary<string, object> soapInputParams = null)
        {
            if (soapInputParams == null) soapInputParams = new Dictionary<string, object>();
            foreach (var prm in paramList)
            {
                if (prm.Type != "")
                {
                    soapInputParams.Add(prm.Name, prm.Type);
                }
                else
                {
                    var subSoapInputParams = new Dictionary<string, object>();
                    InitParamTypes(prm.Children, subSoapInputParams);
                    soapInputParams.Add(prm.Name, subSoapInputParams);
                }
            }
            return soapInputParams;
        }
        private bool GetFaultString(XmlElement obj, out string faultCode, out string faultString)
        {
            faultCode = "";
            faultString = "";
            foreach (XmlNode el in obj.GetElementsByTagName("faultcode"))
            {
                faultCode += el.InnerText;
            }
            foreach (XmlNode el in obj.GetElementsByTagName("faultstring"))
            {
                faultString = el.InnerText;
            }
            return !(string.IsNullOrEmpty(faultCode) || string.IsNullOrEmpty(faultString));
        }
        public class InputParam
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public InputParam[] Include { get; set; }
        }
    }
}