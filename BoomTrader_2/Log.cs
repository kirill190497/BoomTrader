using System;
using System.IO;
using System.Windows.Forms;
//using System.Windows.Controls;
//using System.Windows.Media;

namespace BoomTrader_2
{
    public static class Log
    {
        private static MainForm mf = MainForm.Instance;
        private static string Prev = "";
        private static System.Drawing.Color PrevColor = System.Drawing.Color.Empty;
        private static int PrevPos = 0;

        public static void Add(string message, System.Drawing.Color color, bool save = true, bool baloon = false, bool send = true)
        {


            var date = DateTime.Now.ToString();
            if (mf.InvokeRequired)
            {
                mf.BeginInvoke((Action)(() =>
                {
                    //var msg = date + " - " + message;
                    mf.logs.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    var i = mf.logs.Rows.Add(date, message);
                    mf.logs.Rows[i].Cells[1].Style.ForeColor = color;
                    mf.logs.FirstDisplayedCell = mf.logs.Rows[^1].Cells[0]; // mf.logs.Rows.Count - 1
                    //mf.logs.TopIndex = mf.logs.Items.Count - 1;
                }));
            }
            else
            {
                var i = mf.logs.Rows.Add(date, message);
                mf.logs.Rows[i].Cells[1].Style.ForeColor = color;
                mf.logs.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                mf.logs.FirstDisplayedCell = mf.logs.Rows[^1].Cells[0];
                //mf.logs.TopIndex = mf.logs.Items.Count - 1;
            }

            if (save)
            {
                Save(date + " - " + message);

            }
            if (send && Monitor.MonitorServer.isStarted())
            {
                Monitor.MonitorServer.LogSend(message);
            }
            if (mf.ThisTabInfo())
            {
                TaskbarProgress.TaskbarStates state = (TaskbarProgress.TaskbarStates)Enum.Parse(typeof(TaskbarProgress.TaskbarStates), "Normal");
                TaskbarProgress.SetState(mf.Handle, state);
                if (state != TaskbarProgress.TaskbarStates.Indeterminate && state != TaskbarProgress.TaskbarStates.NoProgress)
                    TaskbarProgress.SetValue(mf.Handle, 100, 100);
            }



        }

        private static void Save(string text)
        {
            var date = DateTime.Now.ToString();
            var day = date.Split(" ")[0].Replace("/", "-");
            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }
            var path = @"logs\" + day + ".txt";
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine(text);
            writer.Close();
        }

        public static void UpdateInfo(decimal usdt, decimal withdraw, decimal margin, decimal bnb, decimal spread, decimal upnl, decimal cpnl, decimal bpnl, decimal spnl, decimal close, decimal averaging, decimal stop, string time, decimal done, decimal weights)
        {


            if (mf.InvokeRequired)
            {
                mf.BeginInvoke((Action)(() =>
                {
                    UpdInfoTable(usdt, withdraw, margin, bnb, spread, upnl, cpnl, bpnl, spnl, close, averaging, stop, time, done, weights);
                }));
            }
            else
            {
                UpdInfoTable(usdt, withdraw, margin, bnb, spread, upnl, cpnl, bpnl, spnl, close, averaging, stop, time, done, weights);
            }

        }
        private static void UpdInfoTable(decimal usdt, decimal withdraw, decimal margin, decimal bnb, decimal spread, decimal upnl, decimal cpnl, decimal bpnl, decimal spnl, decimal close, decimal averaging, decimal stop, string time, decimal done, decimal weights)
        {
            mf.info.Items.Clear();
            mf.info.Items.Add(InfoItem.Add("Spread", decimal.Round(spread, 3).ToString(), "Spread"));
            mf.info.Items.Add(InfoItem.Add("Averaging PnL", decimal.Round(averaging, 3).ToString(), "Spread"));
            if (done > 0)
                mf.info.Items.Add(InfoItem.Add("Average done", done.ToString(), "Average"));
            if (time != "")
                mf.info.Items.Add(InfoItem.Add("Entry time", time, "Time"));
            mf.info.Items.Add(InfoItem.Add("Unrealized", decimal.Round(upnl, 6).ToString(), "PNL"));
            mf.info.Items.Add(InfoItem.Add("Long PNL", decimal.Round(bpnl, 6).ToString(), "PNL"));
            mf.info.Items.Add(InfoItem.Add("Short PNL", decimal.Round(spnl, 6).ToString(), "PNL"));
            //mf.info.Items.Add(InfoItem.Add("Short PNL", decimal.Round(spnl, 3).ToString(), "PNL"));
            mf.info.Items.Add(InfoItem.Add("Calculated", decimal.Round(cpnl, 6).ToString(), "PNL"));
            mf.info.Items.Add(InfoItem.Add("Close profit", decimal.Round(close, 6).ToString(), "Profit"));
            if (TraderBot.Instance.Cfg.stopLossState)
                mf.info.Items.Add(InfoItem.Add("Stop-Loss", decimal.Round(stop, 6).ToString(), "Profit"));
            mf.info.Items.Add(InfoItem.Add("Ping", Request.Ping("fapi.binance.com"), "Ping"));
            mf.info.Items.Add(InfoItem.Add("USDT balance", usdt.ToString(), "Balance"));
            mf.info.Items.Add(InfoItem.Add("Available", withdraw.ToString(), "Balance"));
            mf.info.Items.Add(InfoItem.Add("Margin balance", margin.ToString(), "Balance"));
            mf.info.Items.Add(InfoItem.Add("BNB balance", bnb.ToString(), "Balance"));
            mf.info.Items.Add(InfoItem.Add("Alignment ", weights.ToString(), "Alignment"));
        }

