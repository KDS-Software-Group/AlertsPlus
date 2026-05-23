using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AlertsPlus
{
    public partial class NotificationItem : UserControl 
    {
        public NotificationItem(string title, string message)
        {
            InitializeComponent();
            TitleTxt.Text = title;
            MessageTxt.Text = message;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is ListBox))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is ListBox listBox)
            {
                listBox.Items.Remove(this);
            }
        }
    }
}
