using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace BoomTrader_2
{
    public class Request
    {
        public static string GET(string Url, string Data = "")
        {
            try
            {
                WebRequest req = WebRequest.Create(Url + "?" + Data);
                req.Method = "GET";
                req.ContentType = "application/x-www-form-urlencoded";
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string Out = sr.ReadToEnd();
                sr.Close();
                return Out;
            }
            catch (WebException ex)
            {
                //StreamReader sr = new StreamReader(ex.Response.GetResponseStream());

                //MainWindow.Instance.addLog(string.Format(Lang.RequestError,Url,Data), save: true, color: "Red");
                return ex.Message;
            }

        }

        public static string Ping(string address)
        {
            try
            {
                Ping p1 = new Ping();
                PingReply PR = p1.Send(address);
                return PR.RoundtripTime.ToString() + " ms";
            }
            catch (PingException)
            {
                //MainWindow.Instance.addLog(e.Message);
                return "Fail";
            }


        }
        public static JObject GetJSON(string Url, string Data = "")
        {
            return JObject.Parse(GET(Url, Data));
        }

        public static string POST(string Url, string Data)
        {
            WebRequest req = System.Net.WebRequest.Create(Url);
            req.Method = "POST";
            req.Timeout = 100000;
            req.ContentType = "application/x-www-form-urlencoded";
            byte[] sentData = Encoding.GetEncoding("utf-8").GetBytes(Data);
            req.ContentLength = sentData.Length;
            Stream sendStream = req.GetRequestStream();
            sendStream.Write(sentData, 0, sentData.Length);
            sendStream.Close();
            System.Net.WebResponse res = req.GetResponse();
            Stream ReceiveStream = res.GetResponseStream();
            StreamReader sr = new StreamReader(ReceiveStream, Encoding.UTF8);
            //Кодировка указывается в зависимости от кодировки ответа сервера
            Char[] read = new Char[256];
            int count = sr.Read(read, 0, 256);
            string Out = String.Empty;
            while (count > 0)
            {
                String str = new String(read, 0, count);
                Out += str;
                count = sr.Read(read, 0, 256);
            }
            return Out;
        }
    }
}
