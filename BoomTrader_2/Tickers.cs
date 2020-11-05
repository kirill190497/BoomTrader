
//using System.Windows.Forms;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace BoomTrader_2
{
    public class Tickers : ObservableCollection<string>
    {

        public Tickers()
        {

            try
            {
                JObject exinfo = JObject.Parse(Request.GET("https://fapi.binance.com/fapi/v1/exchangeInfo", ""));
                foreach (var symbol in exinfo["symbols"])
                {
                    Add(symbol["symbol"].ToString());

                }
                
            }
            catch (Exception)
            {
                MessageBox.Show("Failed internet connection");
            }


        }
    }
}
