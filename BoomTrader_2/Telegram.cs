using BoomTrader_2.Settings;
using System;
using static System.Net.WebRequestMethods;

namespace BoomTrader_2
{
    public class Telegram
    {
        public bool close;
        public bool open;
        public bool adds;
        public bool sendpnl;
        public int period;
        private string _api;
        private string _name;
        private string _uri;
        private string _chat;
        //public static TelegramNotify Insctance;

        private IniFile settings = new IniFile("settings.ini");
        private void ReadCfg()
        {
            bool closeState, openState, addsState, pnlState;
            int PeriodVal;
            try
            {
                closeState = Convert.ToBoolean(settings.Read("close", "TELEGRAM"));

            }
            catch (FormatException)
            {
                closeState = false;
            }
            try
            {
                openState = Convert.ToBoolean(settings.Read("open", "TELEGRAM"));

            }
            catch (FormatException)
            {
                openState = false;
            }
            try
            {
                addsState = Convert.ToBoolean(settings.Read("adds", "TELEGRAM"));

            }
            catch (FormatException)
            {
                addsState = false;
            }
            try
            {
                pnlState = Convert.ToBoolean(settings.Read("sendpnl", "TELEGRAM"));

            }
            catch (FormatException)
            {
                pnlState = false;
            }
            try
            {
                PeriodVal = Convert.ToInt32(settings.Read("pnlperiod", "TELEGRAM"));

            }
            catch (FormatException)
            {
                PeriodVal = 1;
            }

            this.open = openState;
            this.close = closeState;
            this.adds = addsState;
            this.sendpnl = pnlState;

            this.period = PeriodVal;


        }
        public Telegram(string botToken = "", string name = "", string chat = "")
        {
            //Insctance = this;
            this._api = botToken;
            this._name = name;
            this._chat = "173983426";//chat;

            this._uri = "https://api.telegram.org/bot" + _api + "/sendMessage";


            ReadCfg();
        }



        public void SendMsg(string message)
        {

            var msg = "BoomTrader (" + this._name + "): " + message;
            Request.POST(this._uri, "chat_id="+_chat+"&text=" + msg);

        }

    }
}
