using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using System.Xml.Linq;
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
            var result = new SOAPClient().GetRequiredParams(wsdlUrl, methodName, out List<SOAP.Parameter> inputParams, out List<SOAP.Parameter> outputParams, out string errorMessage);
            return Json(new { result, inputParams = new { clientId = Guid.Empty, InputParams = InitInputParams(inputParams) }, outputParams = InitOutputParams(outputParams, inputParams), errorMessage }, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult GetRequiredParams2(Guid clientId)
        {
            var clientObj = db.ClientDetails.Find(clientId);
            if (!string.IsNullOrEmpty(clientObj.RequestXML))
            {
                string soap_request = clientObj.RequestXML;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(soap_request);
                var body_element = doc.DocumentElement.LastChild.LastChild;
                string jsonRequest = JsonConvert.SerializeXmlNode(JsonConvert.DeserializeXmlNode(JsonConvert.SerializeXmlNode(body_element)));
                return Json(new { result = true, inputParams = jsonRequest }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false, errorMessage = "RequestXML is not found!" }, JsonRequestBehavior.AllowGet);
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
        public class JsonDynamicWrapper
        {
            public dynamic clientId { get; set; }
            public dynamic request { get; set; }
            public dynamic orgName { get; set; }
        }
        [AllowAnonymous]
        [HttpPost]
        [AllowCrossSiteJson]
        public ActionResult SendRequest2(JsonDynamicWrapper json)
        {
            var log = new ReceiveHistoryItem
            {
                EntryTime = DateTime.Now,
                InputSize = 0,
                OutputSize = 0,
                IsSuccess = false
            };
            var clientResult = new SoapClientResult
            {
                isSuccess = false
            };
            try
            {
                if (json.request == null) throw new ApplicationException("\"request\" not found!");
                if (json.clientId == null) throw new ApplicationException("\"clientId\" not found!");
                if (json.orgName != null) log.OrgName = json.orgName;

                var clientObj = db.ClientDetails.Find(Guid.Parse((string)json.clientId));
                log.ClientId = clientObj.Id;

                string jsonString = JsonConvert.SerializeObject(json.request);
                var srcBody_method = JsonConvert.DeserializeXmlNode(jsonString);

                XmlDocument orig_soap_request = new XmlDocument();
                orig_soap_request.LoadXml(clientObj.RequestXML);

                //TODO: Init headers
                var orig_header = orig_soap_request.DocumentElement.FirstChild;
                XmlNode newHeader = InitXRoadHeader(orig_soap_request, clientObj);
                orig_soap_request.DocumentElement.ReplaceChild(newHeader, orig_header);

                var orig_body_method = orig_soap_request.DocumentElement.LastChild.LastChild;
                if(srcBody_method.DocumentElement == null) throw new ApplicationException("Required param of name \"" + orig_body_method.LocalName + "\" is not found!");
                var newBody_method = InitXmlNode(orig_soap_request, orig_body_method.Name, orig_body_method.NamespaceURI, orig_body_method.NodeType, orig_body_method.ChildNodes, srcBody_method.DocumentElement.ChildNodes);


                orig_soap_request.DocumentElement.LastChild.ReplaceChild(newBody_method, orig_body_method);
                //orig_soap_request.Save("E:\\soap_request_from_json.xml");

                log.InputSize = GetSizeOfObjectInBytes(srcBody_method) ?? 0;
                var soapClient = new SOAPClient();
                var result = soapClient.ExecuteXML(ConfigurationManager.AppSettings["TUNDUK_HOST"], orig_soap_request, out XmlDocument soap_response, out string errorMessage);
                /*
                 * string errorMessage = "";
                 * XmlDocument soap_response = new XmlDocument();
                soap_response.Load("E:\\patent_soap_response.xml");*/
                if (result)
                {
                    var response_body = soap_response.DocumentElement.LastChild.LastChild;
                    XElement response_body_WithoutNs = RemoveAllNamespaces(XElement.Parse(response_body.OuterXml));
                    var response_body_without_ns = new XmlDocument();
                    response_body_without_ns.LoadXml(response_body_WithoutNs.ToString());
                    string jsonResponse = JsonConvert.SerializeXmlNode(JsonConvert.DeserializeXmlNode(JsonConvert.SerializeXmlNode(response_body_without_ns.FirstChild)));
                    
                    log.IsSuccess = true;
                    log.OutputSize = GetSizeOfObjectInBytes(response_body) ?? 0;
                    var jobj = JObject.Parse(jsonResponse);
                    var rObj = new RouteValueDictionary(jobj);
                    clientResult.response = System.Web.Helpers.Json.Decode<Dictionary<string, object>>(jsonResponse); ;//JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonResponse);//System.Web.Helpers.Json.Decode(jsonResponse));//JsonConvert.DeserializeObject(jsonResponse);
                    clientResult.isSuccess = true;
                }
                else
                    throw new ApplicationException(errorMessage);
            }
            catch (Exception e)
            {
                log.IsSuccess = false;
                log.ErrorMessage = e.GetBaseException().Message;
                clientResult.isSuccess = false;
                clientResult.errorMessage = e.GetBaseException().Message + ", trace: " + e.StackTrace;
            }
            finally
            {
                log.OperationDuration = (DateTime.Now - log.EntryTime).TotalMilliseconds;
                db.ReceiveHistoryItems.Add(log);
                db.SaveChanges();
            }
            return Json(clientResult, JsonRequestBehavior.AllowGet);
        }

        private XmlNode InitXRoadHeader(XmlDocument orig_soap_request, ClientDetail clientObj)
        {
            var h = new SOAPClient.SOAPEnvelope.SOAPHeader
            {
                protocolVersion = clientObj.ServiceCode.Subsystem.ProtocolVersion,
                service = new SOAPClient.SOAPEnvelope.SOAPHeader.SOAPService
                {
                    memberClass = clientObj.ServiceCode.Subsystem.TundukOrganization.MemberClass,
                    memberCode = clientObj.ServiceCode.Subsystem.TundukOrganization.MemberCode,
                    objectType = ConfigurationManager.AppSettings["s_objectType"],
                    subsystemCode = clientObj.ServiceCode.Subsystem.Name,
                    xRoadInstance = clientObj.ServiceCode.Subsystem.TundukOrganization.XRoadInstance,
                    serviceCode = clientObj.ServiceCode.Name,
                    serviceVersion = clientObj.ServiceCode.Version
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
            };
            string soapenvNsURI = "http://schemas.xmlsoap.org/soap/envelope/";
            string soapenvPrefix = orig_soap_request.GetPrefixOfNamespace(soapenvNsURI);
            string xroNsURI = "http://x-road.eu/xsd/xroad.xsd";
            string xroPrefix = orig_soap_request.GetPrefixOfNamespace(xroNsURI);
            string idenNsURI = "http://x-road.eu/xsd/identifiers";
            string idenPrefix = orig_soap_request.GetPrefixOfNamespace(idenNsURI);

            var userIdNode = orig_soap_request.CreateElement(xroPrefix, "userId", xroNsURI);
            userIdNode.InnerText = h.userId;

            var s_objectTypeAttr = orig_soap_request.CreateAttribute(idenPrefix, "objectType", idenNsURI);
            s_objectTypeAttr.Value = h.service.objectType;
            var s_xRoadInstance = orig_soap_request.CreateElement(idenPrefix, "xRoadInstance", idenNsURI);
            s_xRoadInstance.InnerText = h.service.xRoadInstance;
            var s_memberClass = orig_soap_request.CreateElement(idenPrefix, "memberClass", idenNsURI);
            s_memberClass.InnerText = h.service.memberClass;
            var s_memberCode = orig_soap_request.CreateElement(idenPrefix, "memberCode", idenNsURI);
            s_memberCode.InnerText = h.service.memberCode;
            var s_subsystemCode = orig_soap_request.CreateElement(idenPrefix, "subsystemCode", idenNsURI);
            s_subsystemCode.InnerText = h.service.subsystemCode;
            var s_serviceCode = orig_soap_request.CreateElement(idenPrefix, "serviceCode", idenNsURI);
            s_serviceCode.InnerText = h.service.serviceCode;
            var s_serviceVersion = orig_soap_request.CreateElement(idenPrefix, "serviceVersion", idenNsURI);
            s_serviceVersion.InnerText = h.service.serviceVersion;

            var serviceNode = orig_soap_request.CreateElement(xroPrefix, "service", xroNsURI);
            //Setup service values
            serviceNode.SetAttributeNode(s_objectTypeAttr);
            serviceNode.AppendChild(s_xRoadInstance);
            serviceNode.AppendChild(s_memberClass);
            serviceNode.AppendChild(s_memberCode);
            serviceNode.AppendChild(s_subsystemCode);
            serviceNode.AppendChild(s_serviceCode);
            if(!string.IsNullOrEmpty(h.service.serviceVersion))
                serviceNode.AppendChild(s_serviceVersion);

            var protocolVersionNode = orig_soap_request.CreateElement(xroPrefix, "protocolVersion", xroNsURI);
            protocolVersionNode.InnerText = h.protocolVersion;
            var issueNode = orig_soap_request.CreateElement(xroPrefix, "issue", xroNsURI);
            issueNode.InnerText = h.issue;
            var idNode = orig_soap_request.CreateElement(xroPrefix, "id", xroNsURI);
            idNode.InnerText = h.id;

            var c_objectTypeAttr = orig_soap_request.CreateAttribute(idenPrefix, "objectType", idenNsURI);
            c_objectTypeAttr.Value = h.client.objectType;
            var c_xRoadInstance = orig_soap_request.CreateElement(idenPrefix, "xRoadInstance", idenNsURI);
            c_xRoadInstance.InnerText = h.client.xRoadInstance;
            var c_memberClass = orig_soap_request.CreateElement(idenPrefix, "memberClass", idenNsURI);
            c_memberClass.InnerText = h.client.memberClass;
            var c_memberCode = orig_soap_request.CreateElement(idenPrefix, "memberCode", idenNsURI);
            c_memberCode.InnerText = h.client.memberCode;
            var c_subsystemCode = orig_soap_request.CreateElement(idenPrefix, "subsystemCode", idenNsURI);
            c_subsystemCode.InnerText = h.client.subsystemCode;

            var clientNode = orig_soap_request.CreateElement(xroPrefix, "client", xroNsURI);
            //Setup client values
            clientNode.SetAttributeNode(c_objectTypeAttr);
            clientNode.AppendChild(c_xRoadInstance);
            clientNode.AppendChild(c_memberClass);
            clientNode.AppendChild(c_memberCode);
            clientNode.AppendChild(c_subsystemCode);

            var headerNode = orig_soap_request.CreateElement(soapenvPrefix, "Header", soapenvNsURI);
            //Setup Header values
            headerNode.AppendChild(userIdNode);
            headerNode.AppendChild(serviceNode);
            headerNode.AppendChild(protocolVersionNode);
            headerNode.AppendChild(issueNode);
            headerNode.AppendChild(idNode);
            headerNode.AppendChild(clientNode);

            return headerNode;
        }

        private XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                /*foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);
                */
                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Where(x => !string.IsNullOrEmpty(x.Value)).Select(el => RemoveAllNamespaces(el)));
        }
        private XmlNode InitXmlNode(XmlDocument orig_soap_request, string elemName, string nsURI, XmlNodeType xmlNodeType, XmlNodeList origNodeList, XmlNodeList srcNodeList)
        {
            XmlNode newElem = orig_soap_request.CreateNode(xmlNodeType, elemName, nsURI);
            foreach (XmlNode origElem in origNodeList)
            {
                if (origElem.NodeType != XmlNodeType.Element && origElem.NodeType != XmlNodeType.Text) continue;
                var srcElem = srcNodeList.Cast<XmlNode>().FirstOrDefault(x => x.NodeType == origElem.NodeType && x.LocalName == origElem.LocalName);
                if (srcElem == null) throw new ApplicationException("Required param of name \"" + origElem.LocalName + "\" is not found!");
                if (origElem.HasChildNodes && srcElem.HasChildNodes)
                {
                    newElem.AppendChild(InitXmlNode(orig_soap_request, origElem.Name, origElem.NamespaceURI, origElem.NodeType, origElem.ChildNodes, srcElem.ChildNodes));
                }
                else if (origElem.HasChildNodes == srcElem.HasChildNodes)
                {
                    var subElem = orig_soap_request.CreateNode(origElem.NodeType, origElem.Name, origElem.NamespaceURI);
                    if (srcElem.InnerText == "&NULL") continue;
                    subElem.InnerText = srcElem.InnerText;
                    newElem.AppendChild(subElem);
                }
                else throw new ApplicationException("Required param of name \"" + origElem.LocalName + "\" is empty!");
            }
            return newElem;
        }
        public class SoapClientResult
        {
            public bool isSuccess { get; set; }
            public object response { get; set; }
            public string errorMessage { get; set; }
        }
        public class ResponseTypes
        {
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
                    public string PaymentTypeName { get; set; }
                    public decimal PaymentSize { get; set; }
                    /*public string LastName { get; set; }
                    public string FirstName { get; set; }
                    public string MiddleName { get; set; }*/
                    public bool IsActive { get; set; }
                    public _arrayOfString Dependants { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }

            public class _arrayOfString
            {
                public string[] item { get; set; }
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
                    public _arrayOfString NewPINs { get; set; }
                    public _arrayOfString ExpiredPINs { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
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
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
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
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }

            public _MSECDetailsResponse MSECDetailsResponse { get; set; }
            public class _MSECDetailsResponse
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string PIN { get; set; }
                }
                public class _response
                {
                    public string OrganizationName { get; set; }
                    public DateTime ExaminationDate { get; set; }
                    public string ExaminationType { get; set; }
                    public string DisabilityGroup { get; set; }
                    public DateTime From { get; set; }
                    public DateTime To { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }

            public _SavePaymentF10Response SavePaymentF10Response { get; set; }
            public class _SavePaymentF10Response
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string OrderPaymentId { get; set; }
                    public decimal Amount { get; set; }
                    public DateTime PayDate { get; set; }
                }
                public class _response
                {
                    public string PaymentF10Id { get; set; }
                    public string faultcode { get; set; }
                    public string faultstring { get; set; }
                }
            }

            public _SaveNotPaymentF20Response SaveNotPaymentF20Response { get; set; }
            public class _SaveNotPaymentF20Response
            {
                public _request request { get; set; }
                public _response response { get; set; }
                public class _request
                {
                    public string OrderPaymentId { get; set; }
                    public DateTime RegDate { get; set; }
                    public string ReasonId { get; set; }
                }
                public class _response
                {
                    public string PaymentF20Id { get; set; }
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