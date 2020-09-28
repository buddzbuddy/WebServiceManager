using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Web.Services.Description;
using System.Net;
using System.Xml.Schema;
using System.IO;

namespace WEB.SOAP
{
    public static class WsdlParser
    {
        public static void ServiceDescriptionSpike(out WebServiceInfo webServiceInfo, string url)
        {
            //url = "http://10.1.4.33/wsdl?xRoadInstance=central-server&memberClass=GOV&memberCode=70000005&subsystemCode=passport-service&serviceCode=testPassportDataByPSN&version=v1";//"http://localhost/TundukFOMS2/wsdl/doc.wsdl";//

            webServiceInfo = WebServiceInfo.OpenWsdl(new Uri(url));
            //if(withConsole)
            //{
            //    Console.WriteLine(string.Format("WebService: {0}", webServiceInfo.Url));

            //    foreach (WebMethodInfo method in webServiceInfo.WebMethods)
            //    {
            //        Console.WriteLine(string.Format("\tWebMethod: {0}", method.Name));
            //        Console.WriteLine("\t\tInput Parameters");
            //        ShowInConsole(method.InputParameters);

            //        Console.WriteLine("\t\tOutput Parameters");

            //        ShowInConsole(method.OutputParameters);
            //    }
            //}
        }
        static string MultipleTab(int count)
        {
            string t = "\t";
            for(int i = 0; i < count; i++)
            {
                t += "\t";
            }
            return t;
        }
        static void ShowInConsole(Parameter[] parameters, int count = 3)
        {
            foreach (Parameter parameter in parameters)
            {
                Console.WriteLine(string.Format("{0}{1} {2}", MultipleTab(count), parameter.Name, parameter.Type));
                ShowInConsole(parameter.Children.ToArray(), count + 1);
            }
        }
    }
    /// <summary>
    /// Information about a web service
    /// </summary>
    public class WebServiceInfo
    {
        static Dictionary<string, WebServiceInfo> _webServiceInfos = new Dictionary<string, WebServiceInfo>();
        private static Types types;
        private static string tns;

        /// <summary>
        /// Constructor creates the web service info from the given url.
        /// </summary>
        /// <param name="url">
        private WebServiceInfo(Uri url)
        {
            Url = url ?? throw new ArgumentNullException("url");
            WebMethods = GetWebServiceDescription(url);
        }

        /// <summary>
        /// Factory method for WebServiceInfo. Maintains a hashtable WebServiceInfo objects
        /// keyed by url in order to cache previously accessed wsdl files.
        /// </summary>
        /// <param name="url">
        /// <returns></returns>
        public static WebServiceInfo OpenWsdl(Uri url)
        {
            WebServiceInfo webServiceInfo;
            if (!_webServiceInfos.TryGetValue(url.ToString(), out webServiceInfo))
            {
                webServiceInfo = new WebServiceInfo(url);
                _webServiceInfos.Add(url.ToString(), webServiceInfo);
            }
            return webServiceInfo;
        }

        /// <summary>
        /// Convenience overload that takes a string url
        /// </summary>
        /// <param name="url">
        /// <returns></returns>
        public static WebServiceInfo OpenWsdl(string url)
        {
            Uri uri = new Uri(url);
            return OpenWsdl(uri);
        }