        public static void Debug(int i = 0)
        {
#if DEBUG
            Log.Add("Debug: " + i, System.Drawing.Color.Violet);
#endif
        }
        public static void Debug(string i = "")
        {
#if DEBUG
            Log.Add("Debug: " + i, System.Drawing.Color.Violet);
#endif
        }
        public static void Status(string Status, System.Drawing.Color color, bool Temporary = false, int pos = 0)
        {

            if (mf.InvokeRequired)
            {
                mf.BeginInvoke((Action)(() =>
                {

                    if (mf.statusStrip1.Items.Count != 0)
                    {
                        if (Temporary)
                        {
                            if (mf.statusStrip1.Items[pos].Text != null)
                            {
                                Prev = mf.statusStrip1.Items[pos].Text;
                                PrevColor = mf.statusStrip1.Items[pos].ForeColor;
                            }
                            else
                            {
                                Prev = "";
                                PrevColor = System.Drawing.Color.Black;
                            }

                            PrevPos = pos;
                            mf.timerStatus.Enabled = true;
                            mf.timerStatus.Tick += TimerStatus_Tick;
                        }
                        mf.statusStrip1.Items[pos].Text = Status;
                        mf.statusStrip1.Items[pos].ForeColor = color;


                    }
                    else
                    {

                        mf.statusStrip1.Items.Add(Status);
                        mf.statusStrip1.Items[0].ForeColor = color;
                        if (Temporary)
                        {

                            Prev = "";
                            PrevColor = System.Drawing.Color.Black;


                            PrevPos = pos;
                            mf.timerStatus.Enabled = true;
                            mf.timerStatus.Tick += TimerStatus_Tick;
                        }
                    }

                }));
            }
            else
            {

                if (mf.statusStrip1.Items.Count != 0)
                {
                    if (Temporary)
                    {
                        if (mf.statusStrip1.Items[pos].Text != null)
                        {
                            Prev = mf.statusStrip1.Items[pos].Text;
                            PrevColor = mf.statusStrip1.Items[pos].ForeColor;
                        }
                        else
                        {
                            Prev = "";
                            PrevColor = System.Drawing.Color.Black;
                        }

                        PrevPos = pos;
                        mf.timerStatus.Enabled = true;
                        mf.timerStatus.Tick += TimerStatus_Tick;
                    }
                    mf.statusStrip1.Items[pos].Text = Status;
                    mf.statusStrip1.Items[pos].ForeColor = color;


                }
                else
                {

                    mf.statusStrip1.Items.Add(Status);
                    mf.statusStrip1.Items[0].ForeColor = color;
                    if (Temporary)
                    {

                        Prev = "";
                        PrevColor = System.Drawing.Color.Black;


                        PrevPos = pos;
                        mf.timerStatus.Enabled = true;
                        mf.timerStatus.Tick += TimerStatus_Tick;
                    }
                }

            }


        }

        private static void TimerStatus_Tick(object sender, EventArgs e)
        {
            Status(Prev, PrevColor, pos: PrevPos);
            mf.timerStatus.Tick -= TimerStatus_Tick;
        }
    }
}
