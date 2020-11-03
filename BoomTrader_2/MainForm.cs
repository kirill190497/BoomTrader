using BoomTrader_2.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;



namespace BoomTrader_2
{
    public partial class MainForm : Form
    {
        public TraderBot bot;
        private delegate void AddQuotesItem(string strArg);
        public decimal Multiplier = 0M;
        public TabPage Protab = new TabPage("Pro settings");
        public static MainForm Instance { get; private set; }

        private string CryptPass = "BooM2020Trader"; //  
        public MainForm()
        {
            InitializeComponent();
            Instance = this;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            //quotes.Columns.AddRange(new ColumnHeader[] { new ColumnHeader("Symbol"), new ColumnHeader("Price"), new ColumnHeader("Mark"), new ColumnHeader("Percent") });
            foreach (var it in new Tickers())
            {
                symbols.Items.Add(it);
            }
            Text = "Market-neutral basket trading bot on Binance Futures " + Resources.Version;
            Icon = Resources.app;

            clearSelected.Image = Resources.arrow_up;
            openFolder.Image = Resources.folder_open;

            selectAll.Image = Resources.arrow_down;
            refresh.Image = Resources.refresh;

            startDouble.Image = Resources.play; startBtn.Image = Resources.play;
            stopDouble.Image = Resources.stop; stopBtn.Image = Resources.stop;
            resetDouble.Image = Resources.repeat; reset.Image = Resources.repeat;
            closeDouble.Image = Resources.times; closePositions.Image = Resources.times;

            stopBtn.Enabled = false;
            stopDouble.Enabled = false;
            //closeDouble.Enabled = false;
            //closePositions.Enabled = false;
            tabs.SelectedTab = loginPage;



            ReadSettings();
            //logs.DrawMode = DrawMode.OwnerDrawVariable;
            //logs += lst_MeasureItem;
            //logs.DrawItem += logs_DrawItem;

            //((Control)telegramPage).Enabled = false;

            this.toolTip.SetToolTip(this.startDouble, "Start bot");
            this.toolTip.SetToolTip(this.stopDouble, "Stop bot");
            this.toolTip.SetToolTip(this.resetDouble, "Reset coefficients");
            this.toolTip.SetToolTip(this.closeDouble, "Close open positions");


        }