        /// <summary>
        /// Load the WSDL file from the given url.
        /// Use the ServiceDescription class to walk the wsdl and create the WebServiceInfo
        /// instance.
        /// </summary>
        /// <param name="url">
        private WebMethodInfoCollection GetWebServiceDescription(Uri url)
        {
            UriBuilder uriBuilder = new UriBuilder(url);
            //uriBuilder.Query = "WSDL";

            WebMethodInfoCollection webMethodInfos = new WebMethodInfoCollection();

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Method = "GET";
            webRequest.Accept = "text/xml";

            ServiceDescription serviceDescription;

            using (WebResponse response = webRequest.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                serviceDescription = ServiceDescription.Read(stream);
            }

            foreach (PortType portType in serviceDescription.PortTypes)
            {
                foreach (Operation operation in portType.Operations)
                {
                    string operationName = operation.Name;
                    string inputMessageName = operation.Messages.Input.Message.Name;
                    string outputMessageName = operation.Messages.Output.Message.Name;

                    // get the message part
                    string inputMessagePartName = serviceDescription.Messages[inputMessageName].Parts[0].Element.Name;
                    string outputMessagePartName = serviceDescription.Messages[outputMessageName].Parts[0].Element.Name;

                    // get the parameter name and type
                    Parameter[] inputParameters = GetParameters(serviceDescription, inputMessagePartName);
                    Parameter[] outputParameters = GetParameters(serviceDescription, outputMessagePartName);

                    WebMethodInfo webMethodInfo = new WebMethodInfo(operation.Name, inputParameters, outputParameters);
                    webMethodInfos.Add(webMethodInfo);
                }
            }
            return webMethodInfos;
        }

        /// <summary>
        /// Walk the schema definition to find the parameters of the given message.
        /// </summary>
        /// <param name="serviceDescription">
        /// <param name="messagePartName">
        /// <returns></returns>
        private static Parameter[] GetParameters(ServiceDescription serviceDescription, string messagePartName)
        {
            List<Parameter> parameters = new List<Parameter>();

            types = serviceDescription.Types;
            tns = serviceDescription.TargetNamespace;
            foreach (XmlSchema xmlSchema in types.Schemas)
            {
                foreach (var item in xmlSchema.Items)
                {
                    XmlSchemaElement schemaElement = item as XmlSchemaElement;
                    InitElement(schemaElement, messagePartName, parameters);
                }
            }


            return parameters.ToArray();
        }

        static void InitElement(XmlSchemaElement schemaElement, string messagePartName, List<Parameter> parameters, Parameter parent = null)
        {
            if (schemaElement != null)
            {
                if (schemaElement.Name == messagePartName)
                {
                    if (schemaElement.SchemaType is XmlSchemaComplexType complexType)
                    {
                        XmlSchemaParticle particle = complexType.Particle;
                        if (particle is XmlSchemaSequence sequence)
                        {
                            foreach (var childObj in sequence.Items)
                            {
                                XmlSchemaElement childElement = null;
                                if (childObj is XmlSchemaChoice)
                                {
                                    childElement = (XmlSchemaElement)((XmlSchemaChoice)childObj).Items[0];
                                }
                                else
                                {
                                    childElement = (XmlSchemaElement)childObj;
                                }

                                string parameterName = childElement.Name;
                                string parameterType = childElement.SchemaTypeName.Name;

                                var parameter = new Parameter(parameterName, parameterType, parent);
                                if (parent == null)
                                    parameters.Add(parameter);

                                if (childElement.SchemaTypeName.Namespace == tns)
                                {
                                    var obj = types.Schemas.Find(childElement.SchemaTypeName, typeof(XmlSchemaComplexType));
                                    if (obj != null && obj is XmlSchemaComplexType childComplexType)
                                    {
                                        InitComplexType(complexType, parameterName, parameters, parameter);
                                    }
                                    else
                                        throw new ApplicationException(string.Format("InitElement1: ComplexType not found. childElement: {0}, childElement.SchemaTypeName: {1}", childElement.Name, childElement.SchemaTypeName));
                                }
                                else
                                {
                                    InitElement(childElement, parameterName, parameters, parameter);
                                }
                            }
                        }
                    }
                    else if (schemaElement.SchemaTypeName.Namespace == tns && schemaElement.SchemaTypeName != null)
                    {
                        var linkedType = types.Schemas.Find(schemaElement.SchemaTypeName, typeof(XmlSchemaComplexType));
                        if (linkedType != null)
                            InitComplexType((XmlSchemaComplexType)linkedType, messagePartName, parameters, parent);
                        else //SimpleType
                        {
                            string parameterName = schemaElement.Name;
                            string parameterType = "string";

                            var parameter = new Parameter(parameterName, parameterType, parent);
                            if (parent == null)
                                parameters.Add(parameter);
                        }
                        //else
                            //throw new ApplicationException(string.Format("InitElement2: ComplexType not found. childElement: {0}, childElement.SchemaTypeName: {1}", schemaElement.Name, schemaElement.SchemaTypeName));
                    }
                }
            }
        }

