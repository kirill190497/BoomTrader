using BoomTrader_2.Settings;
using System;

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
        public Telegram(string crierApi = "", string name = "")
        {
            //Insctance = this;
            this._api = crierApi;
            this._name = name;

            this._uri = "http://crierbot.appspot.com/" + this._api + "/send";


            ReadCfg();
        }



        public void SendMsg(string message)
        {

            var msg = "BoomTrader (" + this._name + "): " + message;
            Request.GET(this._uri, "message=" + msg);

        }

    }
}