        public void ReadSettings()
        {
            var ini = new IniFile("settings.ini");

            if (ini.KeyExists("entry", "TRADE"))
            {

                try
                {

                    apiKey.Text = Security.Decode(ini.Read("APIkey", "SECURITY"), CryptPass);
                    secretKey.Text = Security.Decode(ini.Read("Secret", "SECURITY"), CryptPass);

                }
                catch
                {

                    apiKey.Text = "";
                    secretKey.Text = "";

                }


                try
                {
                    risks.Checked = Convert.ToBoolean(ini.Read("risks", "SECURITY"));
                }
                catch
                {
                    risks.Checked = false;
                }

                try
                {
                    stopLossState.Checked = Convert.ToBoolean(ini.Read("isstoploss", "TRADE"));
                }
                catch
                {
                    stopLossState.Checked = false;
                }

                //changepairscount
                try
                {
                    if (AllowPosVolume.Checked)
                        posVolume.Value = Convert.ToDecimal(ini.Read("volume", "TRADE"));

                    entrySpread.Value = Convert.ToDecimal(ini.Read("entry", "TRADE"));
                    closeProfit.Value = Convert.ToDecimal(ini.Read("profit", "TRADE"));
                    buyCount.Value = Convert.ToDecimal(ini.Read("long_count", "TRADE"));
                    sellCount.Value = Convert.ToDecimal(ini.Read("short_count", "TRADE"));
                    trailing.Checked = Convert.ToBoolean(ini.Read("istrailing", "TRADE"));
                    noOpen.Checked = Convert.ToBoolean(ini.Read("isnoopen", "TRADE"));
                    leverage.Value = Convert.ToDecimal(ini.Read("leverage", "TRADE"));
                    average.Value = Convert.ToDecimal(ini.Read("average", "TRADE"));
                    averageCount.Value = Convert.ToDecimal(ini.Read("averagecount", "TRADE"));

                    stopLoss.Value = Convert.ToDecimal(ini.Read("stoploss", "TRADE"));
                }
                catch
                {
                    posVolume.Value = 100;
                    entrySpread.Value = 0.5M;
                    closeProfit.Value = 0.5M;
                    buyCount.Value = 4;
                    sellCount.Value = 4;
                    leverage.Value = 75;
                    average.Value = 3;
                    averageCount.Value = 4;
                    stopLoss.Value = 50;
                }




                try
                {
                    trailingsValue.Value = Convert.ToDecimal(ini.Read("trailing", "SECRET"));
                }
                catch
                {
                    trailingsValue.Value = 10;
                }




                try
                {
                    if (ini.Read("selected", "TRADE") != "ALL" && !ini.Read("selected", "TRADE").StartsWith("ALL-"))
                    {
                        foreach (var it in ini.Read("selected", "TRADE").Split("#"))
                        {
                            if (it != "")
                            {
                                selected.Items.Add(it);
                                symbols.Items.Remove(it);
                            }
                        }
                    }
                    else if (ini.Read("selected", "TRADE").Contains("ALL-"))
                    {

                        var delete = ini.Read("selected", "TRADE").Replace("ALL-", "");


                        foreach (var it in new Tickers())
                        {
                            if (!delete.Contains(it))
                            {
                                selected.Items.Add(it);
                                symbols.Items.Remove(it);
                            }

                        }
                    }
                    else
                    {
                        symbols.Items.Clear();
                        foreach (var it in new Tickers())
                        {
                            selected.Items.Add(it);
                        }
                    }

                }
                catch
                {

                }
                // телега

                bool closeState, openState, addsState, pnlState;

                try
                {
                    closeState = Convert.ToBoolean(ini.Read("close", "TELEGRAM"));

                }
                catch (FormatException)
                {
                    closeState = false;
                }
                try
                {
                    openState = Convert.ToBoolean(ini.Read("open", "TELEGRAM"));

                }
                catch (FormatException)
                {
                    openState = false;
                }
                try
                {
                    addsState = Convert.ToBoolean(ini.Read("adds", "TELEGRAM"));

                }
                catch (FormatException)
                {
                    addsState = false;
                }
                try
                {
                    pnlState = Convert.ToBoolean(ini.Read("sendpnl", "TELEGRAM"));

                }
                catch (FormatException)
                {
                    pnlState = false;
                }

                var PeriodVal = "120";
                if (!string.IsNullOrEmpty(ini.Read("pnlperiod", "TELEGRAM")))
                    PeriodVal = ini.Read("pnlperiod", "TELEGRAM");
                tgClose.Checked = closeState;
                tgOpen.Checked = openState;
                tgAdds.Checked = addsState;
                tgPeriod.Checked = pnlState;
                tgPeriodValue.Text = PeriodVal;
                tgApi.Text = ini.Read("api", "TELEGRAM");
                tgName.Text = ini.Read("name", "TELEGRAM");



            }
            else
            {
                var settings = Request.GetJSON("https://license.boomtrader.info/settings/default", "");
                try
                {

                    apiKey.Text = Security.Decode(ini.Read("APIkey", "SECURITY"), CryptPass);
                    secretKey.Text = Security.Decode(ini.Read("Secret", "SECURITY"), CryptPass);

                }
                catch
                {

                    apiKey.Text = "";
                    secretKey.Text = "";

                }

                try
                {
                    entrySpread.Value = Convert.ToDecimal(settings["entry_spread"]);
                }
                catch
                {
                    entrySpread.Value = entrySpread.Maximum;
                }
                Multiplier = Convert.ToDecimal(settings["multiplier"]);
                closeProfit.Value = Convert.ToDecimal(settings["profit"]);
                buyCount.Value = Convert.ToDecimal(settings["pairs_count"]);
                sellCount.Value = Convert.ToDecimal(settings["pairs_count"]);
                leverage.Value = Convert.ToDecimal(settings["leverage"]);
                average.Value = Convert.ToDecimal(settings["average_before"]);
                averageCount.Value = Convert.ToDecimal(settings["stoploss"]);
                stopLoss.Value = Convert.ToDecimal(settings["entry_spread"]);

                stopLossState.Checked = Convert.ToBoolean(settings["stoploss_state"]);
                trailing.Checked = Convert.ToBoolean(settings["trailings_state"]);

            }
        }
        public void WriteSettings()
        {
            var ini = new IniFile("settings.ini");

            ini.Write("volume", posVolume.Value.ToString(), "TRADE");
            ini.Write("entry", entrySpread.Value.ToString(), "TRADE");
            ini.Write("profit", closeProfit.Value.ToString(), "TRADE");
            ini.Write("long_count", buyCount.Value.ToString(), "TRADE");
            ini.Write("short_count", sellCount.Value.ToString(), "TRADE");
            ini.Write("isnoopen", noOpen.Checked.ToString(), "TRADE");
            ini.Write("istrailing", trailing.Checked.ToString(), "TRADE");

            ini.Write("leverage", leverage.Value.ToString(), "TRADE");
            ini.Write("average", average.Value.ToString(), "TRADE");
            ini.Write("averagecount", averageCount.Value.ToString(), "TRADE");
            ini.Write("isstoploss", stopLossState.Checked.ToString(), "TRADE");
            ini.Write("stoploss", stopLoss.Value.ToString(), "TRADE");
            string select = "";

            if (symbols.Items.Count != 0 && selected.Items.Count < 26)
            {
                foreach (var it in selected.Items)
                {
                    select += it.ToString() + "#";
                }
                ini.Write("selected", select, "TRADE");
            }
            else if (symbols.Items.Count == 0)
            {
                ini.Write("selected", "ALL", "TRADE");
            }
            else
            {
                foreach (var it in symbols.Items)
                {
                    select += it.ToString() + "#";
                }
                ini.Write("selected", "ALL-" + select, "TRADE");
            }
            ini.Write("trailing", trailingsValue.Value.ToString(), "SECRET");


        }

