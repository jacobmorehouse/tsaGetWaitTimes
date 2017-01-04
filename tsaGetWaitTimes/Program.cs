using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace tsaGetWaitTimes
{
    class databaseOps
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["production"].ConnectionString;

    }

    class Program
    {
        static void Main(string[] args)
        {
            var theUri = "http://apps.tsa.dhs.gov/MyTSAWebService/GetTSOWaitTimes.ashx?ap="; //needs the airport code after the =, such as SEA.
            XmlDocument xmlResult = new XmlDocument();

            
            //test3
            try
            {
                HttpWebRequest webReq = WebRequest.CreateHttp(theUri);
                webReq.CookieContainer = new CookieContainer();
                webReq.Method = "GET";
                webReq.UserAgent = "Something";
                webReq.Referer = "https://www.tsa.gov";

                WebResponse thisthis = webReq.GetResponse();
                using (var streamReader = new StreamReader(thisthis.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    xmlResult.LoadXml(result);
                    xmlResult.Save("c:\\users\\jmwin7\\Desktop\\tsaWaitTimesResult.txt"); //working!
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("catch1 triggered: {0}", e.Message);
            }

        }
    }
}
