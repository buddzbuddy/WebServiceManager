using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WEB.SOAP.Clients
{
    public static class SOAPClient
    {

        [XmlRoot(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", ElementName = "Envelope")]
        public class SOAPEnvelope
        {
            [XmlElement(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SOAPHeader Header;
            [XmlElement(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SOAPBody Body { get; set; }

            public class SOAPHeader
            {
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public SOAPClient client;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public SOAPService service;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public string userId;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public string id;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public string requestHash;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public string issue;
                [XmlElement(Namespace = "http://x-road.eu/xsd/xroad.xsd")]
                public string protocolVersion;

                public class SOAPClient
                {
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string xRoadInstance;
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string memberClass;
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string memberCode;
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string subsystemCode;
                    [XmlAttribute(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string objectType;
                }
                public class SOAPService : SOAPClient
                {
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string serviceCode;
                    [XmlElement(Namespace = "http://x-road.eu/xsd/identifiers")]
                    public string serviceVersion;
                }
            }
            public class SOAPBody
            {
                [XmlAnyElement]
                public object Content { get; set; }
            }
        }
        static void RetrieveSOAPResponse<T>(T obj, string tab = "")
        {
            Console.WriteLine("--->");
            foreach (var property in typeof(T).GetProperties())
            {
                if (new Type[] { typeof(int), typeof(string), typeof(DateTime), typeof(double), typeof(decimal), typeof(float), typeof(Guid), typeof(long) }.Contains(property.PropertyType))
                {
                    Console.WriteLine(string.Format("{0}{1}{2}:{3}", Environment.NewLine, tab, property.Name, property.GetValue(obj)));
                }
                else
                {
                    RetrieveSOAPResponse(property.GetValue(obj), tab + "\t");
                }
            }
            Console.WriteLine("<---");
        }

        public static bool GetRequiredParams(string wsdlUrl, string methodName, out List<Parameter> inputParams, out List<Parameter> outputParams, out string errorMessage)
        {
            inputParams = new List<Parameter>();
            outputParams = new List<Parameter>();
            errorMessage = "";
            try
            {
                WsdlParser.ServiceDescriptionSpike(out WebServiceInfo webServiceInfo, wsdlUrl);
                var method = webServiceInfo.WebMethods.FirstOrDefault(x => x.Name == methodName);
                if (method != null)
                {
                    inputParams = method.InputParameters.ToList();
                    outputParams = method.OutputParameters.ToList();
                    return true;
                }
                else
                {
                    errorMessage = string.Format("Method of \"{0}\" not found! WsdlURL: {1}", methodName, wsdlUrl);
                    return false;
                }
            }
            catch(Exception e)
            {
                errorMessage = e.GetBaseException().Message;
                return false;
            }
        }
        static void CreateSoapParams(XmlDocument document, XmlElement xmlSoapParam, List<Parameter> parameters, Dictionary<string, object> paramList, string tns)
        {
            foreach (var p in parameters)
            {
                if (paramList.ContainsKey(p.Name))
                {
                    var newXmlSoapParam = document.CreateElement(p.Name, tns);
                    if (p.Children.Count > 0)
                    {
                        if(paramList[p.Name] is Dictionary<string, object>)
                        {
                            CreateSoapParams(document, newXmlSoapParam, p.Children, (Dictionary<string, object>)paramList[p.Name], tns);
                        }
                        else
                        {
                            throw new FormatException("The type param of '" + p.Name + "' isn't 'Dictionary<string, object>'. Dictionary<string, object> expected.");
                        }
                    }
                    else
                    {
                        newXmlSoapParam.InnerText = paramList[p.Name].ToString();
                    }
                    xmlSoapParam.AppendChild(newXmlSoapParam);
                }
                else
                    throw new ArgumentNullException("The param of '" + p.Name + "' doesn't exist in the given paramList");
            }
        }
        public static bool Execute(Dictionary<string, object> inputParams, string tns, string serviceUrl, string wsdlUrl, string methodName, SOAPEnvelope soap_request, out SOAPEnvelope soap_response, out string errorMessage)
        {
            errorMessage = "";
            soap_response = new SOAPEnvelope();
            try
            {
                WsdlParser.ServiceDescriptionSpike(out WebServiceInfo webServiceInfo, wsdlUrl);

                HttpWebRequest request = CreateWebRequest(serviceUrl);
                
                var soap_request_xml = SerializeToXmlDocument(soap_request, tns);

                foreach (var webMethod in webServiceInfo.WebMethods.Where(x => x.Name == methodName))
                {
                    var xmlSoapMethod = soap_request_xml.CreateElement(webMethod.Name, tns);
                    CreateSoapParams(soap_request_xml, xmlSoapMethod, webMethod.InputParameters.ToList(), inputParams, tns);
                    soap_request_xml.DocumentElement.GetElementsByTagName("Body")[0].AppendChild(xmlSoapMethod);
                }

                //soap_request_xml.Load("D:\\soap_request.xml");
                using (Stream stream = request.GetRequestStream())
                {
                    soap_request_xml.Save(stream);
                    //soap_request_xml.Save("D:\\soap_request.xml");
                    XmlSerializer serializer = new XmlSerializer(typeof(SOAPEnvelope), tns);
                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                        {
                            soap_response = (SOAPEnvelope)serializer.Deserialize(rd);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                errorMessage = string.Format("{0}({2}), {1}", e.GetBaseException().Message, e.StackTrace, serviceUrl);
                return false;
            }
        }
        public static XmlDocument SerializeToXmlDocument(object input, string targetNamespace)
        {
            XmlSerializer ser = new XmlSerializer(input.GetType());

            XmlDocument xd = null;

            using (MemoryStream memStm = new MemoryStream())
            {
                ser.Serialize(memStm, input);

                memStm.Position = 0;

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true
                };

                using (var xtr = XmlReader.Create(memStm, settings))
                {
                    xd = new XmlDocument();
                    xd.Load(xtr);
                }
            }



            return xd;
        }
        public static HttpWebRequest CreateWebRequest(string serviceUrl)
        {
            if (string.IsNullOrEmpty(serviceUrl))
                serviceUrl = System.Configuration.ConfigurationManager.AppSettings["TUNDUK_HOST"];
            var webRequest = (HttpWebRequest)WebRequest.Create(serviceUrl);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
    }
}