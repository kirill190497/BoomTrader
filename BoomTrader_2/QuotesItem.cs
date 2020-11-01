using System;
using System.Drawing;
using System.Windows.Forms;

namespace BoomTrader_2
{
    public static class QuotesItem
    {

        public static ListViewItem Add(string symbol, decimal percent, decimal mark, decimal price, Color color)
        {


            ListViewItem lvi = new ListViewItem();
            ListViewItem.ListViewSubItem percents = new ListViewItem.ListViewSubItem();
            ListViewItem.ListViewSubItem marks = new ListViewItem.ListViewSubItem();
            ListViewItem.ListViewSubItem prices = new ListViewItem.ListViewSubItem();

            lvi.Text = symbol;
            lvi.ForeColor = color;
            percents.Text = Decimal.Round(percent, 3).ToString();
            lvi.SubItems.Add(percents);
            marks.Text = Decimal.Round(mark, 3).ToString();
            lvi.SubItems.Add(marks);
            prices.Text = price.ToString();
            lvi.SubItems.Add(prices);
            return lvi;
        }


    }
}
