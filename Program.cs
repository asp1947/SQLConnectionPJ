using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;

namespace ConsoleApp2
{
    internal class Program
    {

        static void Main(string[] args)
        {
            String my_key = "my_key";
            String station_st = "여의도";

            String query = "http://swopenAPI.seoul.go.kr/api/subway/" + my_key + "/json/realtimeStationArrival/0/5/" + station_st;

            SQLserverquery testconnect = new SQLserverquery();

            WebRequest wr = WebRequest.Create(query);
            wr.Method = "GET";

            WebResponse wrs = wr.GetResponse();
            Stream s = wrs.GetResponseStream();
            StreamReader sr = new StreamReader(s);

            string response = sr.ReadToEnd();

            JObject realtime = JObject.Parse(response);

            int[] subwayId = new int[5]; //열차 호선
            string[] updnLine = new string[5]; // 상하행
            string[] trainLineNm = new string[5]; //도착지 방면
            string[] btrainSttus = new string[5];
            string[] arvlMsg2 = new string[5];
            string statnNm = null;
            int countcheck = 0;
            string codecheck = null;
            string codecheck_none = null;
            string message = null;

            try
            {
                codecheck = realtime["errorMessage"]["code"].ToString();
            } catch (Exception ex)
            {
                codecheck_none = realtime["code"].ToString();
                message = realtime["message"].ToString();
                Console.WriteLine(codecheck_none + ": " +  message);
                Console.WriteLine("역에 도착하는 열차가 없습니다. 절차를 생략하고 종료 단계로 들어갑니다.");
                goto Enforcementquit;

            }
            
            countcheck = Int32.Parse(realtime["errorMessage"]["total"].ToString());
            statnNm = realtime["realtimeArrivalList"][0]["statnNm"].ToString();


            if (countcheck >= 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    subwayId[i] = Int32.Parse(realtime["realtimeArrivalList"][i]["subwayId"].ToString());
                    updnLine[i] = realtime["realtimeArrivalList"][i]["updnLine"].ToString();
                    trainLineNm[i] = realtime["realtimeArrivalList"][i]["trainLineNm"].ToString();
                    arvlMsg2[i] = realtime["realtimeArrivalList"][i]["arvlMsg2"].ToString();

                    if (realtime["realtimeArrivalList"][i]["btrainSttus"] != null)
                    {
                        btrainSttus[i] = realtime["realtimeArrivalList"][i]["btrainSttus"].ToString();
                    }
                    else
                    {
                        btrainSttus[i] = null;
                    }
                }

            }
            else if (countcheck == 0)
            {
                Console.WriteLine(statnNm + "역에 도착하는 열차가 없습니다. 절차를 생략하고 종료 단계로 들어갑니다.");
                goto Enforcementquit;
            }

            else
            {
                for (int i = 0; i < countcheck; i++)
                {
                    subwayId[i] = Int32.Parse(realtime["realtimeArrivalList"][i]["subwayId"].ToString());
                    updnLine[i] = realtime["realtimeArrivalList"][i]["updnLine"].ToString();
                    trainLineNm[i] = realtime["realtimeArrivalList"][i]["trainLineNm"].ToString();
                    arvlMsg2[i] = realtime["realtimeArrivalList"][i]["arvlMsg2"].ToString();

                    if (realtime["realtimeArrivalList"][i]["btrainSttus"] != null)
                    {
                        btrainSttus[i] = realtime["realtimeArrivalList"][i]["btrainSttus"].ToString();
                    }
                    else
                    {
                        btrainSttus[i] = null;
                    }
                }
            }

            Console.WriteLine(subwayId[0]);
            Console.WriteLine(updnLine[0]);
            Console.WriteLine(trainLineNm[0]);
            Console.WriteLine(arvlMsg2[0]);
            Console.WriteLine(btrainSttus[0]);

            testconnect.connect(subwayId, updnLine, trainLineNm, arvlMsg2, btrainSttus, countcheck);

            Enforcementquit:
            Console.Write("Press to exit... ");
            while (Console.ReadKey().Key != ConsoleKey.Enter)
            { }

        }
    }

    public class SQLserverquery
    {
        public void connect(int[] a1, string[] a2, string[] a3, string[] a4, string[] a5, int b1)
        {
         
            try {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "Server Address";
                builder.UserID = "ID";
                builder.Password = "PW";
                builder.InitialCatalog = "DataBase Name";
               
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    string inittable = "truncate table REALTIMEARRIVAL";
                    string sql = "insert into REALTIMEARRIVAL(subwayID, updnLine, trainlinenm, arvlMsg2, btrainSttus) values(@subwayID, @updnLine, @trainlinenm, @arvlMsg2, @btrainSttus)";
                    
                    connection.Open();

                    using (SqlCommand init = new SqlCommand(inittable, connection))
                    {
                        
                        int initresult = init.ExecuteNonQuery();

                        Console.WriteLine("truncate complete!");
                    }
                    
                    
                    Console.WriteLine("connection open.");
                    if (b1 >= 5)
                    {
                        for (int i = 0; i < 5; i++)
                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@subwayID", a1[i]);
                                command.Parameters.AddWithValue("@updnLine", a2[i]);
                                command.Parameters.AddWithValue("@trainlinenm", a3[i]);
                                command.Parameters.AddWithValue("@arvlMsg2", a4[i]);
                                command.Parameters.AddWithValue("@btrainSttus", a5[i]);

                                int result = command.ExecuteNonQuery();
                                

                                if (result < 0)
                                    Console.WriteLine("Error inserting data into DB!!!");

                                Console.WriteLine("insert complete!");
                            }

                    }

                    else if (b1 >= 1 || b1 <= 4)
                    {
                        for (int i = 0; i < b1; i++)
                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                //참조, truncate 해서 입력할때마다 테이블 값을 매번 날려야 함.
                                command.Parameters.AddWithValue("@subwayID", a1[i]);
                                command.Parameters.AddWithValue("@updnLine", a2[i]);
                                command.Parameters.AddWithValue("@trainlinenm", a3[i]);
                                command.Parameters.AddWithValue("@arvlMsg2", a4[i]);
                                command.Parameters.AddWithValue("@btrainSttus", a5[i]);

                                int result = command.ExecuteNonQuery();
                                
                                if (result < 0)
                                    Console.WriteLine("Error inserting data into DB!!!");

                                Console.WriteLine("insert complete!");
                            }
                    }
                    connection.Close();
                    Console.WriteLine("connection close.");
                }
            } catch (Exception ex) { 
                Console.WriteLine("could not connect DB Server!!!");
                Console.WriteLine(ex);
            };


        }
        

        

    }
}
