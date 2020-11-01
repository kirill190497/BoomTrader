using System.Collections.Generic;

namespace BoomTrader_2
{
    public class PercentItem
    {
        public string Symbol { get; set; }
        public decimal Start { get; set; }
        public decimal Percent { get; set; }
        public decimal Entry { get; set; }
        public decimal Price { get; set; }

        public bool Long { get; set; }
        public bool Short { get; set; }
    }

    class PercentCompare : IComparer<PercentItem>
    {
        public int Compare(PercentItem o1, PercentItem o2)
        {
            if (o1.Percent > o2.Percent)
            {
                return -1;
            }
            else if (o1.Percent < o2.Percent)
            {
                return 1;
            }

            return 0;
        }
    }
}