        static void InitComplexType(XmlSchemaComplexType complexType, string messagePartName, List<Parameter> parameters, Parameter parent = null)
        {
            XmlSchemaParticle particle = complexType.Particle;
            if (particle is XmlSchemaSequence sequence)
            {
                foreach (var childObj in sequence.Items)
                {
                    XmlSchemaElement childElement = null;
                    if (childObj is XmlSchemaChoice)
                    {
                        childElement = (XmlSchemaElement)((XmlSchemaChoice)childObj).Items[0];
                    }
                    else
                    {
                        childElement = (XmlSchemaElement)childObj;
                    }

                    string parameterName = childElement.Name;
                    string parameterType = childElement.SchemaTypeName.Name;

                    if (childElement.SchemaTypeName.Namespace == tns)
                    {
                        var obj = types.Schemas.Find(childElement.SchemaTypeName, typeof(XmlSchemaComplexType));
                        if (obj != null && obj is XmlSchemaComplexType childComplexType)
                        {
                            InitComplexType(childComplexType, parameterName, parameters, parent);
                        }
                        else//SimpleType
                        {
                            parameterType = "string";
                            var parameter = new Parameter(parameterName, parameterType, parent);
                            InitElement(childElement, parameterName, parameters, parameter);
                        }
                        //else
                        //throw new ApplicationException(string.Format("InitComplexType: ComplexType not found. childElement: {0}, childElement.SchemaTypeName: {1}", childElement.Name, childElement.SchemaTypeName));
                    }
                    else
                    {
                        var parameter = new Parameter(parameterName, parameterType, parent);
                        if (parent == null)
                            parameters.Add(parameter);
                        //InitElement(childElement, parameterName, parameters, parameter);
                    }
                }
            }
        }

        /// <summary>
        /// WebMethodInfo
        /// </summary>
        public WebMethodInfoCollection WebMethods { get; } = new WebMethodInfoCollection();

        /// <summary>
        /// Url
        /// </summary>
        public Uri Url { get; set; }
    }

    /// <summary>
    /// Information about a web service operation
    /// </summary>
    public class WebMethodInfo
    {
        string _name;
        Parameter[] _inputParameters;
        Parameter[] _outputParameters;

        /// <summary>
        /// OperationInfo
        /// </summary>
        public WebMethodInfo(string name, Parameter[] inputParameters, Parameter[] outputParameters)
        {
            _name = name;
            _inputParameters = inputParameters;
            _outputParameters = outputParameters;
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// InputParameters
        /// </summary>
        public Parameter[] InputParameters
        {
            get { return _inputParameters; }
        }

        /// <summary>
        /// OutputParameters
        /// </summary>
        public Parameter[] OutputParameters
        {
            get { return _outputParameters; }
        }
    }

    /// <summary>
    /// A collection of WebMethodInfo objects
    /// </summary>
    public class WebMethodInfoCollection : KeyedCollection<string, WebMethodInfo>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public WebMethodInfoCollection() : base() { }

        protected override string GetKeyForItem(WebMethodInfo webMethodInfo)
        {
            return webMethodInfo.Name;
        }
    }

    /// <summary>
    /// represents a parameter (input or output) of a web method.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">
        /// <param name="type">
        public Parameter(string name, string type, Parameter parent = null)
        {
            this.Name = name;
            this.Type = type;
            Children = new List<Parameter>();
            if(parent != null)
            {
                parent.Children.Add(this);
            }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Children
        /// </summary>
        public List<Parameter> Children { get; set; }
    }
}
