using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces.SubClients.Futures;
using Binance.Net.Objects.Futures.FuturesData;
using Binance.Net.Objects.Futures.MarketData;
using BoomTrader_2.Settings;
using CryptoExchange.Net.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace BoomTrader_2
{
    public class TraderBot
    {
        public List<string> SelectedSymbols { get; set; }
        MainForm mf = MainForm.Instance;
        public static TraderBot Instance { get; set; }
        public Config Cfg { get; set; }
        public bool Status { get; private set; }
        public string ApiKey { get; private set; }
        public decimal Spread = 0;
        public decimal AverageDone = 0;
        public decimal AveragingSpread = 0;
        public decimal UnrealizedPnL = 0;
        public decimal CalculatedPnL = 0;
        public decimal BuyPNL = 0;
        public decimal SellPNL = 0;

        public Telegram Telegram { get; set; }

        public decimal CloseProfit { get; private set; } //StopLoss
        public decimal StopLoss { get; private set; }
        public string SecretKey { get; private set; }
        public bool Scalper = false;
        public bool TrailingEnabled { get; private set; }

        public decimal Weights { get; private set; }
        public Tuple<bool, string, string> License { get; set; }
        public BinanceClient client = new BinanceClient();
        private int baskets = 2;

        public IniFile settings = new IniFile("settings.ini");
        public IniFile work = new IniFile("work.ini");

        public bool Entered = false;
        public List<PercentItem> Percents = new List<PercentItem>();
        public string EntryTime { get; set; }
        private System.Timers.Timer SortTimer { get; set; }
        public BinanceFuturesExchangeInfo Exinfo { get; private set; }
        private bool Busy = false;
        public decimal UsdtBalance, BnbBalance, AvailableBalance, marginBalance;
        private Task WorkThread { get; set; }
        private BinanceSocketClient socketClient { get; set; }

        private bool NeedToClose = false;
        private IBinanceClientFuturesUsdt futures { get; set; }
        public TraderBot(string api, string secret)
        {
            try
            {
                //Instance = this;
                this.Status = false;

                this.ApiKey = api;
                this.SecretKey = secret;
                Instance = this;
                this.Telegram = new Telegram();
                this.TrailingEnabled = false;
                this.client.SetApiCredentials(this.ApiKey, this.SecretKey);
                //this.socketClient = new BinanceSocketClient();
                //this.socketClient.SetApiCredentials(this.ApiKey, this.SecretKey);
                this.futures = this.client.FuturesUsdt;
                Exinfo = futures.System.GetExchangeInfo().Data;

                

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);

            }
        }

        public string GetWallet()
        {
            try
            {
                return futures.Account.GetBalance().Data.ToList()[0].AccountAlias;
            }
            catch (Exception)
            {
                return "error";
            }

        }


        public void GetBalances()
        {




            try
            {
                var balances = futures.Account.GetBalance().Data.ToList();
                UsdtBalance = balances[0].Balance;
                BnbBalance = balances[1].Balance;
                AvailableBalance = balances[0].MaxWithdrawAvailable;
                marginBalance = balances[0].Balance + UnrealizedPnL;

            }
            catch
            {
                //Log.Add("Error update balance", Color.Orange);
            }

        }

        public void Work()
        {



            while (Status)
            {

              
                if (!Status) break;


                ObservableCollection<ListViewItem> quotes = new ObservableCollection<ListViewItem>();
                decimal weights = 0;
                int cnt = 0;
                foreach (var it in SelectedSymbols)
                {
                    decimal price = 0;
                    decimal entry = 0;

                    List<BinanceFuturesPosition> positions = new List<BinanceFuturesPosition>();
                    try
                    {
                        var temp = futures.Market.GetPrice(it);
                        if (temp.Success)
                            price = temp.Data.Price;

                        var postemp = futures.GetPositionInformation();
                        if (postemp.Success)
                            positions = postemp.Data.ToList();


                        foreach (var pos in positions.ToList())
                        {
                            if (pos.Symbol == it && pos.Quantity != 0)
                            {
                                entry = pos.EntryPrice;
                                if (pos.EntryPrice * pos.Quantity != 0)
                                {
                                    weights += pos.EntryPrice * pos.Quantity;
                                    cnt++;
                                }
                            }

                        }
                    }
                    catch (NullReferenceException)
                    {
                        Log.Add("Error get symbol prices", Color.Red, send: false);
                    }

                    var list = new List<string>();

                    decimal percent = 0;
                    foreach (var start in Percents.ToList())
                    {
                        list.Add(start.Symbol);
                        if (start.Symbol == it)
                        {

                            if (price != 0)
                            {
                                if (entry != 0)
                                    start.Start = entry;
                                if (start.Start != 0)
                                    percent = 100 / (start.Start / price) - 100;
                                else
                                    percent = 0;
                                start.Percent = percent;
                                start.Entry = entry;
                                start.Price = price;
                            }
                            else
                            {
                                Log.Add("Error get symbol prices", Color.Red, send: false);

                            }

                            if (Entered && start.Entry == 0)
                                Percents.Remove(start);

                        }

                    }

                    if (entry > 0 && !list.Contains(it))
                    {
                        Percents.Add(new PercentItem { Symbol = it, Entry = entry, Price = price, Percent = percent, Start = entry });
                    }




                    

                }
                if (Validation(cnt))
                    Weights = weights;
                else
                    Weights = -0;
                if (Entered)
                {
                    //Log.Add(weights.ToString(), Color.Green);
                    decimal price = 0;
                    foreach (var it in Percents.ToList())
                    {
                        var temp = futures.Market.GetPrice(it.Symbol);
                        if (temp.Success)
                        {
                            price = temp.Data.Price;
                        }


                        if (price != 0)
                        {
                            var percent = 100 / (it.Start / price) - 100;
                            it.Percent = percent;
                            //it.Entry = entry;
                            it.Price = price;
                        }
                        else
                        {
                            Log.Add("Error get symbol prices", Color.Red, send: false);

                        }


                    }
                }

                if (mf.InvokeRequired)
                {
                    mf.BeginInvoke((Action)(() =>
                    {
                        mf.quotes.Items.Clear();
                    }));
                }
                else
                {
                    mf.quotes.Items.Clear();
                }


                Percents.Sort(new PercentCompare());
                foreach (var it in Percents.ToList())
                {
                    mf.BeginInvoke((Action)(() =>
                    {
                        Color color = Color.Black;
                        if (it.Long)
                            color = Color.DarkGreen;
                        if (it.Short)
                            color = Color.Red;
                        mf.quotes.Items.Add(QuotesItem.Add(it.Symbol, it.Percent, it.Entry, it.Price, color));
                    }));
                }




                decimal s1 = 0;
                decimal s2 = 0;
                //int success = 0;
                List<PercentItem> ts = new List<PercentItem>();



                if (Entered)
                {
                    foreach (var it in Percents)
                    {
                        if (it.Short)
                        {
                            s2 += it.Percent;
                        }
                        else if (it.Long)
                        {
                            s1 += it.Percent;
                        }
                    }
                }


                if (mf.InvokeRequired)
                {
                    mf.BeginInvoke((Action)(() =>
                    {
                        if (Telegram.sendpnl)
                        {
                            mf.tgtimer.Enabled = true;
                        }
                        else
                        {
                            mf.tgtimer.Enabled = false;
                        }
                    }));
                }
                else
                {
                    if (Telegram.sendpnl)
                    {
                        mf.tgtimer.Enabled = true;
                    }
                    else
                    {
                        mf.tgtimer.Enabled = false;
                    }
                }



                int sc;
                int bc;
                try
                {
                    bc = Cfg.buyCount;

                }
                catch (Exception)
                {
                    bc = Percents.Count / 2;
                }
                try
                {
                    sc = Cfg.sellCount;

                }
                catch (Exception)
                {
                    sc = Percents.Count / 2;
                }
                decimal sbuy;
                decimal ssell;

                try
                {
                    sbuy = s1 / bc;

                }
                catch (Exception)
                {

                    sbuy = 0;

                }
                try
                {
                    ssell = s2 / sc;
                }
                catch (Exception)
                {
                    ssell = 0;
                }

                if (bc == 0 || sc == 0)
                    baskets = 1;
                if (Entered)
                    Spread = (sbuy - ssell) / baskets;
                decimal volume;
                try
                {
                    volume = Convert.ToDecimal(work.Read("Volume", "ORDERS"));
                }
                catch
                {
                    volume = Cfg.volume;
                }
                if (!TrailingEnabled)
                    CloseProfit = volume / 100 * Cfg.closeProfit;




                if (Entered)
                {

                    if (mf.averageCount.Value > 0)
                    {
                        if (CalculatedPnL <= AveragingSpread)
                        {

                            BasketAveraging(false);
                        }
                    }

                }


                if (Cfg.noEnter)
                {

                    Log.Status("New positions will not be opened", Color.Blue);
                }
                else
                {
                    if (Status)
                        Log.Status("Started", Color.Green);
                    else
                        Log.Status("Stoped", Color.Red);
                }

                if (Entered && !Busy)
                {
                    if (Math.Abs(Weights) > 0.3M && CalculatedPnL < 0)
                    {
                        //Log.Add(Weights.ToString(), Color.Black);
                        NeedToClose = BasketAlignment();


                    }
                }

                TimerSort_Tick();

                Thread.Sleep(10000);
            }


            if (mf.InvokeRequired)
            {
                mf.BeginInvoke((Action)(() =>
                {
                    mf.quotes.Items.Clear();
                }));
            }
            else
            {
                mf.quotes.Items.Clear();
            }
        }

        private void SetLeverage(string Pair, int Leverage)
        {
            WebCallResult<BinanceFuturesInitialLeverageChangeResult> lever;//= client.ChangeInitialLeverage(Pair, Leverage);
            bool success = false;

            while (!success)
            {
                if (Leverage == 0)
                {
                    Log.Add(string.Format("Failed to set leverage for the {0} pair, excluded from calculations ", Pair), Color.Orange);
                    SelectedSymbols.Remove(Pair);
                    break;
                }
                lever = futures.ChangeInitialLeverage(Pair, Leverage);
                if (lever.Success)
                {

                    Log.Add(string.Format("For {0} set leverage {1}, max: {2} ", lever.Data.Symbol, lever.Data.Leverage, lever.Data.MaxNotionalValue), Color.Black, send: false);
                    success = true;


                }
                Leverage -= 5;

            }
        }

        private bool BasketAlignment()
        {
            List<PercentItem> temp = new List<PercentItem>();
            OrderSide side = OrderSide.Buy;
            PositionSide positionSide = PositionSide.Both;
            string sideName = "";
            decimal OrderVolume = 0;
            if (Weights > 0)
            {
                // перекос в сторону лонга идем в шорт
                foreach (var it in Percents)
                {
                    if (it.Short)
                        temp.Add(it);
                }
                side = OrderSide.Sell;
                sideName = "Short";
                positionSide = PositionSide.Short;
            }
            else if (Weights < 0)
            {
                // перекос в сторону шорта идем в лонг
                foreach (var it in Percents)
                {
                    if (it.Long)
                        temp.Add(it);
                }
                side = OrderSide.Buy;
                sideName = "Long";
                positionSide = PositionSide.Long;
            }
            else if (Weights == 0)
            {
                Log.Add("Alignment is not required", Color.Green);
                return false;
            }
            PercentItem PairAlign = new PercentItem();
            if (side == OrderSide.Sell)
            {
                if (temp.Count != 0)
                    PairAlign = temp[0];
                else
                {
                    Log.Add("Error! there are no positions in " + sideName + ". Closing all positions", Color.OrangeRed);
                    return true;
                }
            }
            else if (side == OrderSide.Buy)
            {
                if (temp.Count != 0)
                    PairAlign = temp[^1];
                else
                {
                    Log.Add("Error! there are no positions in " + sideName + ". Closing all positions", Color.OrangeRed);
                    return true;
                }
            }

            var price = futures.Market.GetPrice(PairAlign.Symbol).Data.Price;
            int precision = 0;
            foreach (var it in Exinfo.Symbols)
            {
                if (it.Name == PairAlign.Symbol)
                    precision = it.QuantityPrecision;

            }
            WebCallResult<BinanceFuturesPlacedOrder> order = null;
            var quantity = decimal.Round(Math.Abs(Weights) / price, precision);

            string message = "";
            bool send = false;
            if (quantity > 0)
            {
                order = futures.Order.PlaceOrder(PairAlign.Symbol, side, OrderType.Market, quantity, positionSide);


                if (order.Success)
                {
                    message = "Basket alignment: " + sideName + " " + order.Data.Symbol + " quantity: " + order.Data.OriginalQuantity;
                    send = true;
                    OrderVolume = order.Data.OriginalQuantity * price;
                    var volume = Convert.ToDecimal(work.Read("Volume", "ORDERS"));
                    work.Write("Volume", (volume + OrderVolume).ToString(), "ORDERS");
                }
                else
                {
                    message = "Basket alignment: I can't align, error:" + order.Error.Message;
                    send = true;
                }

            }
            else
            {
                message = "Basket alignment: I can't find a pair for alignment";
                send = false;
            }



            Log.Add(message, Color.Blue, send: send);
            return false;
        }

        private void BasketAveraging(bool trand)
        {
            Busy = true;
            PercentItem[] tempPercents = new PercentItem[2];
            List<PercentItem> temp = new List<PercentItem>();
            
            bool success = false;

            var msg = "Averaging";


            decimal quantity = 0;


            OrderSide side = OrderSide.Buy;
            PositionSide positionSide = PositionSide.Both;
            var OrderVolume = Cfg.volume / 2 / Cfg.buyCount;


            foreach (var pos in Percents.ToList())
            {
                if (pos.Long)
                    temp.Add(pos);
            }
            try
            {
                tempPercents[0] = temp[^1];
                temp.Clear();
            }
            catch
            {
                Log.Debug("no long");
            }

            foreach (var pos in Percents.ToList())
            {
                if (pos.Short)
                    temp.Add(pos);
            }
            try
            {
                tempPercents[1] = temp[^1];
                temp.Clear();
            }
            catch
            {
                Log.Debug("no short");
            }
            temp.Clear();
            decimal Short = 0;
            decimal Long = 0;
            foreach (var i in futures.GetPositionInformation().Data.ToList())
            {
                foreach (var item in tempPercents)
                {
                    if (i.Symbol == item.Symbol && item != null)
                    {
                        if (i.Quantity > 0)
                        {
                            Long = i.UnrealizedPnL;

                        }

                        else if (i.Quantity < 0)
                        {
                            Short = i.UnrealizedPnL;

                        }

                    }
                }
            }
            PercentItem PairAverage = new PercentItem();
            if (Math.Max(Short, Long) == Long)
            {
                PairAverage = tempPercents[0];
                
                side = OrderSide.Buy;
                positionSide = PositionSide.Long;
            }
            else
            {
                PairAverage = tempPercents[1];
                
                side = OrderSide.Sell;
                positionSide = PositionSide.Short;
            }
            Array.Clear(tempPercents, 0, tempPercents.Length);




            // логика



            var price = futures.Market.GetPrice(PairAverage.Symbol).Data.Price;
            int precision = 0;

            foreach (var it in Exinfo.Symbols)
            {
                if (it.Name == PairAverage.Symbol)
                    precision = it.QuantityPrecision;

            }
            quantity = decimal.Round(OrderVolume / price, precision);
            WebCallResult<BinanceFuturesPlacedOrder> order = null;
            //var quantity = decimal.Round(Math.Abs(Weights) / price, precision);

            string message = "";

            if (quantity > 0)
            {
                order = futures.Order.PlaceOrder(PairAverage.Symbol, side, OrderType.Market, quantity, positionSide);


                if (order.Success)
                {
                    message = msg + ": " + positionSide + " " + order.Data.Symbol + " quantity: " + order.Data.OriginalQuantity;

                    success = true;
                }
                else
                {
                    message = msg + ": Error " + order.Error.Message;

                }

            }
            else
            {
                message = msg + ": failed to perform the averaging";

            }


            if (success)
            {
                var volume = Convert.ToDecimal(work.Read("Volume", "ORDERS"));
                work.Write("EntrySpread", Spread.ToString(), "ORDERS");
                work.Write("AveragingPnL", CalculatedPnL.ToString(), "ORDERS");
                work.Write("Volume", (volume + OrderVolume).ToString(), "ORDERS");
                if (!trand)
                {
                    if (mf.InvokeRequired)
                    {
                        mf.BeginInvoke((Action)(() =>
                        {
                            mf.averageCount.Value -= 1;

                        }));
                    }
                    else
                    {
                        mf.averageCount.Value -= 1;
                    }
                    work.Write("AveragingLeft", mf.averageCount.Value.ToString(), "ORDERS");
                    AverageDone++;
                    work.Write("AveragingDone", AverageDone.ToString(), "ORDERS");
                }
            }
            Log.Add(message, Color.Blue);

            Busy = false;


        }


        public async void StartAsync()
        {
            Status = true;

            Percents.Clear();

            var selected = work.Read("symbols", "CALC");
            try
            {
                var culture = new CultureInfo("en-US");
                EntryTime = TimeAgo.DateTimeExtensions.TimeAgo(DateTime.Parse(work.Read("EntryTime", "ORDERS")), culture);
            }
            catch
            {
                EntryTime = "";
            }
            mf.UpdateVolume();

            if (Telegram.sendpnl)
            {
                mf.tgtimer.Interval = Telegram.period * 60000;
                mf.tgtimer.Enabled = true;
                mf.tgtimer.Tick += Tgtimer_Tick;
            }

            if (selected != "")
            {

                Entered = true;
            }

            try
            {
                if (mf.InvokeRequired)
                {
                    mf.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            mf.averageCount.Value = Convert.ToDecimal(work.Read("AveragingLeft", "ORDERS"));
                        }
                        catch
                        {

                        }

                    }));
                }
                else
                {
                    mf.averageCount.Value = Convert.ToDecimal(work.Read("AveragingLeft", "ORDERS"));
                }

            }
            catch
            {
                if (mf.InvokeRequired)
                {
                    mf.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            mf.averageCount.Value = Convert.ToDecimal(settings.Read("averagecount", "TRADE"));
                        }
                        catch
                        {

                        }

                    }));
                }
                else
                {
                    mf.averageCount.Value = Convert.ToDecimal(settings.Read("averagecount", "TRADE"));
                }

            }

            




            try
            {
                CloseProfit = Convert.ToDecimal(work.Read("Volume", "ORDERS")) / 100 * Cfg.closeProfit;
            }
            catch (Exception)
            {
                CloseProfit = Cfg.volume / 100 * Cfg.closeProfit;
            }
            /*
            try
            {
                AveragingSpread = Convert.ToDecimal(work.Read("EntrySpread", "ORDERS")) - cfg.averageBefore;
            }
            catch (Exception)
            {
                AveragingSpread = cfg.spreadEntry - cfg.averageBefore;
            }
            */



            var hedge = futures.ModifyPositionMode(true);
            if (hedge.Success)
                Log.Add("Position mode: " + hedge.Data.PositionMode, Color.Green);

            if (selected != "")
            {

                foreach (var it in selected.Split("#"))
                {
                    if (it != "")
                    {
                        var symbol = it.Split("%")[0];
                        var start = Convert.ToDecimal(it.Split("%")[1]);

                        var Long = false;
                        var Short = false;

                        foreach (var pos in futures.GetPositionInformation().Data.ToList())
                        {
                            if (pos.Symbol == symbol && pos.Quantity != 0)
                            {
                                
                                    if (pos.PositionSide == PositionSide.Long)
                                        Long = true;
                                    else if (pos.PositionSide == PositionSide.Short)
                                        Short = true;
                                    
                                    start = pos.EntryPrice;
                                
                                
                            }
                        }

                        var data = futures.Market.GetPrice(symbol).Data;
                        var price = data.Price;
                        decimal percent = 0;
                        if (price != 0 && start != 0)
                            percent = 100 / (start / price) - 100;

                        Percents.Add(new PercentItem { Symbol = symbol, Start = start,Percent = percent, Price = price, Long = Long, Short = Short });
                    }

                }
            }
            else
            {
                foreach (var it in SelectedSymbols.ToList())
                {
                    SetLeverage(it, Cfg.leverage);

                }
                foreach (var it in SelectedSymbols.ToList())
                {
                    var price = futures.Market.GetPrice(it).Data.Price;
                    var percent = 100 / (price / price) - 100;

                    Percents.Add(new PercentItem { Symbol = it, Start = price, Percent = percent, Price = price });
                }
            }


            SortTimer = new System.Timers.Timer();
            SortTimer.Elapsed += SortTimer_Elapsed;
            SortTimer.Interval = 1000;
            SortTimer.AutoReset = true;
            SortTimer.Start();


            await (WorkThread = Task.Run(() => Work()));
            Log.Status("Started", Color.Green);
        }

        private void SortTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            GetPositions();
            GetBalances();
            if (NeedToClose)
                CloseAllPositions(true);



            try
            {
                var culture = new CultureInfo("en-US");
                EntryTime = TimeAgo.DateTimeExtensions.TimeAgo(DateTime.Parse(work.Read("EntryTime", "ORDERS")), culture);
            }
            catch
            {
                EntryTime = "";
            }
            try
            {//GetBalance(0), GetBalance(0, true), GetBalance(3), GetBalance(1)

                Log.UpdateInfo(UsdtBalance, AvailableBalance, marginBalance, BnbBalance, Spread, UnrealizedPnL, CalculatedPnL, BuyPNL, SellPNL, CloseProfit, AveragingSpread, StopLoss, EntryTime, AverageDone, Weights);
            }
            catch (Exception)
            {
                Log.Add("update info", Color.Red, send: false);
            }
            decimal volume;
            try
            {
                volume = Convert.ToDecimal(work.Read("Volume", "ORDERS"));
            }
            catch
            {
                volume = Cfg.volume;
            }

            CloseProfit = volume / 100 * Cfg.closeProfit;

            try
            {
                AverageDone = Convert.ToInt32(work.Read("AveragingDone", "ORDERS"));
            }
            catch
            {
                AverageDone = 0;
            }
            //decimal.Round((volume - Cfg.volume) / multivol);

            

            if ((CalculatedPnL >= CloseProfit || TrailingEnabled) && CalculatedPnL > 0 && Validation(Percents.Count()))
            {
                if (Cfg.trailing && TrailingOut(CalculatedPnL) && !Busy)
                {
                    Busy = true;
                    var text = string.Format("Trailing stop: {0}", CalculatedPnL);
                    Log.Add(text, Color.Green);
                    if (Telegram.close)
                        Telegram.SendMsg(text);

                    CloseAllPositions(true);

                }
                else if (!Cfg.trailing && !Busy)
                {
                    Busy = true;
                    var text = "Profit: " + CalculatedPnL;
                    Log.Add(text, Color.Green);

                    if (Telegram.close)
                        Telegram.SendMsg(text);
                    CloseAllPositions(true);
                }

            }
            decimal AveragingPnL;
            try
            {
                AveragingPnL = Convert.ToDecimal(work.Read("AveragingPnL", "ORDERS"));
            }
            catch
            {
                AveragingPnL = 0;
            }

            AveragingSpread = -(volume / 100 * Cfg.averageBefore) + AveragingPnL;
            if (Cfg.stopLossState)
            {
                StopLoss = -(volume / 100 * Cfg.stopLoss);
                if (CalculatedPnL < StopLoss && !Busy && Validation(Percents.Count()))
                {
                    Busy = true;
                    Log.Add("StopLoss: " + CalculatedPnL, Color.Brown);
                    if (Telegram.close)
                        Telegram.SendMsg("StopLoss: " + CalculatedPnL);
                    CloseAllPositions(true);
                }
            }

            if (TrailingEnabled && !Busy && Validation(Percents.Count()))
            {

                var closeProfit = volume / 100 * Cfg.closeProfit;
                if (CalculatedPnL >= closeProfit * 2 && !Busy)
                {
                    // тренд
                    //Busy = true;
                    //BasketAveraging(true);
                }
                if (CloseProfit <= CloseProfit - (CloseProfit * 0.1M) && CalculatedPnL > 0)
                {
                    Busy = true;
                    var text = string.Format("Trailing stop: {0}", CalculatedPnL);
                    Log.Add(text, Color.Green);
                    CloseAllPositions(true);
                    if (Telegram.close)
                        Telegram.SendMsg(text);
                }

            }
            //Log.Add("Debug", Color.Gray);
            //SortTimer.Start();
        }
        private bool Validation(int count)
        {
            if (count == Cfg.buyCount + Cfg.sellCount)
            {
                return true;
            }
            else
                return false;
        }
        private void Tgtimer_Tick(object sender, EventArgs e)
        {
            var msg = HttpUtility.UrlEncode("Calculated PnL: " + CalculatedPnL + "\nTime:" + DateTime.Now);
            Telegram.SendMsg(msg);
        }

        private void TimerSort_Tick()
        {

            if (!Entered)
            {

                decimal s1 = 0;
                decimal s2 = 0;
                int success = 0;
                List<PercentItem> ts = new List<PercentItem>();

                if (((Cfg.buyCount != 0 && Cfg.sellCount != 0) || Scalper) && Percents.Count > 0)
                {
                    for (var i = 0; i < Cfg.buyCount; i++)
                    {

                        s1 += Percents[i].Percent;
                        ts.Add(new PercentItem { Symbol = Percents[i].Symbol, Start = Percents[i].Start, Entry = Percents[i].Entry, Percent = Percents[i].Percent, Price = Percents[i].Price, Long = true });
                    }


                    for (var i = Percents.Count - 1; i > (Percents.Count - 1 - Cfg.sellCount); i--)
                    {

                        s2 += Percents[i].Percent;
                        ts.Add(new PercentItem { Symbol = Percents[i].Symbol, Start = Percents[i].Start, Entry = Percents[i].Entry, Percent = Percents[i].Percent, Price = Percents[i].Price, Short = true });
                    }

                }

                int sc;
                int bc;
                try
                {
                    bc = Cfg.buyCount;

                }
                catch (Exception)
                {
                    bc = Percents.Count / 2;
                }
                try
                {
                    sc = Cfg.sellCount;

                }
                catch (Exception)
                {
                    sc = Percents.Count / 2;
                }
                decimal sbuy;
                decimal ssell;

                try
                {
                    sbuy = s1 / bc;

                }
                catch (Exception)
                {

                    sbuy = 0;

                }
                try
                {
                    ssell = s2 / sc;
                }
                catch (Exception)
                {
                    ssell = 0;
                }

                if (bc == 0 || sc == 0)
                    baskets = 1;

                Spread = (sbuy - ssell) / baskets;


                if (Math.Abs(Spread) >= Cfg.spreadEntry && !Entered && !Cfg.noEnter)
                {
                    //Log.Add("Buy: " + buy); Log.Add("Sell :" + sell);
                    //mf.timerSort.Tick -= TimerSort_Tick;
                    //mf.timerSort.Enabled = false;
                    decimal quantity = 0;
                    List<string> list = new List<string>();
                    foreach (var it in ts.ToList())
                    {
                        if (!list.Contains(it.Symbol))
                        {



                            OrderSide side;
                            PositionSide positionSide;
                            string type;
                            int count;
                            if (it.Long)
                            {
                                side = OrderSide.Buy;
                                positionSide = PositionSide.Long;
                                type = "Long";
                                count = Cfg.buyCount;
                            }
                            else
                            {
                                side = OrderSide.Sell;
                                type = "Short";
                                positionSide = PositionSide.Short;
                                count = Cfg.sellCount;
                            }

                            var price = futures.Market.GetPrice(it.Symbol);
                            foreach (var iter in Exinfo.Symbols)
                            {
                                if (iter.Name == it.Symbol)
                                    quantity = decimal.Round(Cfg.volume / baskets / price.Data.Price / count, iter.QuantityPrecision);
                            }

                            var culture = new CultureInfo("en-US");
                            EntryTime = TimeAgo.DateTimeExtensions.TimeAgo(DateTime.Now, culture);
                            work.Write("EntryTime", DateTime.Now.ToString(), "ORDERS");
                            var order = OpenOrder(it.Symbol, side, quantity, positionSide);
                            if (order.Success)
                            {
                                var text = type + " " + it.Symbol + " quantity: " + quantity + " spread: " + decimal.Round(Spread, 3);
                                Log.Add(text, Color.Black);
                                it.Entry = price.Data.Price;
                                if (Telegram.open)
                                {
                                    Telegram.SendMsg(text);
                                }
                                list.Add(it.Symbol);
                                success++;
                            }
                            else
                            {
                                Log.Add(it.Symbol + " - " + order.Error.Message + " quantity=" + quantity, Color.Red);
                                if (Telegram.open)
                                {
                                    Telegram.SendMsg(it.Symbol + " - " + order.Error.Message + " quantity=" + quantity);
                                }
                            }
                        }
                    }

                    if (success != 0)
                    {
                        Percents.Clear();
                        var symbols = "";
                        foreach (var it in ts)
                        {
                            Percents.Add(it);
                            symbols += it.Symbol + "%" + it.Entry + "#";
                        }
                        work.Write("symbols", symbols, "CALC");
                        work.Write("EntrySpread", Spread.ToString(), "ORDERS");
                        work.Write("Volume", Cfg.volume.ToString(), "ORDERS");
                        Entered = true;
                    }
                }
                else
                {
                    SortTimer.Start();
                }
            }
        }

        

        private void GetPositions()
        {
            var openPositions = futures.GetPositionInformation();
            if (openPositions.Success)
            {

                var temp = openPositions.Data.ToList();
                BuyPNL = 0;
                SellPNL = 0;
                UnrealizedPnL = 0;
                decimal Short = 0;
                decimal Long = 0;
                foreach (var it in temp)
                {
                    UnrealizedPnL += it.UnrealizedPnL;
                    foreach (var i in Percents.ToList())
                    {


                        if (i.Symbol == it.Symbol)

                        {

                            if (it.Quantity > 0)
                            {
                                i.Long = true;
                                Entered = true;
                                Long += it.IsolatedMargin * it.Leverage;

                            }

                            else if (it.Quantity < 0)
                            {
                                i.Short = true;
                                Entered = true;
                                Short += it.IsolatedMargin * it.Leverage;
                            }



                            if (i.Long)
                                BuyPNL += it.UnrealizedPnL;
                            else if (i.Short)
                                SellPNL += it.UnrealizedPnL;

                        }
                    }



                }

                CalculatedPnL = BuyPNL + SellPNL;


                if (!Entered)
                {
                    if (CalculatedPnL != 0)
                        Entered = true;
                    else
                        Entered = false;
                }



            }
        }

        private bool TrailingOut(decimal Profit)
        {
            decimal Max;
            TrailingEnabled = true;

            try
            {
                Max = Convert.ToDecimal(work.Read("Max", "PROFIT"));

            }
            catch (Exception)
            {
                work.Write("Max", Profit.ToString(), "PROFIT");
                Max = Profit;
            }

            if (Profit == Math.Max(Max, Profit))
            {
                work.Write("Max", Profit.ToString(), "PROFIT");
                Max = Profit;
            }

            var trailing = CloseProfit * (Cfg.trailingValue / 100);
            CloseProfit = Math.Abs(Max - trailing);

            if (Math.Abs(Max - Profit) > trailing)
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        public void CloseAllPositions(bool reset)
        {
            Busy = true;
            NeedToClose = false;
            var temp = futures.GetPositionInformation().Data.ToList();
            //int buy = Convert.ToInt32(work.Read("Buy", "ORDERS"));
            if (!Status)
            {
                //reset = false;
            }

            OrderSide side = 0;
            PositionSide positionSide = PositionSide.Both;
            var sideText = "";
            decimal quantity;
            string symbols = "";
            bool empty = true;

            if (SelectedSymbols != null)
            {
                foreach (var it in SelectedSymbols)
                {
                    if (!symbols.Contains(it))
                    {
                        symbols += it + "#";
                    }
                }
            }
            else
            {
                foreach (var it in mf.selected.Items)
                {
                    if (!symbols.Contains(it.ToString()))
                    {
                        symbols += it + "#";
                    }
                }
            }





            foreach (var pos in temp)
            {

                if (symbols.Split("#").ToList().Contains(pos.Symbol))
                {
                    quantity = Math.Abs(pos.Quantity);
                    if (pos.Quantity < 0)
                    {
                        side = OrderSide.Buy;
                        sideText = "short";
                        positionSide = PositionSide.Short;
                    }
                    else if (pos.Quantity > 0)
                    {
                        side = OrderSide.Sell;
                        sideText = "long";
                        positionSide = PositionSide.Long;
                    }

                    if (quantity != 0)
                    {
                        //futures.Order.CancelOrder()
                        //Log.Debug(futures.Order.GetForcedOrders().Data.ToList()[0].Symbol);
                        var close = futures.Order.PlaceOrder(pos.Symbol, side, OrderType.Market, quantity, positionSide);
                        if (close.Success)
                        {


                            //        Log.Add("Order id " + it.OrderId + " closed", Color.Black);

                            Log.Add(string.Format("Close " + sideText + " position on " + pos.Symbol + " current spread: " + decimal.Round(Spread, 3) + " quantity: " + quantity), Color.Green);
                            empty = false;
                        }
                        else
                        {
                            Log.Add(close.Error.Message, Color.Red);

                        }
                    }

                }




            }

            if (empty)
            {
                Log.Add("Open orders not exists", Color.Blue, send: false);
                Log.Status("Open orders not exists", Color.Blue, true);
                Busy = false;
            }
            else
            {

                Reset(reset);
                Entered = false;
            }

        }

        private WebCallResult<BinanceFuturesPlacedOrder> OpenOrder(string symbol, OrderSide side, decimal quantity, PositionSide positionSide, OrderType type = OrderType.Market)
        {

            return futures.Order.PlaceOrder(symbol, side, type, quantity, positionSide, closePosition: false);
        }

        public void Reset(bool state)
        {
            //add reset settings

            Stop();

            Weights = 0;
            work.DeleteSection("CALC");
            work.DeleteSection("ORDERS");
            work.DeleteSection("PROFIT");
            TrailingEnabled = false;
            Entered = false;




            Percents.Clear();

            Log.Status("Coefficients are reset", Color.Blue, true);
            Log.Add("Coefficients are reset", Color.Blue);
            mf.SetVolumeHalfDepo();
            



            if (Cfg != null)
            {
                CloseProfit = Cfg.volume / 100 * Cfg.closeProfit;

                if (!Cfg.noEnter && state)
                {

                    StartAsync();

                }
            }


            Busy = false;

        }



        public void Stop()
        {

            //socketClient.UnsubscribeAll();

            mf.tgtimer.Enabled = false;
            mf.tgtimer.Tick -= Tgtimer_Tick;

            if (Status)
                SortTimer.Stop();

            Status = false;
            if (WorkThread != null)
            {
                WorkThread.Wait();
                WorkThread.Dispose();
            }
            mf.BeginInvoke((Action)(() =>
            {
                mf.quotes.Items.Clear();
            }));
            decimal count = 0;
            try
            {
                count = Convert.ToDecimal(settings.Read("averagecount", "TRADE"));
            }
            catch (Exception)
            {
                count = 0;
            }
            mf.BeginInvoke((Action)(() =>
            {
                mf.averageCount.Value = count;
            }));

            mf.BeginInvoke((Action)(() =>
            {
                mf.info.Items.Clear();
            }));
            Log.Status("Stoped", Color.Red);

        }


    }
}
