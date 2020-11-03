using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BoomTrader_2.Monitor
{
    class MonitorServer
    {

        private static IPAddress remoteIPAddress;
        private static int remotePort;
        private static int localPort;
        private static Thread tRec;
        private static object gate = new object();
        static bool isCancellationRequested = true;
        private static UdpClient receivingUdpClient = null;
        [STAThread]
        public static void Start()
        {
            try
            {
                // Получаем данные, необходимые для соединения
                //Log.Add("Укажите локальный порт" , System.Drawing.Color.Black );
                localPort = Convert.ToInt32(49001);

                //Log.Add("Укажите удаленный порт", System.Drawing.Color.Black);
                remotePort = Convert.ToInt32(49000);

                //Log.Add("Укажите удаленный IP-адрес", System.Drawing.Color.Black);
                remoteIPAddress = IPAddress.Parse("127.0.0.1");

                // Создаем поток для прослушивания
                tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();

                //while (true)
                //{
                //   Send(Console.ReadLine());
                //}
            }
            catch (Exception ex)
            {
                Log.Add("Exception: " + ex.Message, System.Drawing.Color.Orange);
            }
        }

        private static void Send(string datagram)
        {
            // Создаем UdpClient
            UdpClient sender = new UdpClient();

            // Создаем endPoint по информации об удаленном хосте
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);

            try
            {
                // Преобразуем данные в массив байтов
                byte[] bytes = Encoding.UTF8.GetBytes(datagram);

                // Отправляем данные
                sender.Send(bytes, bytes.Length, endPoint);
            }
            catch (Exception ex)
            {
                Log.Add("Exception: " + ex.Message, System.Drawing.Color.Orange);
            }
            finally
            {
                // Закрыть соединение
                sender.Close();
            }
        }

        public static void Receiver()
        {
            // Создаем UdpClient для чтения входящих данных
            bool opened = false;

            while (!opened)
            {
                try { receivingUdpClient = new UdpClient(localPort); opened = true; }
                catch { localPort++; }

            }


            IPEndPoint RemoteIpEndPoint = null;

            try
            {


                while (isCancellationRequested)
                {



                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(
                       ref RemoteIpEndPoint);

                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    switch (returnData.ToString())
                    {
                        case "hello":
                            Send(Hello());
                            break;
                        case "stop":
                            MainForm.Instance.StopBot();
                            break;
                        case "start":
                            MainForm.Instance.StartBot();
                            break;
                        case "hide":
                            MainForm.Instance.BeginInvoke((Action)(() =>
                            {
                                MainForm.Instance.Hide();
                            }));
                            break;
                        case "show":
                            MainForm.Instance.BeginInvoke((Action)(() =>
                            {

                                MainForm.Instance.Show();
                                MainForm.Instance.WindowState = System.Windows.Forms.FormWindowState.Normal;
                                MainForm.Instance.Activate();
                            }));
                            break;
                        case "settings":
                            Send(GetSettings());
                            break;

                    }



                }
            }
            catch (SocketException)
            {

            }
            catch (Exception ex)
            {
                //if (ex.InnerException == )
                Log.Add("Exception: " + ex.Message, System.Drawing.Color.Orange);
            }
        }

        public static string GetSettings()
        {
            string path = Directory.GetCurrentDirectory();
            var dir = path.Split("\\")[^1];
            TraderBot b = TraderBot.Instance;
            string hello = dir;
            /*string hello = "{\"settings\":" +
                //"{\"volume\":\"" + b.Cfg.volume +
                "\",\"multiplier\":\"" + MainForm.Instance.Multiplier +
                //"\",\"leverage\":\"" + b.Cfg.leverage +
                "\",\"average-pnl\":\"" + TraderBot.Instance.AveragingSpread +
                "\",\"spread\":\"" + TraderBot.Instance.Spread +
                "\",\"long-pnl\":\"" + TraderBot.Instance.BuyPNL +
                "\",\"short-pnl\":\"" + TraderBot.Instance.SellPNL +
                "\",\"unrealized\":\"" + TraderBot.Instance.UnrealizedPnL +
                "\",\"close\":\"" + TraderBot.Instance.CloseProfit +
                "\",\"folder\":\"" + dir +

                ",\"port\":\"" + localPort +
                "\"}}";
            */

            return hello;

        }
        public static void Stop()
        {

            isCancellationRequested = false;
            if (receivingUdpClient != null)
                receivingUdpClient.Close();
            receivingUdpClient = null;


        }
        public static bool isStarted()
        {
            bool status = false;
            if (remoteIPAddress != null)
                status = true;
            return status;
        }
        public static void LogSend(string message)
        {
            try
            {

                string path = Directory.GetCurrentDirectory();
                var dir = path.Split("\\")[^1];
                var date = DateTime.Now.ToString();
                string json = "{\"log\":{\"message\":\"" + message + "\",\"date\":\"" + date.Replace("/", "-") + "\",\"port\":\"" + localPort + "\",\"name\":\"" + dir + "\"}}";
                Send(json);
            }
            catch (Exception)
            {

            }


        }



        private static string Hello()
        {
            string path = Directory.GetCurrentDirectory();
            var dir = path.Split("\\")[^1];
            bool window = MainForm.Instance.Visible;// ? true : false;

            string hello = "{\"hello\":" +
                "{\"folder\":\"" + dir +
                "\",\"path\":\"" + path.Replace("\\", "\\\\") +
                "\",\"calculated\":\"" + TraderBot.Instance.CalculatedPnL +
                "\",\"average-pnl\":\"" + TraderBot.Instance.AveragingSpread +
                "\",\"spread\":\"" + TraderBot.Instance.Spread +
                "\",\"long-pnl\":\"" + TraderBot.Instance.BuyPNL +
                "\",\"short-pnl\":\"" + TraderBot.Instance.SellPNL +
                "\",\"unrealized\":\"" + TraderBot.Instance.UnrealizedPnL +
                "\",\"close\":\"" + TraderBot.Instance.CloseProfit +
                "\",\"status\":\"" + TraderBot.Instance.Status +
                "\",\"window\":\"" + window +
                "\",\"wallet\":\"" + TraderBot.Instance.GetWallet() +
                "\",\"balances\":" +
                    "{\"usdt\":\"" + TraderBot.Instance.UsdtBalance +
                    "\",\"available\":\"" + TraderBot.Instance.AvailableBalance +
                    "\",\"margin\":\"" + TraderBot.Instance.marginBalance +
                    "\",\"bnb\":\"" + TraderBot.Instance.BnbBalance + "\"}" +
                ",\"port\":\"" + localPort +
                "\"}}";


            return hello;
        }

    }

}

