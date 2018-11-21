using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using WEB.Models;
using WEB.Models.Entities;
using WEB.SOAP.Clients;

namespace WEB.Controllers
{
    [Authorize]
    public class SoapClientController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult GetRequiredParams(string wsdlUrl, string methodName)
        {
            var result = SOAPClient.GetRequiredParams(wsdlUrl, methodName, out List<SOAP.Parameter> inputParams, out List<SOAP.Parameter> outputParams, out string errorMessage);
            return Json(new { result, inputParams = new { clientId = Guid.Empty, InputParams = InitInputParams(inputParams) }, outputParams = InitOutputParams(outputParams, inputParams), errorMessage }, JsonRequestBehavior.AllowGet);
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
        [AllowAnonymous]
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
            string src = "";
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
                            protocolVersion = obj.ServiceCode.Subsystem.ProtocolVersion,
                            service = new SOAPClient.SOAPEnvelope.SOAPHeader.SOAPService
                            {
                                memberClass = obj.ServiceCode.Subsystem.TundukOrganization.MemberClass,
                                memberCode = obj.ServiceCode.Subsystem.TundukOrganization.MemberCode,
                                objectType = ConfigurationManager.AppSettings["s_objectType"],
                                subsystemCode = obj.ServiceCode.Subsystem.Name,
                                xRoadInstance = obj.ServiceCode.Subsystem.TundukOrganization.XRoadInstance,
                                serviceCode = obj.ServiceCode.Name,
                                serviceVersion = obj.ServiceCode.Version
                            },
                            client = new SOAPClient.SOAPEnvelope.SOAPHeader.SOAPClient
                            {
                                memberClass = ConfigurationManager.AppSettings["c_memberClass"],
                                memberCode = ConfigurationManager.AppSettings["c_memberCode"],
                                objectType = ConfigurationManager.AppSettings["c_objectType"],
                                subsystemCode = ConfigurationManager.AppSettings["c_subsystemCode"],
                                xRoadInstance = ConfigurationManager.AppSettings["c_xRoadInstance"]
                            },
                            id = "mlsd-system-id",
                            issue = "mlsd-system-issue",
                            userId = "mlsd-system-user"
                        },
                        Body = new SOAPClient.SOAPEnvelope.SOAPBody
                        {

                        }
                    };
                    var result = SOAPClient.Execute(soapInputParams, obj.ServiceCode.Subsystem.TargetNamespace, ConfigurationManager.AppSettings["TUNDUK_HOST"], obj.ServiceCode.WsdlPath, obj.ServiceCode.Name, soap_request, out SOAPClient.SOAPEnvelope soap_response, out string errorMessage);
                    if (result)
                    {
                        string jsonText = JsonConvert.SerializeXmlNode((XmlElement)soap_response.Body.Content);

                        jsonText = jsonText/*.Substring(0, jsonText.Length - 1)*/.Replace("\\", "").Replace("@", "");
                        var str = jsonText.Substring(0, 15);
                        var endIndx = jsonText.IndexOf(":" + obj.ServiceCode.Name);
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
                        src = jsonText;
                        var data = JsonConvert.DeserializeObject<TempResult>(jsonText);
                        //return Json(new { result, soapInputParams, data, jsonText }, JsonRequestBehavior.AllowGet);
                        if (GetFaultString((XmlElement)soap_response.Body.Content, out string faultCode, out string faultString))
                        {
                            throw new ApplicationException(string.Format("Error from remote-server! FaultCode: {0}, FaultString: {1}", faultCode, faultString));
                        }
                        log.IsSuccess = true;
                        log.OutputSize = GetSizeOfObjectInBytes(soap_response.Body.Content) ?? 0;
                        var serializedData = typeof(TempResult).GetProperty(obj.ServiceCode.Name + "Response").GetValue(data);
                        return Json(serializedData, JsonRequestBehavior.AllowGet);
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
                return Json(new { result = false, errorMessage = e.GetBaseException().Message + ", trace: " + e.StackTrace + ", src: " + src }, JsonRequestBehavior.AllowGet);
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

            public _GetActivePaymentsByPINResponse GetActivePaymentsByPINResponse { get; set; }
            public class _GetActivePaymentsByPINResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string PIN { get; set; }
                }
                public class _response
                {
                    public DateTime StartDate { get; set; }
                    public DateTime EndDate { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }

            public _GetNewOldRecipientsByYearMonthResponse GetRecipientsResponse { get; set; }
            public class _GetNewOldRecipientsByYearMonthResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public int Year { get; set; }
                    public int Month { get; set; }
                }
                public class _response
                {
                    public _array NewPINs { get; set; }
                    public _array ExpiredPINs { get; set; }
                    public class _array
                    {
                        public string[] item { get; set; }
                    }
                }
            }

            public _testAsbAddressResponse testAsbAddressResponse { get; set; }
            public class _testAsbAddressResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string pin { get; set; }
                }
                public class _response
                {
                    private bool _error = false;
                    public bool error
                    {
                        get
                        {
                            return _error;
                        }
                        set
                        {
                            if (value)
                            {
                                //throw new ApplicationException(address);
                            }
                            _error = value;
                        }
                    }
                    public string address { get; set; }
                }
            }

            public _testZagsDataByPinResponse testZagsDataByPinResponse { get; set; }
            public class _testZagsDataByPinResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string pin { get; set; }
                    public bool? datetimeFormat { get; set; }
                }
                public class _response
                {
                    public string pin { get; set; }
                    public DateTime pinGenerationDate { get; set; }
                    public string surname { get; set; }
                    public string name { get; set; }
                    public string patronymic { get; set; }
                    public string gender { get; set; }
                    public DateTime dateOfBirth { get; set; }
                    public string maritalStatus { get; set; }
                    public int? maritalStatusId { get; set; }
                    public string nationality { get; set; }
                    public int? nationalityId { get; set; }
                    public string citizenship { get; set; }
                    public int? citizenshipId { get; set; }
                    public string pinBlocked { get; set; }
                    public DateTime? deathDate { get; set; }
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

        private List<InputParam> InitInputParams(List<SOAP.Parameter> inParams, List<InputParam> inputParams = null)
        {
            if (inputParams == null) inputParams = new List<InputParam>();
            foreach (var prm in inParams)
            {
                if (prm.Children == null || prm.Children.Count == 0)
                {
                    inputParams.Add(new InputParam { Name = prm.Name, Value = prm.Type });
                }
                else
                {
                    var subInputParams = new List<InputParam>();
                    InitInputParams(prm.Children, subInputParams);
                    inputParams.Add(new InputParam { Name = prm.Name, Value = prm.Type, Include = subInputParams.ToArray() });
                }
            }
            return inputParams;
        }

        private Dictionary<string, object> InitOutputParams(List<SOAP.Parameter> paramList, List<SOAP.Parameter> requestParams, Dictionary<string, object> soapParams = null)
        {
            if (soapParams == null) soapParams = new Dictionary<string, object>();

            foreach (var prm in paramList)
            {
                if (prm.Children == null || prm.Children.Count == 0)
                {
                    soapParams.Add(prm.Name, prm.Type);
                }
                else
                {
                    if (prm.Name == "request")
                    {
                        prm.Children = requestParams[0].Children;
                    }
                    else if (prm.Name == "response")
                    {
                        var chFiltered = new List<SOAP.Parameter>();
                        prm.Children.ForEach(x =>
                        {
                            if(!requestParams[0].Children.Any(x1 => x1.Name == x.Name))
                            {
                                chFiltered.Add(x);
                            }
                        });
                        prm.Children = chFiltered;
                    }
                    var subSoapParams = new Dictionary<string, object>();
                    InitParamTypes(prm.Children, subSoapParams);
                    soapParams.Add(prm.Name, subSoapParams);
                }
            }
            return soapParams;
        }

        private Dictionary<string, object> InitParamTypes(List<SOAP.Parameter> paramList, Dictionary<string, object> soapParams = null)
        {
            if (soapParams == null) soapParams = new Dictionary<string, object>();
            foreach (var prm in paramList)
            {
                if (prm.Children == null || prm.Children.Count == 0)
                {
                    soapParams.Add(prm.Name, prm.Type);
                }
                else
                {
                    var subSoapParams = new Dictionary<string, object>();
                    InitParamTypes(prm.Children, subSoapParams);
                    soapParams.Add(prm.Name, subSoapParams);
                }
            }
            return soapParams;
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