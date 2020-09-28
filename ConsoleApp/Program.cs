using ConsoleApp.CalculatorReference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if(SendRequest("22310199070001", "AN", "4112312", out TempResult result, out string errorMessage))
            {
                Console.WriteLine("result: " + result.isSuccess + "," + (result.response.testPassportDataByPSNResponse.response));
            }
            else
            {
                Console.WriteLine("ERROR, " + errorMessage);
            }
            Console.ReadLine();
        }
        public class TempResult
        {
            private bool _isSuccess;
            public bool isSuccess
            {
                get
                {
                    return _isSuccess && string.IsNullOrEmpty(response.testPassportDataByPSNResponse.response.faultcode) && string.IsNullOrEmpty(response.testPassportDataByPSNResponse.response.faultstring);
                }
                set
                {
                    _isSuccess = value;
                }
            }
            private string _errorMessage;
            public string errorMessage
            {
                get
                {
                    return string.Format("{0}, faultcode: {1}, faultstring: {2}", _errorMessage, response.testPassportDataByPSNResponse.response.faultcode, response.testPassportDataByPSNResponse.response.faultstring);
                }
                set
                {
                    _errorMessage = value;
                }
            }
            public _response response { get; set; }
            public class _response
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
            }
        }
        public static bool SendRequest(string pin, string series, string number, out TempResult result, out string errorMessage)
        {
            errorMessage = "";
            result = new TempResult();
            try
            {
                string webAddr = "http://195.38.189.101:8088/ServiceConstructor2/SoapClient/SendRequest2";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(
                        new
                        {
                            request =
                            new
                            {
                                testPassportDataByPSN =
                                new
                                {
                                    request =
                                    new
                                    {
                                        pin,
                                        series,
                                        number
                                    }
                                }
                            },
                            clientId = new Guid("8d8461a4-9d3e-4136-98a7-66697078371d")
                        });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var responseText = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    responseText = streamReader.ReadToEnd();
                }
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<TempResult>(responseText);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.GetBaseException() + ", trace: " + e.StackTrace;
                return false;
            }
        }
    }
}
