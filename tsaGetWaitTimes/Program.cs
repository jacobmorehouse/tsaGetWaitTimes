using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
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

        public static void insertWaitTime(waitTime iwaitTime)
        {
            SqlConnection sqlConnection1 = new SqlConnection(connectionString);
            SqlCommand cmd1 = new SqlCommand();
            
            cmd1.CommandText = String.Format(@"
                IF NOT EXISTS(SELECT * FROM waittimes WHERE airportcode = '{0}' and checkpointindex = '{1}' and waittime = '{2}' and tsadatecreated = '{3}')
                INSERT INTO waittimes 
                    (airportcode, checkpointindex, waittime, tsadatecreated) 
                values ('{0}','{1}','{2}','{3}')
                ", iwaitTime.airportCode, iwaitTime.checkpoint, iwaitTime.thisWaitTime, iwaitTime.Created_Datetime.Substring(0, iwaitTime.Created_Datetime.Length - 6));

            cmd1.CommandType = CommandType.Text;
            cmd1.Connection = sqlConnection1;

            sqlConnection1.Open();
            cmd1.ExecuteNonQuery();
            sqlConnection1.Close();
        }
    }

    class waitTime
    {
        public string airportCode { get; set; }
        public int checkpoint { get; set; }
        public string Created_Datetime { get; set; }
        public int thisWaitTime { get; set; }

        public waitTime(string airportCode, int checkpoint, int thisWaitTime, string Created_Datetime)
        {
            this.airportCode = airportCode;
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
                        
                        XmlNodeList xmlNodes = xmlResult.SelectNodes("/DocumentElement/WaitTimes");
                        foreach (XmlNode node in xmlNodes)
                        {
                            waitTime thisWait = new waitTime(
                                airportCode: ap,
                                checkpoint: Int32.Parse(node["CheckpointIndex"].InnerText),
                                thisWaitTime: Int32.Parse(node["WaitTime"].InnerText),
                                Created_Datetime: node["Created_Datetime"].InnerText
                            );

                            databaseOps.insertWaitTime(thisWait);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("catch1 triggered: {0}", e.Message);
                }
            }
        }
    }
}
