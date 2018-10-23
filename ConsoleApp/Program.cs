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
            //CalculatorClient client = new CalculatorClient();

            //Console.WriteLine(client.Add(1, 2));
            // Используйте переменную "client", чтобы вызвать операции из службы.

            // Всегда закройте клиент.
            //client.Close();

            if(SendRequest("22209198800515", "AN", "4106420", out TempResult result, out string errorMessage))
            {
                Console.WriteLine("OK, " + result.testPassportDataByPSNResponse.response.surname);
            }
            else
            {
                Console.WriteLine("ERROR, " + errorMessage);
            }
            Console.ReadLine();
        }
        public class InputParam
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public InputParam[] Include { get; set; }
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
                }
            }
        }
        public static bool SendRequest(string pin, string series, string number, out TempResult result, out string errorMessage)
        {
            errorMessage = "";
            result = new TempResult();
            var inParams = new InputParam[]
            {
                new InputParam
                {
                    Name = "request",
                    Include = new InputParam[]
                    {
                        new InputParam
                        {
                            Name = "pin",
                            Value = pin
                        },
                        new InputParam
                        {
                            Name = "series",
                            Value = series
                        },
                        new InputParam
                        {
                            Name = "number",
                            Value = number
                        }
                    }
                }
            };
            try
            {
                string webAddr = "http://195.38.189.101:8088/ServiceConstructor/SoapClient/SendRequest";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { InputParams = inParams, clientId = new Guid("8d8461a4-9d3e-4136-98a7-66697078371d") });

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