        public bool ThisTabInfo()
        {
            bool thistab = false;
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    thistab = tabs.SelectedTab != infoPage;
                }));
            }
            else
                thistab = tabs.SelectedTab != infoPage;

            return thistab;
        }


        public void ClearLog()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    this.ClearLog_Click(clearLog, null);
                }));
            }
            else
            {
                this.ClearLog_Click(clearLog, null);
            }

        }

        private void ProSettings(string mode)
        {

            if (mode == "full-pro")
            {
                // Protab = new TabPage("Pro settings");

                var ini = new IniFile("settings.ini");
                Protab.Controls.Add(trailingsValue);
                Protab.Controls.Add(label8);
                /* Protab.Controls.Add(settingSlot);
                 var setts = Request.GetJSON("https://license.boomtrader.info/settings","");

                 foreach (var it in setts)
                 {
                     settingSlot.Items.Add(new SettingSlotItem { Text = it.Value.ToString(), Value = it.Key});
                 }
                 settingSlot.SelectedIndex = 1;
                */
                Protab.Controls.Add(AllowPosVolume);
                Protab.Controls.Add(AllowChangeBaskets);

                Protab.Location = new Point(4, 24);
                Protab.Name = "proPage";
                Protab.Size = new Size(559, 268);
                Protab.TabIndex = 5;
                Protab.Text = "Pro settings";

                Protab.UseVisualStyleBackColor = true;
                tabs.TabPages.Add(Protab);
                try
                {
                    AllowPosVolume.Checked = Convert.ToBoolean(ini.Read("changeposvol", "SECRET"));
                }
                catch
                {
                    AllowPosVolume.Checked = false;
                    VolumeMultiple.Enabled = true;
                }
                try
                {
                    AllowChangeBaskets.Checked = Convert.ToBoolean(ini.Read("changepairscount", "SECRET"));
                }
                catch
                {
                    AllowChangeBaskets.Checked = false;
                }
                VolumeMultiple.Maximum = 300;
                if (Multiplier != 0M)
                {
                    VolumeMultiple.Value = Multiplier;
                }
                posVolume.Enabled = AllowPosVolume.Checked;
                var work = new IniFile("work.ini");
                if (!AllowPosVolume.Checked)
                {
                    bot.GetBalances();
                    if (decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100)) > 1)

                    {
                        if (work.KeyExists("symbols", "CALC"))
                        {
                            posVolume.Value = Convert.ToDecimal(ini.Read("volume", "TRADE"));
                        }
                        else
                        {
                            decimal val = decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100));
                            if (val > posVolume.Maximum)
                                val = posVolume.Maximum;
                            posVolume.Value = val;
                        }

                    }
                    else
                        posVolume.Value = 1;
                }





                if (!AllowChangeBaskets.Checked)
                {
                    sellCount.Enabled = false;
                    buyCount.Enabled = false;
                    buyCount.Value = 4;
                    sellCount.Value = 4;
                }

            }
            else
            {
                var ini = new IniFile("settings.ini");
                var work = new IniFile("work.ini");
                ini.DeleteSection("SECRET");
                posVolume.Enabled = false;
                bot.GetBalances();
                if (Multiplier != 0M)
                {
                    VolumeMultiple.Value = Multiplier;
                }
                if (work.KeyExists("symbols", "CALC"))
                {
                    posVolume.Value = Convert.ToDecimal(ini.Read("volume", "TRADE"));
                }
                else if (decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100)) > 1)
                {
                    posVolume.Value = decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100));
                }
                else
                {
                    posVolume.Value = 1;

                }
                VolumeMultiple.Maximum = 50;
                sellCount.Enabled = false;
                buyCount.Enabled = false;
                buyCount.Value = 4;
                sellCount.Value = 4;
            }
        }

        public string GetVersion()
        {
            return Resources.Version;
        }
        public void SetVolumeHalfDepo()
        {
            if (!AllowPosVolume.Checked)
            {
                bot.GetBalances();
                decimal half = decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100));
                if (half > 1)
                {
                    posVolume.Value = half;
                }
                else
                    posVolume.Value = 1;
            }
        }
        public void MFInvoke()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {

                }));
            }
            else
            {

            }
        }

        public void LoginTab()
        {

            ((Control)this.loginPage).Enabled = true;
            tabs.TabPages.Remove(Protab);


            tabs.SelectedTab = loginPage;
        }
        private void StartBtn_Click(object sender, EventArgs e)
        {
            if (bot != null)
            {


                if (risks.Checked)
                {
                    if (selected.Items.Count != 0)
                    {
                        if (selected.Items.Count >= (buyCount.Value + sellCount.Value))
                        {
                            if ((buyCount.Value != 0 && sellCount.Value != 0))
                            {
                                bot.SelectedSymbols = new List<string>();
                                foreach (var it in selected.Items)
                                {
                                    bot.SelectedSymbols.Add(it.ToString());
                                }



                                WriteSettings();



                                if (bot.settings.KeyExists("api", "TELEGRAM") && bot.settings.KeyExists("name", "TELEGRAM"))
                                {
                                    var api = bot.settings.Read("api", "TELEGRAM");
                                    var name = bot.settings.Read("name", "TELEGRAM");

                                    bot.Telegram = new Telegram(api, name);
                                }

                                bot.Cfg = new Config
                                {
                                    buyCount = Convert.ToInt32(buyCount.Value),
                                    sellCount = Convert.ToInt32(sellCount.Value),
                                    spreadEntry = entrySpread.Value,
                                    volume = posVolume.Value,
                                    closeProfit = closeProfit.Value,
                                    trailing = trailing.Checked,
                                    trailingValue = trailingsValue.Value,
                                    noEnter = noOpen.Checked,
                                    leverage = Convert.ToInt32(leverage.Value),
                                    averageBefore = average.Value,
                                    averageCount = averageCount.Value,
                                    stopLoss = stopLoss.Value,
                                    stopLossState = stopLossState.Checked,

                                };


                                bot.StartAsync();
                                SwitchInterfase();
                                tabs.SelectedTab = infoPage;
                            }
                            else
                            {
                                MessageBox.Show("Sell and Buy values can't be zero");
                            }

                        }
                        else
                        {
                            MessageBox.Show("Sell and Buy counts exceeds Selected pairs");
                        }

                    }
                    else
                    {
                        MessageBox.Show("Selected empty");
                    }

                }
                else
                {
                    MessageBox.Show("Please read help");
                    tabs.SelectedTab = helpPage;
                }



            }

            else
            {
                MessageBox.Show("Check api keys");
                tabs.SelectedTab = loginPage;
                ((Control)this.loginPage).Enabled = true;
            }
        }




        private void Symbols_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (symbols.SelectedItem != null)
            {
                selected.Items.Add(symbols.SelectedItem);
                symbols.Items.Remove(symbols.SelectedItem);
            }




        }

        private void SwitchUI()
        {
            selected.Enabled = !bot.Status;
            symbols.Enabled = !bot.Status;
            clearSelected.Enabled = !bot.Status;
            startBtn.Enabled = !bot.Status; startDouble.Enabled = !bot.Status;
            if (AllowChangeBaskets.Checked)
            {
                buyCount.Enabled = !bot.Status;
                sellCount.Enabled = !bot.Status;
            }

            VolumeMultiple.Enabled = !bot.Status;
            refresh.Enabled = !bot.Status;
            trailing.Enabled = !bot.Status;
            leverage.Enabled = !bot.Status;
            average.Enabled = !bot.Status;
            stopLoss.Enabled = !bot.Status;
            stopLossState.Enabled = !bot.Status;
            //reset.Enabled = bot.status;
            entrySpread.Enabled = !bot.Status;
            if (AllowPosVolume.Checked)
                posVolume.Enabled = !bot.Status;
            closeProfit.Enabled = !bot.Status;
            selectAll.Enabled = !bot.Status;

            stopBtn.Enabled = bot.Status; stopDouble.Enabled = bot.Status;
            //closePositions.Enabled = bot.status; closeDouble.Enabled = bot.status;
            //pro

            foreach (Control c in Protab.Controls)
                c.Enabled = !bot.Status;
            statusStrip1.Items.Clear();
            var state = "";
            var color = Color.Empty;
            if (bot.Status)
            {
                color = Color.Green;
                state = "Started";
            }
            else
            {
                state = "Stoped";
                color = Color.Red;
            }
            Log.Status(state, color);
        }

        private void SwitchInterfase()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    SwitchUI();
                }));
            }
            else
            {
                SwitchUI();
            }
        }



        private void Selected_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selected.SelectedItem != null)
            {
                symbols.Items.Add(selected.SelectedItem);
                selected.Items.Remove(selected.SelectedItem);
            }
        }
        public void StopBot()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    StopBtn_Click(stopBtn, null);
                }));
            }
            else
            {
                StopBtn_Click(stopBtn, null);
            }
        }

        public void StartBot()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    StartBtn_Click(startBtn, null);
                }));
            }
            else
            {
                StartBtn_Click(startBtn, null);
            }
        }


        private void StopBtn_Click(object sender, EventArgs e)
        {
            bot.Stop();
            SwitchInterfase();
            //bot.SelectedSymbols.Clear();
            bot.Percents.Clear();
            quotes.Items.Clear();


        }
        public void UpdateVolume()
        {
            bot.Cfg.volume = posVolume.Value;
        }
        private void CheckLogin_Click(object sender, EventArgs e)
        {

            var ini = new IniFile("settings.ini");

            bot = new TraderBot(apiKey.Text, secretKey.Text);


            var wallet = bot.GetWallet();
            if (wallet != "error")
            {
                Log.Add("Wallet: " + wallet, Color.Blue, send: false);



                ini.Write("APIkey", Security.Encode(apiKey.Text, CryptPass), "SECURITY");
                ini.Write("Secret", Security.Encode(secretKey.Text, CryptPass), "SECURITY");



                ((Control)this.loginPage).Enabled = false;
                tabs.SelectedTab = settingsPage;


                Monitor.MonitorServer.Start();
                ProSettings("full-pro");

                try
                {
                    if (Multiplier != 0M)
                    {
                        if (Multiplier > VolumeMultiple.Maximum)
                            Multiplier = VolumeMultiple.Maximum;
                        VolumeMultiple.Value = Multiplier;
                    }
                    else
                        VolumeMultiple.Value = Convert.ToDecimal(ini.Read("volumepercent", "TRADE"));
                }
                catch
                {
                    VolumeMultiple.Value = 50;
                }




            }

        }



        private void ClearLog_Click(object sender, EventArgs e)
        {
            logs.Rows.Clear();
            Log.Status("Log cleared", Color.Blue, true);

        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void ClosePositions_Click(object sender, EventArgs e)
        {
            var msg = MessageBox.Show("Are you sure want to close open positions for the selected pairs?", "Close positions?", MessageBoxButtons.YesNo);
            if (msg == DialogResult.Yes)
            {
                if (bot != null)
                    bot.CloseAllPositions(false);
                //StopBot();
                SwitchInterfase();
                tabs.SelectedTab = infoPage;
            }

        }

        private void Risks_CheckedChanged(object sender, EventArgs e)
        {
            var file = new IniFile("settings.ini");
            file.Write("risks", risks.Checked.ToString(), "SECURITY");

        }



        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ThisTabInfo())
            {
                TaskbarProgress.TaskbarStates state = (TaskbarProgress.TaskbarStates)Enum.Parse(typeof(TaskbarProgress.TaskbarStates), "NoProgress");
                TaskbarProgress.SetState(Handle, state);
                if (state != TaskbarProgress.TaskbarStates.Indeterminate && state != TaskbarProgress.TaskbarStates.NoProgress)
                    TaskbarProgress.SetValue(Handle, 100, 100);
            }
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {

            selected.Items.Clear();
            symbols.Items.Clear();
            foreach (var it in new Tickers())
            {
                selected.Items.Add(it);
            }

        }
        private void ClearSelected_Click(object sender, EventArgs e)
        {
            selected.Items.Clear();
            symbols.Items.Clear();
            foreach (var it in new Tickers())
            {
                symbols.Items.Add(it);
            }
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            var state = false;
            if (bot != null)
            {

                ClosePositions_Click(stopBtn, null);
                if (bot.Status)
                    state = true;

            }
            bot.Reset(state);
            SwitchInterfase();

        }

        private void Logs_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void NoOpen_CheckedChanged(object sender, EventArgs e)
        {
            if (bot != null)
            {
                if (bot.Cfg != null)
                    bot.Cfg.noEnter = noOpen.Checked;
                bot.settings.Write("isnoopen", noOpen.Checked.ToString(), "TRADE");
            }
            else
            {
                var ini = new IniFile("settings.ini");
                ini.Write("isnoopen", noOpen.Checked.ToString(), "TRADE");
            }

        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            symbols.Items.Clear();

            foreach (var it in new Tickers())
            {
                if (!selected.Items.Contains(it))
                    symbols.Items.Add(it);
            }
        }

        private void TgTest_Click(object sender, EventArgs e)
        {

            var telegram = new Telegram(tgApi.Text, tgName.Text);
            telegram.SendMsg("Test message");
        }

        private void SaveTelegram_Click(object sender, EventArgs e)
        {
            if (bot != null)
            {
                var ini = new IniFile("settings.ini");
                ini.Write("api", tgApi.Text, "TELEGRAM");
                ini.Write("name", tgName.Text, "TELEGRAM");
                ini.Write("open", tgOpen.Checked.ToString(), "TELEGRAM");
                ini.Write("close", tgClose.Checked.ToString(), "TELEGRAM");
                ini.Write("adds", tgAdds.Checked.ToString(), "TELEGRAM");
                ini.Write("sendpnl", tgPeriod.Checked.ToString(), "TELEGRAM");
                ini.Write("pnlperiod", tgPeriodValue.Text, "TELEGRAM");

                if (!string.IsNullOrEmpty(tgPeriodValue.Text))
                {
                    bot.Telegram = new Telegram(tgApi.Text, tgName.Text);
                    tabs.SelectedTab = settingsPage;
                    Log.Status("Telegram config saved", Color.Blue, true);
                }
                else
                {
                    MessageBox.Show("Period send no set");
                }
            }
            else
            {
                MessageBox.Show("Please check licence");
                tabs.SelectedTab = loginPage;
            }

        }

        private void RichTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {

            try
            {
                Process.Start("explorer.exe", e.LinkText);
            }
            catch (Exception)
            {

            }
        }
        private void AllowPosVolume_CheckedChanged(object sender, EventArgs e)
        {
            posVolume.Enabled = AllowPosVolume.Checked;
            VolumeMultiple.Enabled = !AllowPosVolume.Checked;
            if (AllowPosVolume.Checked == true)
            {
                VolumeMultiple.Value = 50;
            }
            bot.GetBalances();
            var volume = decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100));

            if (volume >= posVolume.Minimum && volume <= posVolume.Maximum)
                posVolume.Value = volume;
            else
                posVolume.Value = volume > posVolume.Maximum ? posVolume.Maximum : posVolume.Minimum;
            var ini = new IniFile("settings.ini");
            ini.Write("changeposvol", AllowPosVolume.Checked.ToString(), "SECRET");
        }

        private void AllowChangeBaskets_CheckedChanged(object sender, System.EventArgs e)
        {

            sellCount.Enabled = AllowChangeBaskets.Checked;
            buyCount.Enabled = AllowChangeBaskets.Checked;

            if (!AllowChangeBaskets.Checked)
            {

                buyCount.Value = 4;
                sellCount.Value = 4;
            }

            var ini = new IniFile("settings.ini");
            ini.Write("changepairscount", AllowChangeBaskets.Checked.ToString(), "SECRET");
        }
        private void OpenFolder_Click(object sender, EventArgs e)
        {
            try
            {

                Process.Start("explorer.exe", Directory.GetCurrentDirectory() + "\\logs");
            }
            catch (Exception)
            {

            }
        }

        private void VolumeMultiple_ValueChanged(object sender, EventArgs e)
        {

            var vol = decimal.Round(bot.UsdtBalance * (VolumeMultiple.Value / 100), MidpointRounding.AwayFromZero);
            if (vol < 1)
            {
                vol = 1;
            }
            var ini = new IniFile("settings.ini");
            ini.Write("volumepercent", VolumeMultiple.Value.ToString(), "TRADE");
            posVolume.Value = vol;

        }

        private void AverageCount_ValueChanged(object sender, EventArgs e)
        {
            if (bot != null)
            {
                if (bot.Status)
                {
                    var ini = new IniFile("work.ini");
                    ini.Write("AveragingLeft", averageCount.Value.ToString(), "ORDERS");
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Monitor.MonitorServer.Stop();
            Application.Exit();
        }
    }
}
