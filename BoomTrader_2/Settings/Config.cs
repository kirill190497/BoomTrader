namespace BoomTrader_2.Settings
{
    public class Config
    {
        public int buyCount { get; set; }
        public int sellCount { get; set; }
        public decimal spreadEntry { get; set; }
        public decimal volume { get; set; }
        public decimal closeProfit { get; set; }
        public bool trailing { get; set; }
        public bool noEnter { get; set; }

        public int leverage { get; set; }

        public decimal averageBefore { get; set; }
        public decimal averageCount { get; set; }
        public decimal stopLoss { get; set; }
        public bool stopLossState { get; set; }

        public decimal trailingValue { get; set; }
        public decimal martingaleValue { get; set; }
        public bool martingale { get; set; }
    }
}
