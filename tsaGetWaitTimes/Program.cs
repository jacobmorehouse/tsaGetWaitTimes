using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data.SqlClient;
using System.Data;

namespace tsaGetWaitTimes
{
    class databaseOps
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["production"].ConnectionString;

        public static void listAirports(ref List<string> listOfAirports)
        {
            SqlConnection sqlConnection1 = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT shortcode FROM airports";
            cmd.CommandType = CommandType.Text;
            cmd.Connection = sqlConnection1;

            sqlConnection1.Open();
            reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    
                    string tempAirportCode = reader["shortcode"].ToString();
                    //Console.WriteLine("tempAirportCode: " + tempAirportCode);
                    listOfAirports.Add(tempAirportCode);
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            reader.Close();
            sqlConnection1.Close();
        }

    }

    class waitTime
    {
        public int checkpoint { get; set; }
        public string Created_Datetime { get; set; }
        public int thisWaitTime { get; set; }

        public waitTime(int checkpoint, int thisWaitTime, string Created_Datetime)
        {
            this.checkpoint = checkpoint;
            this.thisWaitTime = thisWaitTime;
            this.Created_Datetime = Created_Datetime;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var theUri = "http://apps.tsa.dhs.gov/MyTSAWebService/GetTSOWaitTimes.ashx?ap="; //needs the airport code after the =, such as SEA.
            XmlDocument xmlResult = new XmlDocument();
            List<string> listOfAirports = new List<string>();
            List<waitTime> listWaitTimes = new List<waitTime>();

            //first get a list of airport codes to look up
            databaseOps.listAirports(ref listOfAirports);

            foreach (var ap in listOfAirports)
            {
                try
                {
                    Console.WriteLine("I'm working on {0}", ap);
                    HttpWebRequest webReq = WebRequest.CreateHttp(theUri + ap);
                    webReq.CookieContainer = new CookieContainer();
                    webReq.Method = "GET";
                    webReq.UserAgent = "Something";
                    webReq.Referer = "https://www.tsa.gov";

                    WebResponse thisthis = webReq.GetResponse();
                    using (var streamReader = new StreamReader(thisthis.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        xmlResult.LoadXml(result);

                        //Console.WriteLine(xmlResult.SelectNodes("/WaitTimes")); //todo left off here

                        XmlNodeList xmlNodes = xmlResult.SelectNodes("/DocumentElement/WaitTimes");
                        foreach (XmlNode node in xmlNodes)
                        {
                            waitTime thisWait = new waitTime(
                                checkpoint: Int32.Parse(node["CheckpointIndex"].InnerText),
                                thisWaitTime: Int32.Parse(node["WaitTime"].InnerText),
                                Created_Datetime: node["Created_Datetime"].InnerText
                            );

                            Console.WriteLine("");
                            Console.WriteLine("Begin thisWait Object");
                            Console.WriteLine("checkpoint: {0}", thisWait.checkpoint);
                            Console.WriteLine("wait: {0}", thisWait.thisWaitTime);
                            Console.WriteLine("Created: {0}", thisWait.Created_Datetime);
                            Console.WriteLine("");

                            listWaitTimes.Add(thisWait);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("catch1 triggered: {0}", e.Message);
                }
            }

            //TODO left off here. Wait times are successfully stored in listWaitTimes.
            Console.ReadLine();
            

        }
    }
}
