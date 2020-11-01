using System.Windows.Forms;

namespace BoomTrader_2
{
    public static class InfoItem
    {

        public static ListViewItem Add(string item, string value, string group = null)
        {

            ListViewItem lvi = new ListViewItem();
            ListViewItem.ListViewSubItem val = new ListViewItem.ListViewSubItem();


            lvi.Text = item;

            val.Text = value;

            lvi.SubItems.Add(val);

            return lvi;
        }


    }
}
